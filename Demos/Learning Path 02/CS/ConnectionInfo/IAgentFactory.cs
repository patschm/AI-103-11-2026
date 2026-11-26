using Azure.AI.OpenAI;
using Azure.AI.Projects;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Responses;

namespace ConnectionInfo;

public interface IAgentFactory
{
    string MODELNAME { get; init; }
    string DEPLOYMENT_NAME { get; init; }
    // PersistentAgent is obsolete. Use AIProjectClient instead (Azure.AI.Projects)
    //PersistentAgentsAdministrationClient CreateProjectAdminAgent();
    AIProjectClient CreateProjectAgent();
    AudioClient CreateAudioAgent();
    ChatClient CreateChatAgent();
    ResponsesClient CreateResponsesClient();
    ImageClient CreateImageAgent();
    AzureOpenAIClient CreateOpenAIClient();
}