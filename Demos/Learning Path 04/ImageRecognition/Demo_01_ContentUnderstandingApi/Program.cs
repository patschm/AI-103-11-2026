using System.Text.Json;
using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Core;
using Azure.Identity;

namespace Demo_01_ContentUnderstandingApi;

public class Program
{
   private const string ENDPOINT = "https://ps-mijneerste-resource.cognitiveservices.azure.com/";
    private const string ANALYZERID = "businesscardanalyzer";
    private const string SCHEMAFILE = "biz-card.json";
    
    public static async Task Main(string[] args)
    {
        var client = new ContentUnderstandingClient(new Uri(ENDPOINT), new AzureCliCredential());

        await CreateAnalyzerAsync(client, ANALYZERID, SCHEMAFILE);
        for (int i = 1; i < 3; i++)
        {
            await AnalyzeCardAsync(client, ANALYZERID, $"biz-card-{i}.png");
        }
    }

    private static async Task CreateAnalyzerAsync(ContentUnderstandingClient client, string analyzerId, string schemaFile)
    {
        Console.WriteLine($"Creating {analyzerId}");

        // The schema file is the analyzer definition; send it as-is
        var schemaJson = await File.ReadAllTextAsync(schemaFile);

        // Create the analyzer (long-running operation), replacing it if it already exists
        var operation = await client.CreateAnalyzerAsync(
            WaitUntil.Completed, analyzerId, RequestContent.Create(BinaryData.FromString(schemaJson)), allowReplace: true);

        Console.WriteLine($"Analyzer '{analyzerId}' created successfully.");
        Console.WriteLine($"Status: {(operation.HasCompleted ? "Succeeded" : "Running")}");
    }

    private static async Task AnalyzeCardAsync(ContentUnderstandingClient client, string analyzerId, string imageFile)
    {
        Console.WriteLine($"Analyzing {imageFile}");

        // Read the image data
        var imageData = await File.ReadAllBytesAsync(imageFile);

        // Submit the image for analysis and wait for completion
        Console.WriteLine("Submitting request...");
        var operation = await client.AnalyzeBinaryAsync(
            WaitUntil.Completed, analyzerId, BinaryData.FromBytes(imageData));
        Console.WriteLine("Analysis succeeded:\n");

        // Save JSON results to a file
        const string outputFile = "results.json";
        await File.WriteAllTextAsync(outputFile,
            JsonSerializer.Serialize(operation.Value, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Response saved in {outputFile}\n");

        // Iterate through the contents and extract fields
        foreach (var content in operation.Value.Contents)
        {
            if (content.Fields is null) continue;
            foreach (var (name, field) in content.Fields)
                Console.WriteLine($"{name}: {(field as ContentStringField)?.Value}");
        }
    }
}
