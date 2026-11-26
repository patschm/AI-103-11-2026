using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

public class StateConsumerExecutor(string? id = null, int delay = 1000) : Executor<string, string>(id ?? nameof(StateConsumerExecutor))
{
    public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        var state = await context.ReadStateAsync<string>("status", "myscope");
        message = $"{Id} Read data from {message}: {state}";
        Console.WriteLine(message);
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {message}");
        Console.WriteLine("}");
        await Task.Delay(delay);
        return $"{message}";
    }
}
