using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Agents.Orchestrators;

public class MAgentOrchestrator : GroupChatManager
{
    private readonly AIAgent _mainAgent;
    private readonly IReadOnlyList<AIAgent> _agents;
    private readonly Func<MAgentOrchestrator, IEnumerable<ChatMessage>, CancellationToken, ValueTask<bool>>? _shouldTerminate;

    public MAgentOrchestrator(
        AIAgent mainAgent,
        IReadOnlyList<AIAgent> agents,
        Func<MAgentOrchestrator, IEnumerable<ChatMessage>, CancellationToken, ValueTask<bool>>? shouldTerminate = null
        )
    {
        _mainAgent = mainAgent;
        _agents = agents;
        _shouldTerminate = shouldTerminate;
    }

    protected override async ValueTask<AIAgent> SelectNextAgentAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        //var resulut = await _mainAgent.RunAsync(history, cancellationToken);
        throw new NotImplementedException();

    }
}
