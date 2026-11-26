using Azure.AI.VoiceLive;
using System.Threading.Tasks;

namespace Lang_06_VoiceLive;

// Just create a new project in https://ai.azure.com
internal class Program
{
    const string VOICE_LIVE_ENDPOINT = "https://ps-mijneerste-resource.cognitiveservices.azure.com/";
    const string VOICE_LIVE_ENDPOINT2 = "https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste";
    
    const string VOICE_LIVE_API_KEY = "3PuxSoiorcTGbqYbCpX7Y2VtlJEtgM3KZAhntqYdYJmR867V6BYFJQQJ99CKACfhMk5XJ3w3AAAAACOGXniN";
    const string VOICE_LIVE_MODEL = "gpt-realtime";
    const string VOICE_LIVE_VOICE = "en-US-Emma2:DragonHDLatestNeural";
    const string VOICE_LIVE_INSTRUCTIONS = "You are a helpful AI assistant with a focus on world history. Respond naturally and conversationally. Keep your responses concise but engaging.";
    
    static async Task Main(string[] args)
    {
        var client = new VoiceLiveClient(
            new Uri(VOICE_LIVE_ENDPOINT), 
            new Azure.AzureKeyCredential(VOICE_LIVE_API_KEY));
        var assistent = new VoiceAssistent(client, VOICE_LIVE_MODEL, VOICE_LIVE_VOICE, VOICE_LIVE_INSTRUCTIONS);
        try
        {
            await assistent.StartAsync();
            Console.ReadLine();
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }

    }
}
