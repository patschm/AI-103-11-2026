using Azure;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Lang_04_SpeechSdk;

// Learn module 4: Create speech-enabled apps with Azure Speech in Foundry Tools
internal class Program
{
    // Foundry resource endpoint and key (Foundry portal > project > Keys and endpoint).
    // Speech SDK 1.48.2 and later connect with an endpoint instead of a region.
    const string SPEECH_ENDPOINT = "https://ps-mijneerste-resource.cognitiveservices.azure.com/";
    const string SPEECH_KEY = "";

    static async Task Main(string[] args)
    {
        var config = SpeechConfig.FromEndpoint(new Uri(SPEECH_ENDPOINT), new AzureKeyCredential(SPEECH_KEY));

        await FromFileAsync(config);          // speech to text, from a wav file
        //await FromMicrophoneAsync(config);  // speech to text, continuous, from the microphone
        //await SpeakAsync(config);           // text to speech, default speaker
        //await SpeakWithVoiceAsync(config);  // text to speech, voice and audio format
        //await ListVoicesAsync(config);      // which voices exist for a locale
        //await SpeakSsmlAsync(config);       // text to speech with SSML markup

        Console.ReadLine();
    }

    // Unit 3: speech to text
    private static async Task FromFileAsync(SpeechConfig config)
    {
        using var audio = AudioConfig.FromWavFileInput("time.wav");
        using var recognizer = new SpeechRecognizer(config, audio);

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

    // Unit 3: continuous recognition from the default microphone. Say "stop" to end.
    private static async Task FromMicrophoneAsync(SpeechConfig config)
    {
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
        Console.WriteLine("Say 'stop' to stop");
        await stopped.Task;
        await recognizer.StopContinuousRecognitionAsync();
    }

    // Unit 4: text to speech on the default speaker
    private static async Task SpeakAsync(SpeechConfig config)
    {
        using var audio = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(config, audio);

        var text = "My voice is my password!";
        var result = await synthesizer.SpeakTextAsync(text);
        ShowSynthesisResult(result, text);
    }

    // Unit 5: choose the output format and the voice on the SpeechConfig
    private static async Task SpeakWithVoiceAsync(SpeechConfig config)
    {
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
        config.SpeechSynthesisVoiceName = "en-US-Brian:DragonHDLatestNeural";

        using var audio = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(config, audio);

        var text = "The rain in Spain stays mainly in the plain";
        var result = await synthesizer.SpeakTextAsync(text);
        ShowSynthesisResult(result, text);
    }

    // Unit 5: list the voices for a locale
    private static async Task ListVoicesAsync(SpeechConfig config, string locale = "nl-NL")
    {
        using var synthesizer = new SpeechSynthesizer(config, null as AudioConfig);
        var result = await synthesizer.GetVoicesAsync(locale);
        Console.WriteLine($"Voices for {locale}:");
        foreach (var voice in result.Voices)
        {
            Console.WriteLine($"{voice.ShortName} ({voice.Gender})");
        }
    }

    // Unit 6: Speech Synthesis Markup Language (styles, voices, phonemes, pauses)
    private static async Task SpeakSsmlAsync(SpeechConfig config)
    {
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
        using var synthesizer = new SpeechSynthesizer(config, audio);
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
}
