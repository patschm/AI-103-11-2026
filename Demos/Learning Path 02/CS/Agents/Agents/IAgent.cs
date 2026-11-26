using Microsoft.Agents.AI;

namespace Agents;

public interface IAgent
{
    Task<AgentResponse> RunAsync(string input, AgentRunOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(string input, AgentRunOptions? options = null, CancellationToken cancellationToken = default);
    AIAgent ToAIAgent();
    Task CreateSessionAsync();
}