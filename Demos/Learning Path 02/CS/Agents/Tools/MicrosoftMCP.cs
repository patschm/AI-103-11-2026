using Azure.AI.Agents.Persistent;

namespace Agents.Tools;

public class MicrosoftMCP : MCPToolDefinition
{
    public MicrosoftMCP() : base("microsoft_learn", "https://learn.microsoft.com/api/mcp")
    {
        AllowedTools.Add("microsoft_docs_search");
    }
}
