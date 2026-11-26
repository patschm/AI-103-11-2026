using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Agents.Executors;

[SendsMessage(typeof(ChatMessage))]
[SendsMessage(typeof(TurnToken))]
public class String2ChatMessageExecuter(string? id = null, int delay = 500) : Executor<object>(id ?? nameof(String2ChatMessageExecuter))
{
    public override async ValueTask HandleAsync(object message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({message})");
        Console.WriteLine("{");
        // Next agents in the workflow might be real agents that expect messages in a conversational format with a User role.
        var msg = new ChatMessage(ChatRole.User, $"40 years ago from now {DateTime.Now.ToShortDateString()} what was the number 1 hit in the dutch top 40?");
        Console.WriteLine($"\r\n - {Id} is waiting {delay}ms and will return {msg}");
        Console.WriteLine("}");
        await Task.Delay(delay);

        // It's important to send the message here, before sending the TurnToken,
        // to ensure that the message is in the workflow context and ready to be processed when the turn token is received.
        // If not doing this you'll get the response from the system prompt because that is cached in the agent.
        await context.SendMessageAsync(msg, cancellationToken: cancellationToken);
        
        // Real agents will process the message immediately and respond,
        // but since we're using a mock agent, we need to send a TurnToken to trigger
        // the processing of the accumulated messages in the workflow (hence AgentKickExecutor).
        // Send a turn token to signal the agent to process the accumulated messages
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken);
        
    }
}
