using Azure.Identity;
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;

namespace ConnectionInfo;

public class GPT_5_2 : AIModel
{
    [SetsRequiredMembers]
    public GPT_5_2()
        : base(
            "ps-mijneerste",
            "gpt-5.2",   // deployment name: adjust to the name of your deployment
            "gpt-5.2")
    {
        // The key is read from an environment variable instead of being hardcoded.
        // DefaultAzureCredential (Entra ID) would be better still: no secret at all.
        string key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? throw new InvalidOperationException("Set the AZURE_OPENAI_API_KEY environment variable.");
        OPENAI_CREDENTIAL = new ApiKeyCredential(key);
        PROJECT_CREDENTIAL = new AzureCliCredential();
    }

    public override string ToString() => MODELNAME;
}
