using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

public class StateProducerExecutor(string? id = null, int delay = 1000) : Executor<string, string>(id ?? nameof(StateProducerExecutor))
{
    public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        await context.QueueStateUpdateAsync("status", $"Status from {Id} at {DateTime.Now}", "myscope");
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms");
        Console.WriteLine("}");
        await Task.Delay(delay);
        return Id;
    }
}
