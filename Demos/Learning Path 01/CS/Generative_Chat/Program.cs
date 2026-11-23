using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace Generative_1_Chat;

internal class Program
{
    static readonly Uri FOUNDRY_PROJECT_ENDPOINT = new Uri("https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste");
    static readonly string MODEL_DEPLOYMENT_NAME = "gpt-5.4-infereence";
    static readonly string[] SAMPLE_QUESTIONS = ["What is the fastest animal on Earth?", "Where can I see one?", "Are they endangered?", "Be creative"];
    static readonly TokenCredential CREDENTIAL = new AzureCliCredential();


    static void Main(string[] args)
    {
        ResponseApi();  // Preferred approach for handling responses
       //CompletionApi(); // Only legacy
        
    }

    private static void CompletionApi()
    {
        AIProjectClient project = new AIProjectClient(FOUNDRY_PROJECT_ENDPOINT, CREDENTIAL);
        ChatClient chatClient = project.ProjectOpenAIClient.GetChatClient(MODEL_DEPLOYMENT_NAME);

        List<ChatMessage> messages = [
            new SystemChatMessage("You are a helpful AI assistant that answers questions.")
            ];

        int index = 0;
        do
        {
            string prompt = GetUserInput(ref index);   
            messages.Add(new UserChatMessage(prompt));
            
            ChatCompletion response = chatClient.CompleteChat(messages);
            foreach (var part in response.Content)
            {
                if (part.Kind == ChatMessageContentPartKind.Text)
                    Console.WriteLine(part.Text);
            }
            // Keep the assistant reply so the model sees the full conversation next turn
            messages.Add(new AssistantChatMessage(response));

            Console.WriteLine("Press ESC to quit, other key to continue");
        }
        while (Console.ReadKey().Key != ConsoleKey.Escape);
    }

    
#pragma warning disable OPENAI001 // Responses client is marked experimental
    private static void ResponseApi()
    {
        AIProjectClient project = new AIProjectClient(FOUNDRY_PROJECT_ENDPOINT, CREDENTIAL);
        ResponsesClient responsesClient = project.ProjectOpenAIClient.GetResponsesClient();

        // Server-side state: each call is chained to the previous one via previous_response_id
        string? lastResponseId = null;
        int index = 0;
        CreateResponseOptions options = new()
        {
            Model = MODEL_DEPLOYMENT_NAME,
            Instructions = "You are a helpful AI assistant that answers questions."
        };
        do
        {   
            string prompt = GetUserInput(ref index);   
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));

            ResponseResult response = responsesClient.CreateResponse(options);

            Console.WriteLine(response.GetOutputText());
            options.PreviousResponseId = response.Id;
  
            Console.WriteLine("Press ESC to quit, other key to continue");
        }
        while (Console.ReadKey().Key != ConsoleKey.Escape);
    }
#pragma warning restore OPENAI001


    private static string GetUserInput(ref int index)
    {
        if (index >= SAMPLE_QUESTIONS.Length) index = 3;
        Console.WriteLine($"Ask anything (default: {SAMPLE_QUESTIONS[index]})");
        string prompt = Console.ReadLine()!;

        if (string.IsNullOrEmpty(prompt)) prompt = SAMPLE_QUESTIONS[index++];   
        return prompt;
    }
}