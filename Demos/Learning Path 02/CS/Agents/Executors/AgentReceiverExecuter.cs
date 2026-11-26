using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;

namespace Agents.Executors;

public class AgentReceiverExecuter(string? id = null, int delay = 500) : Executor<IList<ChatMessage>, string>(id ?? nameof(AgentReceiverExecuter))
{
    public override async ValueTask<string> HandleAsync(IList<ChatMessage> messages, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({messages})");
        Console.WriteLine("{");
        var bld = new StringBuilder();
        foreach (var item in messages)
        {
            bld.AppendLine($"[{item.Role.Value}]:");
            
            foreach (AIContent item2 in item.Contents)
            {
                if (item2 is TextContent textContent)
                {
                    bld.Append(textContent.Text);
                }
                
            }
            bld.AppendLine();
        }
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {bld.ToString()}");
        Console.WriteLine("}");
        await Task.Delay(delay);

        return bld.ToString();
    }
}
