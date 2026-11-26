using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;

namespace Agents.Providers;

public class SolutionInfoContext: AIContextProvider
{
    private readonly ProviderSessionState<SolutionInfo> _solutionInfoState;
    private readonly IChatClient _chatClient;

    public SolutionInfoContext(IChatClient chatClient, Func<AgentSession?, SolutionInfo?>? storeInitializer = null)
    {
        _chatClient = chatClient;
        if (storeInitializer == null) storeInitializer = _ => new SolutionInfo();
        _solutionInfoState = new ProviderSessionState<SolutionInfo>(storeInitializer!, nameof(SolutionInfoContext));
    }

    public SolutionInfo? GetProjectInfo(AgentSession? session) => _solutionInfoState.GetOrInitializeState(session);
    public void SetProjectInfo(AgentSession? session, SolutionInfo solutionInfo) => _solutionInfoState.SaveState(session, solutionInfo);

    public override IReadOnlyList<string> StateKeys => [_solutionInfoState.StateKey];
    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (_solutionInfoState == null) return;

        var projectInfo = _solutionInfoState.GetOrInitializeState(context.Session);
        if (context.RequestMessages.Any(x => x.Role == ChatRole.User))
        {
            var result = await this._chatClient.GetResponseAsync<SolutionInfo>(
                context.RequestMessages,
                new ChatOptions { 
                    Instructions = "Extract the solution Name and Workfolder from the message if present." + 
                                          "Only extract this information if the info is currently unknown." +
                                          "Try to extract the ProjectInfo Properties Name and Folder and add them to the Projects collection",
                },
                cancellationToken: cancellationToken);
            projectInfo.WorkFolder ??= result.Result.WorkFolder;
            projectInfo.Name ??= result.Result.Name;
        }
        _solutionInfoState.SaveState(context.Session, projectInfo);
    }
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var solutionInfo = _solutionInfoState.GetOrInitializeState(context.Session);
        var bld = new StringBuilder();
        if (solutionInfo.Name != null)
        {
            bld.AppendLine($"The solution name is {solutionInfo.Name}.");
        }
        else if (solutionInfo.WorkFolder != null)
        {
            bld.AppendLine($"The solution's work folder is {solutionInfo.WorkFolder}.");
        }
        else if(solutionInfo.Projects.Any())
        {
            bld.AppendLine($"The solution has the following projects:");
            foreach(var project in solutionInfo.Projects)
            {
                bld.AppendLine($"- {project.Name} in folder {project.Folder}");
            }
        }
        else
        {
            bld.AppendLine("Ask the user friendly for the name of the solution");
        }
        return new ValueTask<AIContext>(new AIContext { Instructions = bld.ToString() });
    }

}
