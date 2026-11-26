using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Starts a workflow that was built in the Foundry portal (exercise 06: ContosoPay customer support triage).
// A Foundry workflow is invoked like an agent: reference it by name and send it an input.

// Explicit project endpoint. In the Foundry portal open the workflow, select Code > .env variables
// and copy AZURE_EXISTING_AIPROJECT_ENDPOINT. Replace the placeholders below.
Uri projectEndpoint = new("https://<your-resource>.services.ai.azure.com/api/projects/<your-project>");

// The name of the workflow as saved in the Foundry portal.
const string workflowName = "ContosoPay-Customer-Support-Triage";

// Entra ID authentication: run 'az login' first.
AIProjectClient projectClient = new(projectEndpoint, new DefaultAzureCredential());

AIAgent workflow = projectClient.AsAIAgent(new AgentReference(workflowName));

// The session maps to a Foundry conversation that holds the workflow's messages.
AgentSession session = await workflow.CreateSessionAsync();
Console.WriteLine($"Starting workflow '{workflowName}'...");

// The workflow ignores the input text; "Start" just triggers it.
await foreach (AgentResponseUpdate update in workflow.RunStreamingAsync("Start", session))
{
    foreach (AIContent content in update.Contents)
    {
        if (content is TextContent text)
        {
            Console.Write(text.Text);
        }
        else if (content is ErrorContent error)
        {
            Console.WriteLine($"{Environment.NewLine}Error: {error.Message}");
        }
    }
}

Console.WriteLine($"{Environment.NewLine}Workflow completed.");
