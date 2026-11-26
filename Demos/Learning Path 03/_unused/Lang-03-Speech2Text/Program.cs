using Azure.AI.OpenAI;
using Azure.Identity;
using NAudio.Wave;
using OpenAI.Audio;
using System.ClientModel;

namespace Lang_03_Speech2Text;

internal class Program
{
    // Models deployed in the Foundry project "ps-mijneerste" (resource ps-mijneerste-resource)
    const string ENDPOINT = "https://ps-mijneerste-resource.openai.azure.com/";
    //const string API_KEY = "";   // Foundry portal > project > Keys and endpoint
    const string TRANSCRIBE_DEPLOYMENT = "gpt-4o-mini-transcribe";
    const string TTS_DEPLOYMENT = "tts";

    static AudioClient transcribeClient = null!;
    static AudioClient ttsClient = null!;

    static async Task Main(string[] args)
    {
        var project = new AzureOpenAIClient(
            new Uri(ENDPOINT),
            new AzureCliCredential());
            //new ApiKeyCredential(API_KEY));
        transcribeClient = project.GetAudioClient(TRANSCRIBE_DEPLOYMENT);
        ttsClient = project.GetAudioClient(TTS_DEPLOYMENT);

        //await FromFileAsync();
        //await FromMicrophoneAsync();
        await SpeakupAsync();
        //await SpeakLongerAsync();

        Console.ReadLine();
    }

    // Speech to text from a wav file
    private static async Task FromFileAsync()
    {
        try
        {
            var result = await transcribeClient.TranscribeAudioAsync("time.wav");
            Console.WriteLine(result.Value.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cancelled ({ex.Message})");
        }
    }

    // Speech to text from the microphone: record a few seconds, then transcribe.
    // Say "stop" to end the loop.
    private static async Task FromMicrophoneAsync()
    {
        Console.WriteLine("Say 'stop' to stop");
        while (true)
        {
            Console.WriteLine("Listening (5 seconds)...");
            var wav = await RecordAsync(TimeSpan.FromSeconds(5));
            var options = new AudioTranscriptionOptions { Language = "nl" };
            var result = await transcribeClient.TranscribeAudioAsync(wav, "mic.wav", options);
            var text = result.Value.Text;
            Console.WriteLine(text);
            if (text.Trim().ToLower().StartsWith("stop"))
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

    // Text to speech: one clip per available voice
    private static async Task SpeakupAsync()
    {
        var text = "The rain in Spain stays mainly in the plain";
        Console.WriteLine(new string('=', 80));
        foreach (var voice in new[] { GeneratedSpeechVoice.Alloy, GeneratedSpeechVoice.Echo, GeneratedSpeechVoice.Fable,
                                      GeneratedSpeechVoice.Onyx, GeneratedSpeechVoice.Nova, GeneratedSpeechVoice.Shimmer })
        {
            Console.WriteLine(voice);
            await SpeakAsync(text, voice);
        }
    }

    // The tts model has no SSML support; punctuation controls the pauses.
    private static async Task SpeakLongerAsync()
    {
        var nu = DateTime.Now;
        var time = $"Bij de volgende toon is het {nu.Hour} uur, {nu.Minute} minuten en {nu.Second + 5} seconden. Biep!";
        await SpeakAsync($"{time} ... Tijd om er een punt achter te zetten.", GeneratedSpeechVoice.Nova);
    }

    private static async Task SpeakAsync(string text, GeneratedSpeechVoice voice)
    {
        var options = new SpeechGenerationOptions { ResponseFormat = GeneratedSpeechFormat.Wav };
        var result = await ttsClient.GenerateSpeechAsync(text, voice, options);

        using var ms = new MemoryStream(result.Value.ToArray());
        using var reader = new WaveFileReader(ms);
        using var output = new WaveOut();
        output.Init(reader);
        output.Play();
        while (output.PlaybackState == PlaybackState.Playing)
            await Task.Delay(100);
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
