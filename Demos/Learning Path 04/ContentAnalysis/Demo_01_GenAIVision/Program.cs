using System.ClientModel.Primitives;
using Azure.Identity;
using OpenAI;
using OpenAI.Responses;

internal class Program
{
    // Configuration
    private const string ENDPOINT = "https://ps-mijneerste-resource.openai.azure.com/openai/v1";
    private const string MODEL_DEPLOYMENT = "gpt-5.2";
    private const string SYSTEM_MESSAGE = "You are an AI assistant in a grocery store that sells fruit. You provide detailed answers to questions about produce.";

    // Local images from the project (copied to the output directory)
    private static readonly string[] ImageNames = ["orange.jpeg", "mango.jpeg", "mystery-fruit.jpeg"];

    private static async Task Main(string[] args)
    {
        var tokenPolicy = new BearerTokenPolicy(new AzureCliCredential(), "https://ai.azure.com/.default");
        var client = new OpenAIClient(tokenPolicy, new OpenAIClientOptions { Endpoint = new Uri(ENDPOINT) })
            .GetResponsesClient();

        var systemMessage = ResponseItem.CreateDeveloperMessageItem(SYSTEM_MESSAGE);
        for(int i = 0; i < ImageNames.Length; i++)
        {
            var prompt = "Give me recipe for the fruit in the image";
            
            var imageFile = ImageNames[i]; // pick any entry from ImageNames
            var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, imageFile));
            
            var imagePart = ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(bytes, "image/jpeg"));
            var textPart = ResponseContentPart.CreateInputTextPart(prompt);
            var userMessage = ResponseItem.CreateUserMessageItem([textPart, imagePart]); 
           
            var options = new CreateResponseOptions
            {
                Model = MODEL_DEPLOYMENT,
                InputItems = { systemMessage, userMessage }
            };

            var response = (await client.CreateResponseAsync(options)).Value;
            Console.WriteLine(response.GetOutputText());
        }
    }
}
