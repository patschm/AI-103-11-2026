using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using OpenAI;
using OpenAI.Images;

internal class Program
{
    // Configuration
    private const string ENDPOINT = "https://ps-mijneerste-resource.openai.azure.com/openai/v1";
    private const string MODEL_DEPLOYMENT = "gpt-image-2";

    private static async Task Main(string[] args)
    {
        try
        {
            // Initialize the client (Entra ID auth via Azure CLI sign-in)
            var tokenPolicy = new BearerTokenPolicy(
                new AzureCliCredential(), "https://cognitiveservices.azure.com/.default");
            var openAi = new OpenAIClient(tokenPolicy, new OpenAIClientOptions { Endpoint = new Uri(ENDPOINT) });
            ImageClient client = openAi.GetImageClient(MODEL_DEPLOYMENT);

            var imgNo = 0;

            // Loop until the user types 'quit'
            while (true)
            {
                Console.Write("Enter the prompt (or type 'quit' to exit): ");
                var prompt = Console.ReadLine() ?? "";
                if (prompt.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
                if (prompt.Length == 0)
                {
                    Console.WriteLine("Please enter a prompt.");
                    continue;
                }

                // Generate an image
                GeneratedImage image = (await client.GenerateImageAsync(prompt)).Value;

                // Save the image
                imgNo++;
                await SaveImageAsync(image.ImageBytes.ToArray(), $"image_{imgNo}.png");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task SaveImageAsync(byte[] data, string fileName)
    {
        // Create the images directory if it doesn't exist
        var imageDir = Path.Combine(Directory.GetCurrentDirectory(), "images");
        Directory.CreateDirectory(imageDir);

        var imagePath = Path.Combine(imageDir, fileName);
        await File.WriteAllBytesAsync(imagePath, data);
        Console.WriteLine($"Image saved as {imagePath}");
    }
}
