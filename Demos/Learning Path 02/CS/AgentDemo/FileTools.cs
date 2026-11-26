using System.ComponentModel;

namespace AgentDemo;

public class FileTools
{
    [Description("Read the full content of a text file on the local disk and return it.")]
    public static async Task<string> ReadFileAsync(
        [Description("Full path to the file")] string path)
    {
        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception e)
        {
            return $"Error reading {path}: {e.Message}";
        }
    }

    [Description("Create a file, or overwrite it entirely. Parent folders are created when missing. Only use when explicitly instructed.")]
    public static async Task<string> WriteFileAsync(
        [Description("Full path to the file")] string path,
        [Description("Complete new content for the file")] string content)
    {
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(path, content);
            return $"Successfully wrote to file {path}";
        }
        catch (Exception e)
        {
            return $"Error writing {path}: {e.Message}";
        }
    }

    [Description("Edit an existing file by replacing one exact piece of text with new text. Read the file first so oldText matches exactly.")]
    public static async Task<string> EditFileAsync(
        [Description("Full path to the file")] string path,
        [Description("The exact existing text to replace. Must occur exactly once in the file.")] string oldText,
        [Description("The text to put in its place")] string newText)
    {
        try
        {
            string content = await File.ReadAllTextAsync(path);
            int first = content.IndexOf(oldText, StringComparison.Ordinal);
            if (first < 0)
            {
                return $"Error: oldText was not found in {path}. Read the file again and copy the text exactly.";
            }
            if (content.IndexOf(oldText, first + oldText.Length, StringComparison.Ordinal) >= 0)
            {
                return $"Error: oldText occurs more than once in {path}. Include more surrounding text to make it unique.";
            }
            string updated = string.Concat(content.AsSpan(0, first), newText, content.AsSpan(first + oldText.Length));
            await File.WriteAllTextAsync(path, updated);
            return $"Successfully modified {path}";
        }
        catch (Exception e)
        {
            return $"Error editing {path}: {e.Message}";
        }
    }
}
