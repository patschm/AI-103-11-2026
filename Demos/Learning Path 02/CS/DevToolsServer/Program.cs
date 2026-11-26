using DevToolsServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// MCP server "DevTools", communicating over stdio.
// stdout carries the MCP protocol, so all logging must go to stderr.
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<FileTools>()
    .WithTools<ShellTools>();

await builder.Build().RunAsync();
