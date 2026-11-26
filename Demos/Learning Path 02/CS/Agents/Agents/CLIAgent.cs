using Agents.Providers;
using Agents.Tools;
using Azure.AI.Agents.Persistent;
using ConnectionInfo;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.Text.Json;


namespace Agents;

public class CLIAgent : BaseAgent
{
    public CLIAgent(AIModel model): base("cli-agent", model)
    {
    }
    public override AIAgent ToAIAgent()
    {
        var mcp = new MicrosoftMCP();
           
        return base.ToAIAgent(
            instructions: "You are a helpful assistant that executes dotnet CLI commands to manage .NET projects. " +
            "You will be provided with a command to run and a path to the solution folder. If none is given, generate one." + 
            "Use this solution folder every time." +
            "Try to extract project information such as Name and project folder. If none is provided generate one." +
            "Your task is to execute the commands and return the output or any errors that occur." +
            "Always explain what you are doing",
            tools: [
                mcp.AsAITool(), 
                AIFunctionFactory.Create(ShellTools.ExecuteDotnetCommand), 
                AIFunctionFactory.Create(ShellTools.ExecuteShellCommand)]
        );
    }
    override public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var agent = ToAIAgent();
        var state = agent.GetService<SolutionInfoContext>();
        if (state != null)
        {
           var info = state.GetProjectInfo(_agentSession);
            Console.WriteLine($"Solution: {info?.Name}, Work folder: {info?.WorkFolder}");
        }
        var response = await RunAsync(message, cancellationToken: cancellationToken);
        return response.Text;
    }
}
