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
namespace Lang_03_SpeechToText;

// Speech to text, three ways (Learn modules 3, 4 and 5):
//   1. A speech-capable generative model        (module 3)
//   2. The Azure Speech SDK                     (module 4)
//   3. An agent with the Azure Speech MCP tool  (module 5)
internal class Program
{
    // --- Foundry resource ps-mijneerste-resource / project ps-mijneerste ---
    const string MODEL_ENDPOINT = "https://ps-mijneerste-resource.openai.azure.com/";
    const string TRANSCRIBE_DEPLOYMENT = "gpt-4o-mini-transcribe";

    const string SPEECH_ENDPOINT = "https://ps-mijneerste-resource.cognitiveservices.azure.com/";
    const string SPEECH_KEY = "";   // Foundry portal > project > Keys and endpoint

    const string PROJECT_ENDPOINT = "https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste";
    const string AGENT_NAME = "my-first-agent";   // agent with the Azure Speech MCP Server tool
    // The agent can only read audio from a URL, e.g. a blob in the container of the Speech MCP tool,
    // with a SAS token (see the README in Lang-04-TextToSpeech).
    const string AUDIO_URL = "";

    static async Task Main(string[] args)
    {
        // 1. Generative model
       // await FromFileWithModelAsync();
       await FromMicrophoneWithModelAsync();

        // 2. Azure Speech SDK
        //await FromFileWithSdkAsync();
        //await FromMicrophoneWithSdkAsync();

        // 3. Agent with the Speech MCP tool
        await FromUrlWithAgentAsync();

        Console.ReadLine();
    }

    // ---------------------------------------------------------------------------------------------
    // 1. Generative model (module 3): the request contains the audio, the response the transcript.
    // ---------------------------------------------------------------------------------------------
    private static AudioClient CreateTranscribeClient()
    {
        var client = new AzureOpenAIClient(new Uri(MODEL_ENDPOINT), new AzureCliCredential());
        return client.GetAudioClient(TRANSCRIBE_DEPLOYMENT);
    }

    private static async Task FromFileWithModelAsync()
    {
        Console.WriteLine("--- Model, from file");
        try
        {
            var result = await CreateTranscribeClient().TranscribeAudioAsync("time.wav");
            Console.WriteLine(result.Value.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cancelled ({ex.Message})");
        }
    }

    // Record a few seconds, then transcribe. Say "stop" to end.
    private static async Task FromMicrophoneWithModelAsync()
    {
        Console.WriteLine("--- Model, from microphone. Say 'stop' to stop");
        var client = CreateTranscribeClient();
        while (true)
        {
            Console.WriteLine("Listening (5 seconds)...");
            var wav = await RecordAsync(TimeSpan.FromSeconds(5));
            var options = new AudioTranscriptionOptions { Language = "nl" };
            var result = await client.TranscribeAudioAsync(wav, "mic.wav", options);
            Console.WriteLine(result.Value.Text);
            if (result.Value.Text.Trim().StartsWith("stop", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Ok. Bye!");
                break;
            }
        }
    }

    private static async Task<Stream> RecordAsync(TimeSpan duration)
    {
        var format = new WaveFormat(16000, 16, 1);
        var ms = new MemoryStream();
        using var waveIn = new WaveIn { WaveFormat = format };
        using var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), format);
        waveIn.DataAvailable += (s, e) => writer.Write(e.Buffer, 0, e.BytesRecorded);
        waveIn.StartRecording();
        await Task.Delay(duration);
        waveIn.StopRecording();
        writer.Flush();
        writer.Dispose();
        ms.Position = 0;
        return ms;
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Azure Speech SDK (module 4): SpeechConfig + AudioConfig + SpeechRecognizer.
    // ---------------------------------------------------------------------------------------------
    private static SpeechConfig CreateSpeechConfig() =>
        SpeechConfig.FromEndpoint(new Uri(SPEECH_ENDPOINT), new AzureKeyCredential(SPEECH_KEY));

    private static async Task FromFileWithSdkAsync()
    {
        Console.WriteLine("--- Speech SDK, from file");
        using var audio = AudioConfig.FromWavFileInput("time.wav");
        using var recognizer = new SpeechRecognizer(CreateSpeechConfig(), audio);

        var result = await recognizer.RecognizeOnceAsync();
        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"Transcription:\n{result.Text}");
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var details = CancellationDetails.FromResult(result);
            Console.WriteLine($"Cancelled ({details.Reason}): {details.ErrorDetails}");
        }
        else
        {
            Console.WriteLine($"Error transcribing message: {result.Reason}");
        }
    }

    // Continuous recognition from the default microphone. Say "stop" to end.
    private static async Task FromMicrophoneWithSdkAsync()
    {
        Console.WriteLine("--- Speech SDK, from microphone. Say 'stop' to stop");
        var config = CreateSpeechConfig();
        config.SpeechRecognitionLanguage = "nl-NL";
        using var audio = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(config, audio);
        var stopped = new TaskCompletionSource();

        recognizer.Recognized += (s, e) =>
        {
            Console.WriteLine(e.Result.Text);
            if (e.Result.Text.StartsWith("stop", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Ok. Bye!");
                stopped.TrySetResult();
            }
        };
        recognizer.Canceled += (s, e) =>
        {
            Console.WriteLine($"Cancelled ({e.Reason})");
            if (e.ErrorDetails != null)
                Console.WriteLine($"- Reason: {e.ErrorDetails}");
            stopped.TrySetResult();
        };
        recognizer.SpeechStartDetected += (s, e) => Console.WriteLine("Hearing something...");
        recognizer.SpeechEndDetected += (s, e) => Console.WriteLine("All quiet?");

        await recognizer.StartContinuousRecognitionAsync();
        await stopped.Task;
        await recognizer.StopContinuousRecognitionAsync();
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Agent with the Azure Speech MCP tool (module 5): the prompt contains a link to the audio.
    // ---------------------------------------------------------------------------------------------
    private static async Task FromUrlWithAgentAsync()
    {
        Console.WriteLine("--- Agent, from URL");
        if (AUDIO_URL == "")
        {
            Console.WriteLine("Set AUDIO_URL to a link (with SAS token) of an audio file.");
            return;
        }

        var projectClient = new AIProjectClient(new Uri(PROJECT_ENDPOINT), new AzureCliCredential());
        var responses = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(AGENT_NAME);

        var response = (await responses.CreateResponseAsync($"Transcribe the file at {AUDIO_URL}")).Value;

        // The MCP tool may ask for approval first ("require_approval": "always").
        while (response.OutputItems.OfType<McpToolCallApprovalRequestItem>().FirstOrDefault() is { } request)
        {
            Console.WriteLine($"  approving tool call: {request.ToolName}");
            var options = new CreateResponseOptions { PreviousResponseId = response.Id };
            options.InputItems.Add(ResponseItem.CreateMcpApprovalResponseItem(request.Id, approved: true));
            response = (await responses.CreateResponseAsync(options)).Value;
        }

        Console.WriteLine(response.GetOutputText());
    }

    // WaveFileWriter closes its stream on dispose; keep the MemoryStream usable.
    private class IgnoreDisposeStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        protected override void Dispose(bool disposing) { }
    }
}
