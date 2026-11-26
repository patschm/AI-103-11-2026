using ConnectionInfo;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace Agents.Executors;

//[SendsMessage(typeof(List<Microsoft.Extensions.AI.ChatMessage>))] //in conjuction with SendMessage
public class WrapAgentExecuter : Executor<string, string>
{
    private readonly AIAgent _agent;
    public WrapAgentExecuter(string? id = null, int delay = 500): base(id ?? nameof(WrapAgentExecuter))
    {
        var model = new GPT_5_2();
        _agent =  model.CreateChatAgent().AsAIAgent($"You are an Irish limeric specialist");
    }
    public override async ValueTask<string> HandleAsync(string subject, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"executor {Id}({subject})");
        Console.WriteLine("{");
        var response = await _agent.RunAsync($"Write a limerick about {subject}");
        //await context.SendMessageAsync(response.Messages, cancellationToken: cancellationToken);
        Console.WriteLine("}");
        return response.Text;
    }
}
