using Azure;
using Azure.AI.Translation.Text;
using Azure.Identity;

namespace Lang_07_Translate;

internal class Program
{
    // Foundry resource endpoint (Foundry portal > project > Keys and endpoint)
    const string TEXTTRANSLATOR_ENDPOINT = "https://ps-mijneerste-resource.cognitiveservices.azure.com/";

    static void Main(string[] args)
    {
        var client = new TextTranslationClient(
            new AzureCliCredential(),
            new Uri(TEXTTRANSLATOR_ENDPOINT));

        var response = client.GetSupportedLanguages(scope: "translation");
        ProcessSupportedLanguages(response);
        Console.WriteLine(new string('=', 80));

        string[] texts = ["This is a test", "C'est un test", "Das ist ein test", "C'est ein test"];

        foreach(var txt in texts)
        {
            var translation = client.Translate("ar", txt);
            foreach(var tran2 in translation.Value)
            {
                Console.WriteLine($"{txt} ({tran2.DetectedLanguage.Language} [{tran2.DetectedLanguage.Score}])");
                foreach(var txttran in tran2.Translations)
                {
                    Console.WriteLine($"- {txttran.Text}");
                }
            }
        }
        Console.WriteLine(new string('=', 80));
        string jap = "こんにちは";
        var resp = client.Transliterate("ja", "Jpan", "Latn", jap);
        foreach(var item in resp.Value)
        {
            Console.WriteLine($"{item.Text} : {item.Script}");
        }
    }

    private static void ProcessSupportedLanguages(GetSupportedLanguagesResult result)
    {
        Console.WriteLine("Supported langauges:");
        foreach(var lang in result.Translation)
        {
            Console.WriteLine($"{lang.Key}: {lang.Value.Name}");
        }
    }
}
