using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

public class HandleBoolExecutor(string? id = null, int delay = 2000) : Executor<bool, string>(id ?? nameof(HandleBoolExecutor))
{
    public override async ValueTask<string> HandleAsync(bool message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        string msg = $"{Id} says {message.ToString()}";
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {msg}");
        Console.WriteLine("}");
        await Task.Delay(delay);
        return msg;
    }
}
