using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

public class StringExecutor(string? id = null, int delay = 1000) : Executor<string, string>(id ?? nameof(StringExecutor))
{
    public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        message = $"{id} stringified: {message}";
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {message}");
        Console.WriteLine("}");
        await Task.Delay(delay);
        return $"{Id} stringified: {message}";
    }
}
