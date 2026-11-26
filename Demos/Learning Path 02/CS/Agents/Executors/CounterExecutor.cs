using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

public class CounterExecutor(string? id = null, ushort counterMax = 5) : Executor<object?, object?>(id ?? nameof(CounterExecutor))
{
    private int _counter = 0;
    public override async ValueTask<object?> HandleAsync(object? message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        if (++_counter < counterMax)
        {
            Console.WriteLine($"\r\n - {Id}'s counter is now {_counter}");
            await context.SendMessageAsync($"Value is now {_counter}");
        }
        else
        {
            Console.WriteLine($"\r\n - {Id}'s counter {_counter} is now equal to {counterMax}");
            await context.YieldOutputAsync("Kappuh");
        }
        Console.WriteLine("}");
        return null;
        
    }
}
