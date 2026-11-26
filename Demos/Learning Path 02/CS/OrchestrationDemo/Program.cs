using Agents;
using Agents.Executors;
using ConnectionInfo;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using System.Text;

namespace OrchestrationDemo;

internal class Program
{
    static AIModel model = new GPT_5_2();
    static string path = @"D:\Gen";

    static async Task Main(string[] args)
    {

        //await CustomOrchestratorTestAsync();

        Console.WriteLine("Waiting to finish program...");
        Console.ReadLine();
    }
    static async Task GroupsOrchestratorTestAsync()
    {
        var strExec = new StringExecutor();
        var builder = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 2 })
            .AddParticipants();
    }

    static async Task HandsoffOrchestratorTestAsync()
    {
        var strExec = new StringExecutor();
        //var builder = AgentWorkflowBuilder
        //    .CreateHandoffBuilderWith(agents => new HandoffManager(agents))
        //    .AddParticipants();
    }
    private static async Task CustomOrchestratorTestAsync()
    {
        var requirements = @$"
            Create a console application in .net.
            the application should perform easy calculations (add, subtract, multiply divide) and print the result to the console.
            The mathematical operation and the numbers should be defined in a separate class and the console application should use that class to perform the calculations.
            The application should be named ConsoleCalculator.
            The location where the application should be created is {path}.";

        var planAgent = new PlanningAgent(model);
        var cliAgent = new CLIAgent(model);
        var codeAgent = new CodeAgent(model);

        await cliAgent.CreateSessionAsync();


        //GroupChatManager manager = new GroupChatManager(model);
        //var builder = AgentWorkflowBuilder.CreateGroupChatBuilderWith(agents => manager);

        var builder = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 2 })
            .AddParticipants(
                planAgent.ToAIAgent(), 
                cliAgent.ToAIAgent(), 
                codeAgent.ToAIAgent());

        var workflow = builder.Build();

        Console.WriteLine(workflow.ToMermaidString());
        Run run = await InProcessExecution.RunAsync(workflow, requirements);
        await ExamineRunAsync(run);
    }
    static async Task ExamineRunAsync(Run? run)
    {
        if (run == null) return;

        foreach (var evt in run.NewEvents)
        {
            switch (evt)
            {
                case ExecutorCompletedEvent comp:
                    Console.WriteLine($"Message handled by {comp.ExecutorId}: {comp.Data}");
                    break;
                default:
                    Console.WriteLine($"Event: {evt.GetType().Name}");
                    break;
            }
        }
    }
}
