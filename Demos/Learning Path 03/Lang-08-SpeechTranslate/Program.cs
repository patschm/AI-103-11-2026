using Azure;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace Lang_08_SpeechTranslate;

// Learn module 7: Translate text and speech - speech translation
internal class Program
{
    // Foundry resource endpoint and key (Foundry portal > project > Keys and endpoint)
    const string SPEECH_ENDPOINT = "https://ps-mijneerste-resource.cognitiveservices.azure.com/";
    const string SPEECH_ENDPOINT2 = "https://ps-mijneerste-resource.services.ai.azure.com/";
    const string SPEECH_KEY = "3PuxSoiorcTGbqYbCpX7Y2VtlJEtgM3KZAhntqYdYJmR867V6BYFJQQJ99CKACfhMk5XJ3w3AAAAACOGXniN";

    static async Task Main(string[] args)
    {
        // SpeechTranslationConfig holds the connection and the languages
        var tranConfig = SpeechTranslationConfig.FromEndpoint(
            new Uri(SPEECH_ENDPOINT2),
            new AzureKeyCredential(SPEECH_KEY));

        tranConfig.SpeechRecognitionLanguage = "en-US";
        tranConfig.AddTargetLanguage("fr");
        tranConfig.AddTargetLanguage("es");
        tranConfig.AddTargetLanguage("nl");
        //tranConfig.AddTargetLanguage("ar");

        // Speech to text: translate the speech in the wav file
        var translations = await FromWavAsync(tranConfig);

        // Manual synthesis (needed for more than one target language):
        // use text to speech for each translation
        var config = SpeechConfig.FromEndpoint(new Uri(SPEECH_ENDPOINT2), new AzureKeyCredential(SPEECH_KEY));
        await TranslationToSpeechAsync(config, translations);

        Console.ReadLine();
    }

    private static async Task<IReadOnlyDictionary<string, string>> FromWavAsync(SpeechTranslationConfig tranConfig)
    {
        using var audio = AudioConfig.FromWavFileInput("station.wav");
        using var recognizer = new TranslationRecognizer(tranConfig, audio);

        var result = await recognizer.RecognizeOnceAsync();
        Console.WriteLine($"Translated: {result.Text}");
        foreach (var tran in result.Translations)
        {
            Console.WriteLine($"[{tran.Key}] {tran.Value}");
        }
        return result.Translations;
    }

    private static async Task TranslationToSpeechAsync(SpeechConfig config, IReadOnlyDictionary<string, string> translations)
    {
        (string Code, string Name)[] voices = [("fr", "fr-FR-HenriNeural"), ("es", "es-ES-ElviraNeural"), ("nl", "nl-NL-ColetteNeural")];

        using var audio = AudioConfig.FromDefaultSpeakerOutput();
        foreach (var item in translations)
        {
            var voice = voices.First(v => v.Code == item.Key);
            //using var audio = AudioConfig.FromWavFileOutput($"station-{voice.Code}.wav");
            config.SpeechSynthesisVoiceName = voice.Name;
            using var synth = new SpeechSynthesizer(config, audio);
            synth.SynthesisCanceled += HandleError;
            await synth.SpeakTextAsync(item.Value);
            synth.SynthesisCanceled -= HandleError;
        }

        void HandleError(object? sender, SpeechSynthesisEventArgs arg)
        {
            Console.WriteLine($"Error: {arg.Result.Reason}");
        }
    }
}
