using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class StringLengthComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        int result = x.Length.CompareTo(y.Length);
        return result == 0 ? string.Compare(x, y, StringComparison.Ordinal) : result;
    }
}

public static class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Proszę podać ścieżkę katalogu jako parametr wywołania programu.");
            return;
        }

        string directoryPath = args[0];
        DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);

        Console.WriteLine($"Zawartość katalogu {directoryPath}:");
        DisplayDirectoryContents(dirInfo, 0);
        Console.WriteLine();

        // Załadowanie bezpośrednich elementów katalogu do uporządkowanej kolekcji
        SortedDictionary<string, long> orderedFiles = LoadDirectoryContents(dirInfo);

        var oldestFileData = dirInfo.GetOldestFileData();
        if (!string.IsNullOrEmpty(oldestFileData.Key))
        {
            Console.WriteLine($"Najstarszy plik: {oldestFileData.Key}, Data utworzenia: {oldestFileData.Value}");
        }
        else
        {
            Console.WriteLine("Brak plików w katalogu.");
        }


        // Serializacja i deserializacja kolekcji
        SerializeAndDeserialize(orderedFiles);
    }

    static void DisplayDirectoryContents(DirectoryInfo dir, int indentLevel)
    {
        try
        {
            foreach (var file in dir.GetFiles())
            {
                Console.WriteLine($"{GetIndent(indentLevel)}{file.Name} ({file.Length} bajtów) {file.ToDOSAttributes()}");
            }

            foreach (var subDir in dir.GetDirectories())
            {
                int directoryCount = subDir.GetFileSystemInfos().Length;
                Console.WriteLine($"{GetIndent(indentLevel)}{subDir.Name} ({directoryCount} items) {subDir.ToDOSAttributes()}");
                DisplayDirectoryContents(subDir, indentLevel + 1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd przy wyświetlaniu zawartości: {ex.Message}");
        }
    }

    static SortedDictionary<string, long> LoadDirectoryContents(DirectoryInfo dir)
    {
        var files = new SortedDictionary<string, long>(new StringLengthComparer());

        foreach (var file in dir.GetFiles())
        {
            files.Add(file.Name, file.Length);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            files.Add(subDir.Name, subDir.GetFileSystemInfos().Length);
        }

        return files;
    }


    static void SerializeAndDeserialize(SortedDictionary<string, long> files)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (var stream = new MemoryStream())
        {
            formatter.Serialize(stream, files);
            stream.Position = 0;

            var deserializedFiles = (SortedDictionary<string, long>)formatter.Deserialize(stream);

            Console.WriteLine("Zawartość kolekcji po deserializacji:");
            foreach (var file in deserializedFiles)
            {
                Console.WriteLine($"{file.Key} -> {file.Value} B");
            }
        }
    }

    static string GetIndent(int level)
    {
        return new string(' ', level * 2);
    }

    static FileInfo GetOldestFileInfo(DirectoryInfo directory, DateTime oldestDate)
    {
        FileInfo oldestFile = null;
        foreach (var file in directory.GetFiles("*.*", SearchOption.AllDirectories))
        {
            if (file.LastWriteTime == oldestDate)
            {
                oldestFile = file;
                break;
            }
        }
        return oldestFile;
    }
}



public static class DirectoryInfoExtensions
{
    public static KeyValuePair<string, DateTime> GetOldestFileData(this DirectoryInfo dir)
    {
        DateTime oldestDate = DateTime.MaxValue;
        string oldestFileName = string.Empty;

        foreach (var file in dir.GetFiles("*.*", SearchOption.AllDirectories))
        {
            if (file.LastWriteTime < oldestDate)
            {
                oldestDate = file.LastWriteTime;
                oldestFileName = file.Name;
            }
        }

        return new KeyValuePair<string, DateTime>(oldestFileName, oldestDate);
    }
}


public static class FileSystemInfoExtensions
{
    public static string ToDOSAttributes(this FileSystemInfo info)
    {
        FileAttributes attributes = info.Attributes;
        return $"{(attributes.HasFlag(FileAttributes.ReadOnly) ? 'r' : '-')}" +
               $"{(attributes.HasFlag(FileAttributes.Archive) ? 'a' : '-')}" +
               $"{(attributes.HasFlag(FileAttributes.Hidden) ? 'h' : '-')}" +
               $"{(attributes.HasFlag(FileAttributes.System) ? 's' : '-')}";
    }
}
