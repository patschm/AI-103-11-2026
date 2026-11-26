using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace Demo_03_KnowledgeMining;

public class Program
{
    // Configuration
    private const string SearchEndpoint = "https://ps-zoekt.search.windows.net";
    private const string IndexName = "index-last-v2";
    private const string QueryKey = "7Egxm3dsNm27xSAQY0ZnyQqeahxUd88HUCWGoz2WH4AzSeBwNaWo";

    public static async Task Main(string[] args)
    {
        await Search();
    }

    private static async Task Search()
    {
        var searchClient = new SearchClient(new Uri(SearchEndpoint), IndexName, new AzureKeyCredential(QueryKey));

        while (true)
        {
            // Get query text
            Console.Write("Enter a query (or type 'quit' to exit): ");
            var queryText = Console.ReadLine() ?? "quit";
            if (queryText.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;
            if (queryText.Length == 0)
            {
                Console.WriteLine("Please enter a query.");
                continue;
            }

            if (!Console.IsOutputRedirected) Console.Clear();

            // Search the index
            var options = new SearchOptions
            {
                IncludeTotalCount = true
            };
            options.Select.Add("title");
            options.Select.Add("locations");
            options.Select.Add("persons");
            options.Select.Add("keyPhrases");

            var response = await searchClient.SearchAsync<SearchDocument>(queryText, options);

            // Parse the results
            Console.WriteLine($"\nSearch returned {response.Value.TotalCount} documents:");
            // 'title' is not sortable in the index, so order the results here
            var documents = new List<SearchDocument>();
            await foreach (var result in response.Value.GetResultsAsync())
                documents.Add(result.Document);

            foreach (var document in documents.OrderBy(d => d["title"]?.ToString()))
            {
                Console.WriteLine($"\nDocument: {document["title"]}");
                PrintList(" - Locations:", document["locations"]);
                PrintList(" - People:", document["persons"]);
                PrintList(" - Key phrases:", document["keyPhrases"]);
            }
        }
    }

    private static void PrintList(string heading, object? values)
    {
        Console.WriteLine(heading);
        if (values is IEnumerable<object> items)
        {
            foreach (var item in items)
                Console.WriteLine($"   - {item}");
        }
    }
}
