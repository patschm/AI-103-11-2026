using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;

namespace Demo_02_DocumentIntelligence;

public class Program
{
    // Configuration
    private const string ENDPOINT = "https://ps-documents.cognitiveservices.azure.com/";

    // Analysis settings
    private const string FILE = "sample-invoice.pdf";
    private const string PREBUILTMODELID = "prebuilt-invoice";

    private const string CUSTOMFORM = "test1.jpg";
    private const string CUSTOMMODELID = "ps-custom";

    public static async Task Main(string[] args)
    {
        try
        {
           await PreBuilt();
           await Custom();
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[Azure Error]: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Unexpected Error]: {ex.Message}");
        }
    }

    private static async Task PreBuilt()
    {
        // Create the client
        var client = new DocumentIntelligenceClient(new Uri(ENDPOINT), new AzureCliCredential());

        // Analyze the invoice
        var data = await BinaryData.FromStreamAsync(System.IO.File.OpenRead(FILE));
        var options = new AnalyzeDocumentOptions(PREBUILTMODELID, data);
        options.Locale = "en-US";
        
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, options);
        var result = operation.Value;

        // Display invoice information to the user
        if (result.Documents.Count == 0)
        {
            Console.WriteLine("No invoice found.");
        }

        foreach (var invoice in result.Documents)
        {
            DisplayField(invoice, "VendorName", "Vendor Name");
            DisplayField(invoice, "CustomerName", "Customer Name");
            DisplayField(invoice, "InvoiceTotal", "Invoice Total");
        }

    }

    private static async Task Custom()
    {
        // Create the client
        var client = new DocumentIntelligenceClient(new Uri(ENDPOINT), new AzureCliCredential());

        // Analyze the form with the custom model
        // (the document type must be included in the types the custom model can analyze)
        var options = new AnalyzeDocumentOptions(CUSTOMMODELID, await BinaryData.FromStreamAsync(System.IO.File.OpenRead(CUSTOMFORM)));
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, options);
        var result = operation.Value;

        var index = 0;
        foreach (var document in result.Documents)
        {
            index++;
            Console.WriteLine($"--------Analyzing document #{index}--------");
            Console.WriteLine($"Document has type {document.DocumentType}");
            Console.WriteLine($"Document has confidence {document.Confidence}");
            Console.WriteLine($"Document was analyzed by model with ID {result.ModelId}");
            foreach (var (name, field) in document.Fields)
            {
                var value = field.ValueString ?? field.Content ?? "N/A";
                Console.WriteLine($"Found field '{name}' with value '{value}' and with confidence {field.Confidence}");
            }
        }

        Console.WriteLine("-----------------------------------");
        Console.WriteLine("\nAnalysis complete.\n");
    }

    private static void DisplayField(AnalyzedDocument invoice, string fieldName, string label)
    {
        if (!invoice.Fields.TryGetValue(fieldName, out var field)) return;

        if (field.FieldType == DocumentFieldType.Currency)
        {
            var currency = field.ValueCurrency;
            Console.WriteLine($"{label}: {currency.CurrencySymbol}{currency.Amount}, with confidence {field.Confidence}.");
        }
        else
        {
            Console.WriteLine($"{label}: {field.ValueString ?? field.Content}, with confidence {field.Confidence}.");
        }
    }
}
