using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

// MCP server "Inventory", communicating over stdio.
// stdout carries the MCP protocol, so all logging must go to stderr.
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<InventoryTools>();

await builder.Build().RunAsync();

[McpServerToolType]
public class InventoryTools
{
    [McpServerTool, Description("Returns current inventory for all products.")]
    public Dictionary<string, int> GetInventoryLevels() => new()
    {
        ["Moisturizer"] = 6,
        ["Shampoo"] = 8,
        ["Body Spray"] = 28,
        ["Hair Gel"] = 5,
        ["Lip Balm"] = 12,
        ["Skin Serum"] = 9,
        ["Cleanser"] = 30,
        ["Conditioner"] = 3,
        ["Setting Powder"] = 17,
        ["Dry Shampoo"] = 45,
    };

    [McpServerTool, Description("Returns number of units sold last week.")]
    public Dictionary<string, int> GetWeeklySales() => new()
    {
        ["Moisturizer"] = 22,
        ["Shampoo"] = 18,
        ["Body Spray"] = 3,
        ["Hair Gel"] = 2,
        ["Lip Balm"] = 14,
        ["Skin Serum"] = 19,
        ["Cleanser"] = 4,
        ["Conditioner"] = 1,
        ["Setting Powder"] = 13,
        ["Dry Shampoo"] = 17,
    };
}
