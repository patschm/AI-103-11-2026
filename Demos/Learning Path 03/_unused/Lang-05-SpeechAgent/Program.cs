using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Responses;

#pragma warning disable AAIP002, OPENAI001 // preview APIs
namespace Lang_05_SpeechAgent;

// Learn module 5: Develop a speech agent with the Azure Speech MCP server.
//
// Prepare in the Foundry portal (see the module):
//  1. A storage account + container (you need Storage Blob Data Contributor) and a SAS URL for the container
//     with Read, Add, Create, Write and List permissions (short expiry).
//  2. Tools > Add a new tool > Catalog > "Azure Speech MCP Server": give the Foundry resource name, the key,
//     and the SAS URL (header X-Blob-Container-Url).
//  3. An agent that uses that tool, e.g. model gpt-4.1 with instruction
//     "You are an AI agent that uses the Azure AI Speech tool to transcribe and generate speech."
internal class Program
{
    const string PROJECT_ENDPOINT = "https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste";
    const string AGENT_NAME = "my-first-agent";

    static async Task Main(string[] args)
    {
        var projectClient = new AIProjectClient(new Uri(PROJECT_ENDPOINT), new AzureCliCredential());
        var responses = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(AGENT_NAME);

        string[] prompts =
        [
            "Generate \"To be or not to be, that is the question.\" as speech",
            "Synthesize \"Better a witty fool, than a foolish wit!\" as speech using the voice \"en-GB-SoniaNeural\".",
            //"Transcribe the file at https://example.com/audio/meeting-recording.wav"
        ];

        foreach (var prompt in prompts)
        {
            Console.WriteLine($"> {prompt}");
            var response = (await responses.CreateResponseAsync(prompt)).Value;

            // The agent's MCP tool may ask for approval first ("require_approval": "always").
            while (response.OutputItems.OfType<McpToolCallApprovalRequestItem>().FirstOrDefault() is { } request)
            {
                Console.WriteLine($"  approving tool call: {request.ToolName}");
                var options = new CreateResponseOptions { PreviousResponseId = response.Id };
                options.InputItems.Add(ResponseItem.CreateMcpApprovalResponseItem(request.Id, approved: true));
                response = (await responses.CreateResponseAsync(options)).Value;
            }

            Console.WriteLine(response.GetOutputText());
            Console.WriteLine(new string('=', 80));
        }
    }
}
