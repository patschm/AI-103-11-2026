using ConnectionInfo;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Agents.Providers;

namespace Agents;

public abstract class BaseAgent : Executor<string, string>, IAgent
{
    private readonly ChatClient _chatClientAgent;
    protected static AgentSession? _agentSession = null;

    public BaseAgent(string id, AIModel model): base(id)
    {
        _chatClientAgent = model.CreateChatAgent();
    }

    public string? Name { get; set; }
    protected virtual AIAgent ToAIAgent(string? instructions = null, params AITool[] tools)
    {
        var chatOptions = new ChatOptions
        {
           Instructions = instructions,
           Tools = tools   
        };
        var options = new ChatClientAgentOptions
        {
            ChatOptions = chatOptions,
            Name = Name ?? "Default Agent",
            AIContextProviders = [new SolutionInfoContext(_chatClientAgent.AsIChatClient())],
        };
        return _chatClientAgent.AsAIAgent(options);
    }

    public abstract AIAgent ToAIAgent();

    public Task<AgentResponse> RunAsync(string input, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        var agent = ToAIAgent();
        return agent.RunAsync(input, _agentSession, options, cancellationToken);
    }
    public IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(string input, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        var agent = ToAIAgent();
        return agent.RunStreamingAsync(input, _agentSession, options, cancellationToken);
    }

    public async Task CreateSessionAsync()
    {
        if (_agentSession == null)
        {
            _agentSession = await _chatClientAgent.AsAIAgent().CreateSessionAsync();
        }
    }

    public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        
        if (_agentSession == null)
        {
            await CreateSessionAsync();
        }
        var response = await RunAsync(message, cancellationToken: cancellationToken);
        return response.Text;
    }
}
