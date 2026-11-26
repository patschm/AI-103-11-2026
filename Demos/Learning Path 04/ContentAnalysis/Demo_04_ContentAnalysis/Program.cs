using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Identity;

internal class Program
{
    // Configuration
    private const string ENDPOINT = "https://ps-mijneerste-resource.services.ai.azure.com";
    private const string ANALYZER_ID = "PatrickBegrijpt";

    private static async Task<int> Main(string[] args)
    {
        // Set up Content Understanding client
        var client = new ContentUnderstandingClient(new Uri(ENDPOINT), new AzureCliCredential());

        while (true)
        {
            Console.Write("\nChoose a file (1, 2, or 3), or anything else to exit: ");
            var fileNo = Console.ReadLine()?.Trim();
            if (fileNo is not ("1" or "2" or "3")) break;

            var filePath = Path.Combine("images", $"image{fileNo}.jpg");
            var bytes = await File.ReadAllBytesAsync(filePath);

            Console.WriteLine($"Analyzing with {ANALYZER_ID} analyzer...");
            Console.WriteLine($"  File: {filePath}\n");

            try
            {
                var operation = await client.AnalyzeBinaryAsync(
                    WaitUntil.Completed, ANALYZER_ID, BinaryData.FromBytes(bytes));
                var fields = operation.Value.Contents[0].Fields;

                foreach (var (name, field) in fields)
                {
                    if (name == "Description")
                    {
                        Console.WriteLine($"{name}:\n{(field as ContentStringField)?.Value}\n");
                    }
                    else if (name == "Tags")
                    {
                        Console.WriteLine($"{name}:");
                        foreach (var tag in (field as ContentArrayField)?.Value ?? [])
                            Console.WriteLine($"  - {(tag as ContentStringField)?.Value}");
                    }
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"[Azure Error]: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Unexpected Error]: {ex.Message}");
                return 1;
            }
        }
        return 0;
    }
}
