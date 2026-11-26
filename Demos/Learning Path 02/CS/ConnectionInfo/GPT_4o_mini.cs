using Azure;
using Azure.Identity;
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;

namespace ConnectionInfo;

public class GPT_4o_mini : AIModel
{
    [SetsRequiredMembers]
    public GPT_4o_mini() 
        : base(
            "ps-agentfw-test",
            "gpt-4o-mini", 
            "gpt-4o-mini")
    {
        // NOTE: A hardcoded API key is insecure. DefaultAzureCredential (Entra ID) would be better:
        // no secret in source code, and access is controlled through Azure RBAC.
        OPENAI_CREDENTIAL = new ApiKeyCredential("2HDYJDOqxdjwaJWpd7ShUOyG3dxvFDGA8WXMMgvtzcZI66puerjmJQQJ99CAACfhMk5XJ3w3AAAAACOGrc90");
        PROJECT_CREDENTIAL = new DefaultAzureCredential();
    }

    public override string ToString() => MODELNAME;
}
