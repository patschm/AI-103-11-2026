using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Agents.Executors;

public class RandomDayExecuter(string? id = null, int delay = 500) : Executor<string, DayOfWeek>(id ?? nameof(RandomDayExecuter))
{
    public override async ValueTask<DayOfWeek> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        DayOfWeek day = (DayOfWeek)Random.Shared.Next(0, 7);
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {day}");
        Console.WriteLine("}");
        await Task.Delay(delay);
        return day;
    }
}
