using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.TextAnalytics;
using Azure.Identity;

#pragma warning disable AAIP002, OPENAI001 // preview APIs
namespace Lang_01_TextAnalysis;

internal class Program
{
    // In Azure Portal create resource "Language Service" to get endpoint and key
    const string ENDPOINT = "https://ps-mijneerste-resource.services.ai.azure.com";
    const string KEY = "3PuxSoiorcTGbqYbCpX7Y2VtlJEtgM3KZAhntqYdYJmR867V6BYFJQQJ99CKACfhMk5XJ3w3AAAAACOGXniN";
    static TextAnalyticsClient client;

    const string PROJECT_ENDPOINT = "https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste";
    // Name of your existing agent
    const string AGENT_NAME = "my-first-agent";

    static async Task Main(string[] args)
    {     
            //AnalyzeWithSdk();
           await AnalyzeWithAgentAsync();
    }

    /// <summary>Function 2: send the reviews to the existing Foundry agent, which uses the Azure Language MCP tool.</summary>
    static async Task AnalyzeWithAgentAsync()
    {
        var projectClient = new AIProjectClient(new Uri(PROJECT_ENDPOINT), new DefaultAzureCredential());
        var responses = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(AGENT_NAME);

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "/Reviews");
        foreach (var file in dir.GetFiles("*.txt"))
        {
            var text = File.ReadAllText(file.FullName);
            Console.WriteLine($"{file.Name}:");
            Console.WriteLine(text);
            Console.WriteLine("====================");

            var response = await responses.CreateResponseAsync($"Analyze this review:\n\n{text}");
            Console.WriteLine(response.Value.GetOutputText());
            Console.WriteLine();
        }
    }

    /// <summary>Function 1: call the Language service directly with the TextAnalytics SDK.</summary>
    static void AnalyzeWithSdk()
    {
        client = new TextAnalyticsClient(new Uri(ENDPOINT), new AzureKeyCredential(KEY));
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "/Reviews");
        foreach(var file in dir.GetFiles("*.txt"))
        {
            var text = File.ReadAllText(file.FullName);
            Console.WriteLine($"{file.Name}:");
            Console.WriteLine(text);
            Console.WriteLine("====================");
            GetLanguage(text);
            GetSentiment(text);
            GetKeyPhrases(text);
            GetEntities(text);
            GetLinkedEntities(text);
            GetPiiEntities(text);
        }

    }

    // Personal information (PII): list what was found and show the text with the PII redacted
    private static void GetPiiEntities(string text)
    {
        var response = client.RecognizePiiEntities(text);
        Console.WriteLine("PII:");
        foreach(var entity in response.Value)
        {
            Console.WriteLine($"  {entity.Text} ({entity.Category}, {entity.ConfidenceScore})");
        }
        Console.WriteLine($"Redacted: {response.Value.RedactedText}");
    }

    private static void GetLanguage(string text)
    {
        var response = client.DetectLanguage(text);
        Console.WriteLine($"{response.Value.Name} (confidence: {response.Value.ConfidenceScore})");
    }

    private static void GetSentiment(string text)
    {
        var response = client.AnalyzeSentiment(text);
        Console.WriteLine($"{response.Value.Sentiment}");
    }

    private static void GetKeyPhrases(string text)
    {
        var response = client.ExtractKeyPhrases(text);
        foreach(var phrase in response.Value)
        {
            Console.WriteLine($"  {phrase}");
        }
    }

    private static void GetEntities(string text)
    {
        var response = client.RecognizeEntities(text);
        foreach(var entity in response.Value)
        {
            Console.WriteLine($"  {entity.Text} ({entity.Category}, {entity.ConfidenceScore})");
        }
    }

    private static void GetLinkedEntities(string text)
    {
        var response = client.RecognizeLinkedEntities(text);
        foreach(var entity in response.Value)
        {
            Console.WriteLine($"  {entity.Name} ({entity.DataSource}, {entity.Url})");
        }
    }
}
