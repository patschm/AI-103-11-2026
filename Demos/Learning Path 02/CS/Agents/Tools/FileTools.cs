using System.ComponentModel;

namespace Agents.Tools;

public class FileTools
{
    [Description("Read the full content of a text file on the local disk.")]
    public static async Task<string> ReadFileAsync(
        [Description("Path to file")]string path)
    {
        try
        {
            await File.ReadAllTextAsync(path);
            return $"Succesfully read file {path}";
        }
        catch(Exception e)
        {
            return e.ToString();
        }
    }
    [Description("Overwrite the entire content of a file. Only use when explicitly instructed and after showing preview.")]
    public static async Task<string> WriteFileAsync(
        [Description("Path to file")] string path,
        [Description("Complete new content for the file")] string content)
    {
        try
        {
            await File.WriteAllTextAsync(path, content);
            return $"Succesfully wrote to file {path}";
        }
        catch (Exception e)
        {
            return e.ToString();

        }
    }

    [Description("Modify the content of a file. Only use when explicitly instructed and after showing preview.")]
    public static async Task<string> EditFileAsync(
        [Description("Path to file")] string path, 
        [Description("A function that performs the edit")]Func<string, string> editFunc)
    {
        try
        {
            var existingContent = await File.ReadAllTextAsync(path);
            var newContent = editFunc(existingContent);
            await File.WriteAllTextAsync(path, newContent);
            return $"Succesfully modified {path}";
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
    [Description("Delete a file from the local disk. Only use when explicitly instructed and after showing preview.")]
    public static async Task<string> DeleteFileAsync(
       [Description("Path to file")] string path)
    {
        try
        {
            File.Delete(path);
            return $"Succesfully deleted {path}";
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
    [Description("Creates a new folder")]
    public static async Task<string> CreateFolderAsync(
        [Description("Full path to folder")] string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return $"Succesfully created folder {path}";
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}
