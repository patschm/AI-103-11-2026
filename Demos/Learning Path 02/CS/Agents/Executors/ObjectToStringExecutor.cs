using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

/// <summary>
/// Adapter that converts any message (e.g. a DayOfWeek) to its string representation,
/// so it can be routed to executors that expect a string.
/// </summary>
public class ObjectToStringExecutor(string? id = null, int delay = 500) : Executor<object, string>(id ?? nameof(ObjectToStringExecutor))
{
    public override async ValueTask<string> HandleAsync(object message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        string result = message?.ToString() ?? string.Empty;
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {result}");
        await Task.Delay(delay, cancellationToken);
        return result;
    }
}
