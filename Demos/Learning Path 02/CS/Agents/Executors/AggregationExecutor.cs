using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Collections;
using System.Text;

namespace Agents.Executors;

public class AggregationExecutor(string? id = null, int nrOfParallelExecuters = 3): Executor<string, string>(id ?? nameof(AggregationExecutor))
{
    private List<string> total = new();

    public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        total.Add(message);
        if (total.Count == nrOfParallelExecuters)
        {
            var msgs = string.Join(Environment.NewLine, total);
            Console.WriteLine($"\r\n - Yielding output {msgs}");
            await context.YieldOutputAsync(msgs);
            return msgs;
        }
        
        Console.WriteLine("}");
        return "";
    }
}
