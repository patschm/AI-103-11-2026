using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using OpenAI.Audio;
using OpenAI.Responses;

#pragma warning disable AAIP002, OPENAI001 // preview APIs
namespace Lang_04_TextToSpeech;

// Text to speech, three ways (Learn modules 3, 4 and 5):
//   1. A speech-capable generative model        (module 3)
//   2. The Azure Speech SDK                     (module 4)
//   3. An agent with the Azure Speech MCP tool  (module 5)
internal class Program
{
    // --- Foundry resource ps-mijneerste-resource / project ps-mijneerste ---
    const string MODEL_ENDPOINT = "https://ps-mijneerste-resource.openai.azure.com/";
    const string TTS_DEPLOYMENT = "tts";

    const string SPEECH_ENDPOINT = "https://ps-mijneerste-resource.cognitiveservices.azure.com/";
    const string SPEECH_KEY = "";   // Foundry portal > project > Keys and endpoint

    const string PROJECT_ENDPOINT = "https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste";
    const string AGENT_NAME = "my-first-agent";   // agent with the Azure Speech MCP Server tool

    static async Task Main(string[] args)
    {
        // 1. Generative model
        //await SpeakWithModelAsync();
       // await SpeakAllVoicesWithModelAsync();

        // 2. Azure Speech SDK
        //await SpeakWithSdkAsync();
        //await SpeakWithVoiceAndFormatAsync();
        //await ListVoicesAsync();
        await SpeakSsmlAsync();

        // 3. Agent with the Speech MCP tool
        //await SpeakWithAgentAsync();

        Console.ReadLine();
    }

    // ---------------------------------------------------------------------------------------------
    // 1. Generative model (module 3): the request contains the text, the response the audio.
    // ---------------------------------------------------------------------------------------------
    private static AudioClient CreateTtsClient()
    {
        var azure = new AzureOpenAIClient(new Uri(MODEL_ENDPOINT), new AzureCliCredential());
        return azure.GetAudioClient(TTS_DEPLOYMENT);
    }

    private static async Task SpeakWithModelAsync()
    {
        Console.WriteLine("--- Model");
        await PlayAsync(CreateTtsClient(), "My voice is my password!", GeneratedSpeechVoice.Alloy);
    }

    // One clip per available voice (the model has a fixed list of voices)
    private static async Task SpeakAllVoicesWithModelAsync()
    {
        var client = CreateTtsClient();
        var text = "The rain in Spain stays mainly in the plain";
        foreach (var voice in new[] { GeneratedSpeechVoice.Alloy, GeneratedSpeechVoice.Echo, GeneratedSpeechVoice.Fable,
                                      GeneratedSpeechVoice.Onyx, GeneratedSpeechVoice.Nova, GeneratedSpeechVoice.Shimmer })
        {
            Console.WriteLine(voice);
            await PlayAsync(client, text, voice);
        }
    }

    private static async Task PlayAsync(AudioClient client, string text, GeneratedSpeechVoice voice)
    {
        var options = new SpeechGenerationOptions { 
            ResponseFormat = GeneratedSpeechFormat.Wav  };
        
        var result = await client.GenerateSpeechAsync(text, voice, options);

        using var ms = new MemoryStream(result.Value.ToArray());
        using var reader = new WaveFileReader(ms);
        using var output = new WaveOut();
        output.Init(reader);
        output.Play();
        while (output.PlaybackState == PlaybackState.Playing)
            await Task.Delay(100);
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Azure Speech SDK (module 4): SpeechConfig + AudioConfig + SpeechSynthesizer.
    // ---------------------------------------------------------------------------------------------
    private static SpeechConfig CreateSpeechConfig() =>
        SpeechConfig.FromEndpoint(new Uri(SPEECH_ENDPOINT), new AzureCliCredential());

    private static async Task SpeakWithSdkAsync()
    {
        Console.WriteLine("--- Speech SDK");
        using var audio = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(CreateSpeechConfig(), audio);

        var text = "My voice is my password!";
        var result = await synthesizer.SpeakTextAsync(text);
        ShowSynthesisResult(result, text);
    }

    // Choose the output format and the voice on the SpeechConfig
    private static async Task SpeakWithVoiceAndFormatAsync()
    {
        Console.WriteLine("--- Speech SDK, voice and format");
        var config = CreateSpeechConfig();
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
        config.SpeechSynthesisVoiceName = "en-US-Brian:DragonHDLatestNeural";

        using var audio = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(config, audio);

        var text = "The rain in Spain stays mainly in the plain";
        var result = await synthesizer.SpeakTextAsync(text);
        ShowSynthesisResult(result, text);
    }

    // List the voices for a locale
    private static async Task ListVoicesAsync(string locale = "nl-NL")
    {
        using var synthesizer = new SpeechSynthesizer(CreateSpeechConfig(), null as AudioConfig);
        var result = await synthesizer.GetVoicesAsync(locale);
        Console.WriteLine($"Voices for {locale}:");
        foreach (var voice in result.Voices)
        {
            Console.WriteLine($"{voice.ShortName} ({voice.Gender})");
        }
    }

    // Speech Synthesis Markup Language: voices, speaking style, phonemes, pauses
    private static async Task SpeakSsmlAsync()
    {
        Console.WriteLine("--- Speech SDK, SSML");
        var ssml = """
            <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis"
                                 xmlns:mstts="https://www.w3.org/2001/mstts" xml:lang="en-US">
                <voice name="en-US-AriaNeural">
                    <mstts:express-as style="cheerful">
                        I say tomato
                    </mstts:express-as>
                </voice>
                <voice name="en-US-GuyNeural">
                    I say <phoneme alphabet="sapi" ph="t ao m ae t ow"> tomato </phoneme>.
                    <break strength="weak"/>Lets call the whole thing off!
                </voice>
            </speak>
            """;

        using var audio = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(CreateSpeechConfig(), audio);
        var result = await synthesizer.SpeakSsmlAsync(ssml);
        ShowSynthesisResult(result, "SSML");
    }

    private static void ShowSynthesisResult(SpeechSynthesisResult result, string text)
    {
        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            Console.WriteLine($"Speech synthesized for text [{text}]");
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var details = SpeechSynthesisCancellationDetails.FromResult(result);
            Console.WriteLine($"Speech synthesis canceled: {details.Reason}");
            if (details.Reason == CancellationReason.Error && details.ErrorDetails != null)
                Console.WriteLine($"Error details: {details.ErrorDetails}");
        }
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Agent with the Azure Speech MCP tool (module 5): the audio is saved in blob storage
    //    and the agent answers with a link. See README.md for the storage and tool setup.
    // ---------------------------------------------------------------------------------------------
    private static async Task SpeakWithAgentAsync()
    {
        Console.WriteLine("--- Agent");
        var projectClient = new AIProjectClient(new Uri(PROJECT_ENDPOINT), new AzureCliCredential());
        var responses = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(AGENT_NAME);

        string[] prompts =
        [
            "Generate \"To be or not to be, that is the question.\" as speech",
            "Synthesize \"Better a witty fool, than a foolish wit!\" as speech using the voice \"en-GB-SoniaNeural\"."
        ];

        foreach (var prompt in prompts)
        {
            Console.WriteLine($"> {prompt}");
            var response = (await responses.CreateResponseAsync(prompt)).Value;

            // The MCP tool may ask for approval first ("require_approval": "always").
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
