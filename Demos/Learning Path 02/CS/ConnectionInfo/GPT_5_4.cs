using Azure.Identity;
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;

namespace ConnectionInfo;

public class GPT_5_4 : AIModel
{
    [SetsRequiredMembers]
    public GPT_5_4()
        : base(
            "ps-agentfw-test",
            "gpt-5.4",   // deployment name: adjust to the name of your deployment
            "gpt-5.4")
    {
        // The key is read from an environment variable instead of being hardcoded.
        // DefaultAzureCredential (Entra ID) would be better still: no secret at all.
        string key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? throw new InvalidOperationException("Set the AZURE_OPENAI_API_KEY environment variable.");
        OPENAI_CREDENTIAL = new ApiKeyCredential(key);
        PROJECT_CREDENTIAL = new DefaultAzureCredential();
    }

    public override string ToString() => MODELNAME;
}
