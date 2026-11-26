using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

public class RandomBoolExecutor(string? id = null, int delay = 1000) : Executor<string, bool>(id ?? nameof(RandomBoolExecutor))
{
    public override async ValueTask<bool> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        bool b = Random.Shared.Next(0, 2) == 1;
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {b}");
        Console.WriteLine("}");
        await Task.Delay(delay);

        return b;
    }
}
