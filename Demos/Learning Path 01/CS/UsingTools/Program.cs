using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using OpenAI;
using OpenAI.Files;
using OpenAI.Responses;
using OpenAI.VectorStores;

namespace UsingTools;

internal class Program
{
    static readonly Uri FOUNDRY_PROJECT_ENDPOINT = new Uri("https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste");
    static readonly string MODEL_DEPLOYMENT_NAME = "gpt-5.4-infereence";
    static readonly TokenCredential CREDENTIAL = new AzureCliCredential();

#pragma warning disable OPENAI001 // Responses client is marked experimental
    static void Main(string[] args)
    {
        AIProjectClient project = new AIProjectClient(FOUNDRY_PROJECT_ENDPOINT, CREDENTIAL);
        OpenAIClient client = project.ProjectOpenAIClient;
        ResponsesClient responsesClient = client.GetResponsesClient();

        //CodeInterpreterTool(responsesClient);
        //WebSearchTool(responsesClient);
        //FileSearchTool(client, responsesClient);
        FunctionTool(responsesClient);
    }

    // Runs on the SERVER: Python executes in a Foundry-hosted sandbox
    private static void CodeInterpreterTool(ResponsesClient responsesClient)
    {
        Console.WriteLine("\n=== code_interpreter ===");
        CreateResponseOptions options = new()
        {
            Model = MODEL_DEPLOYMENT_NAME,
            Instructions = "You are an AI assistant that provides information. Use the python tool to run code for math problems."
        };
        options.Tools.Add(ResponseTool.CreateCodeInterpreterTool(
            new CodeInterpreterToolContainer(CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration())));
        options.InputItems.Add(ResponseItem.CreateUserMessageItem("What is the square root of 16?"));

        ResponseResult response = responsesClient.CreateResponse(options);
        Console.WriteLine(response.GetOutputText());
    }

    // Runs on the SERVER: Foundry performs the web search
    private static void WebSearchTool(ResponsesClient responsesClient)
    {
        Console.WriteLine("\n=== web_search ===");
        CreateResponseOptions options = new()
        {
            Model = MODEL_DEPLOYMENT_NAME,
            Instructions = "You are an AI assistant. Use web search when current information is required."
        };
        options.Tools.Add(ResponseTool.CreateWebSearchTool());
        options.InputItems.Add(ResponseItem.CreateUserMessageItem("What are three major announcements from Microsoft Build this year?"));

        ResponseResult response = responsesClient.CreateResponse(options);
        Console.WriteLine(response.GetOutputText());
    }

    // Runs on the SERVER: file is indexed and searched in a Foundry-hosted vector store
    private static void FileSearchTool(OpenAIClient client, ResponsesClient responsesClient)
    {
        Console.WriteLine("\n=== file_search ===");

        // Create a small sample policy document, upload it and index it
        string path = Path.Combine(Path.GetTempPath(), "expenses_policy.txt");
        File.WriteAllText(path, "Expenses policy: Taxi rides may be claimed up to a maximum of 50 euros per ride. Meals are limited to 30 euros per day.");

        OpenAIFile file = client.GetOpenAIFileClient().UploadFile(path, FileUploadPurpose.Assistants);

        VectorStoreCreationOptions storeOptions = new() { Name = "policy-docs" };
        storeOptions.FileIds.Add(file.Id);
        VectorStore store = client.GetVectorStoreClient().CreateVectorStore(storeOptions);

        // Wait until the file is indexed
        while (client.GetVectorStoreClient().GetVectorStore(store.Id).Value.FileCounts.InProgress > 0)
            Thread.Sleep(1000);

        CreateResponseOptions options = new() { Model = MODEL_DEPLOYMENT_NAME };
        options.Tools.Add(ResponseTool.CreateFileSearchTool([store.Id]));
        options.InputItems.Add(ResponseItem.CreateUserMessageItem("What's the maximum amount I can claim for a taxi ride?"));

        ResponseResult response = responsesClient.CreateResponse(options);
        Console.WriteLine(response.GetOutputText());

        // Clean up
        client.GetVectorStoreClient().DeleteVectorStore(store.Id);
        client.GetOpenAIFileClient().DeleteFile(file.Id);
    }

    // Runs on the CLIENT: the model only requests the call, our C# code executes it
    private static void FunctionTool(ResponsesClient responsesClient)
    {
        Console.WriteLine("\n=== function ===");
        ResponseTool getTime = ResponseTool.CreateFunctionTool(
            functionName: "get_time",
            functionDescription: "Get the current time",
            functionParameters: BinaryData.FromString("""{"type":"object","properties":{}}"""),
            strictModeEnabled: false);

        string[] prompts = ["Hello", "What time is it?"];
        string? lastResponseId = null;
        CreateResponseOptions options = new()
        {
            Model = MODEL_DEPLOYMENT_NAME,
            Instructions = "Answer only with the tool output when a tool is used.",
            //PreviousResponseId = lastResponseId
        };
        options.Tools.Add(getTime);

        foreach (string prompt in prompts)
        {
            Console.WriteLine($"> {prompt}");        
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));

            ResponseResult response = responsesClient.CreateResponse(options);

            // Server returned a function_call: run it HERE, then send the result back
            while (response.OutputItems.OfType<FunctionCallResponseItem>().Any())
            {
                
                options.PreviousResponseId = response.Id;
                foreach (FunctionCallResponseItem call in response.OutputItems.OfType<FunctionCallResponseItem>())
                {
                    Console.WriteLine($"(model called {call.FunctionName})");
                    options.InputItems.Add(ResponseItem.CreateFunctionCallOutputItem(call.CallId, GetTime()));
                }
                response = responsesClient.CreateResponse(options);
            }

            Console.WriteLine(response.GetOutputText());
            lastResponseId = response.Id;
        }
    }
#pragma warning restore OPENAI001

    // Executes locally on this machine, never on the server
    private static string GetTime() => DateTime.Now.ToString("HH:mm:ss");
}
