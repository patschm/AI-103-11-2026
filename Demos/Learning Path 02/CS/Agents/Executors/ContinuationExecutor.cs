
using Microsoft.Agents.AI.Workflows;

namespace Agents.Executors;

public class ContinuationExecutor(string? id = null) : Executor<bool, object?>(id ?? nameof(ContinuationExecutor))
{
    public override async ValueTask<object?> HandleAsync(bool canContinue, IWorkflowContext context, CancellationToken cancellationToken = default)
    {

        if (canContinue)
            await context.SendMessageAsync("En we gaan dooorrrrrr..");
        else
            await context.YieldOutputAsync("Kappuh");
            
        return null;
    }
}
