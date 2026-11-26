using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Responses;
using System.ClientModel;

namespace ConnectionInfo;

public class AIModel : IAgentFactory
{
    private readonly Uri _project_endpoint;
    private readonly Uri _openai_endpoint;
    private readonly string _deployment_name;
    private readonly ApiKeyCredential? _openai_credential;
    private readonly TokenCredential? _project_credential;
    private readonly string _model_name;

    public AIModel(string project_name, string deployment_name, string model_name)
    {
        _openai_endpoint = new Uri($"https://{project_name}-resource.openai.azure.com/");
        _project_endpoint = new Uri($"https://{project_name}-resource.services.ai.azure.com/api/projects/{project_name}");
        _deployment_name = deployment_name;
        _model_name = model_name;
    }
    public Uri OPENAI_ENDPOINT { get => _openai_endpoint; init => _openai_endpoint = value; }
    public Uri PROJECT_ENDPOINT { get => _project_endpoint; init => _project_endpoint = value; }
    public ApiKeyCredential? OPENAI_CREDENTIAL { get => _openai_credential; init => _openai_credential = value; }
    public TokenCredential? PROJECT_CREDENTIAL { get => _project_credential; init => _project_credential = value; }
    public string DEPLOYMENT_NAME { get => _deployment_name; init => _deployment_name = value; }
    public string MODELNAME { get => _model_name; init => _model_name = value; }
  
    public AzureOpenAIClient CreateOpenAIClient()
    {
        return new AzureOpenAIClient(OPENAI_ENDPOINT, OPENAI_CREDENTIAL);
    }
    public ChatClient CreateChatAgent()
    {
        var client = CreateOpenAIClient();
        return client.GetChatClient(DEPLOYMENT_NAME);

    }
    public ResponsesClient CreateResponsesClient()
    {
        // Azure.AI.OpenAI 2.9.0-beta.1 is built against OpenAI 2.9.x and breaks with the OpenAI 2.15
        // that the Foundry/Extensions packages require. Use the plain OpenAI client against Azure's /openai/v1/ endpoint.
        var options = new OpenAI.OpenAIClientOptions { Endpoint = new Uri(OPENAI_ENDPOINT, "openai/v1/") };
        return new OpenAI.OpenAIClient(OPENAI_CREDENTIAL, options).GetResponsesClient();
    }
    public ImageClient CreateImageAgent()
    {
        var client = CreateOpenAIClient();
        return client.GetImageClient(DEPLOYMENT_NAME);
    }
    public AudioClient CreateAudioAgent()
    {
        var client = CreateOpenAIClient();
        return client.GetAudioClient(DEPLOYMENT_NAME);
    }

    public AIProjectClient CreateProjectAgent()
    {
        if (_project_credential == null) throw new Exception("No TokenCredential specified. Provide one");
        var admin = new AIProjectClient(PROJECT_ENDPOINT, _project_credential);
        return admin;
    }

    public static implicit operator AzureOpenAIClient(AIModel modelInfo)
    {
        return new AzureOpenAIClient(modelInfo.OPENAI_ENDPOINT, modelInfo.OPENAI_CREDENTIAL);
    }
}
