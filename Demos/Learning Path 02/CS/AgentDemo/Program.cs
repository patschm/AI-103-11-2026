#pragma warning disable OPENAI001 // Responses API types are evaluation-only
using Anthropic;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using ConnectionInfo;
using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;
using OpenAI.Chat;
using OpenAI.Responses;
using System.Net;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

//using OpenAI.Responses;

namespace AgentDemo;

internal class Program
{
    static async Task Main(string[] args)
    {
       //await SimpleAgentAsync();
        //await FoundryPortalAgentAsync();
        await AgentWithToolsAsync();
        //await AgentWithDevToolsMcpAsync();
         //await AgentWithMCPAsync();
        //await AgentWithGitHubMcpAsync();
        //await OtherProvidersAsync();
        Console.ReadLine();
    }
    static async Task SimpleAgentAsync()
    {
        IAgentFactory model = new GPT_5_2();

        // Completely verbose way to create an agent, without using the helper method CreateAIChatAgent() in AIModel
        //AIAgent agent = new AzureOpenAIClient(model.OPENAI_ENDPOINT, model.OPENAI_KEY)
        //    .GetChatClient(model.DEPLOYMENT_NAME)
        //    .AsAIAgent();
        AIAgent agent = model.CreateChatAgent()
            .AsAIAgent(instructions: "you are a travel agent specialized in city trips");
        // For multi-turn conversations, you need to create a session. This is where the conversation history will be stored.
        var session = await agent.CreateSessionAsync();

        string? userInput = GetUserInput("Describe your favorite destination");
        for (int i = 0; i < 5; i++)
        {
            var resp = await agent.RunAsync(userInput, session);
            Console.WriteLine(resp.Text);
            userInput = GetUserInput("");
        }

    }
    // Runs an agent that was created in the Foundry portal
    static async Task FoundryPortalAgentAsync(string agentName = "my-first-agent")
    {
        Uri projectEndpoint = new("https://ps-mijneerste-resource.services.ai.azure.com/api/projects/ps-mijneerste");
        AIProjectClient projectClient = new(projectEndpoint, new AzureCliCredential());

        // Reference the existing agent by name. Add a version to pin one, otherwise the latest is used.
        AIAgent agent = projectClient.AsAIAgent(new AgentReference(agentName));
        var session = await agent.CreateSessionAsync();

        Console.WriteLine($"Connected to Foundry agent '{agentName}'. Empty line to quit.");
        while (true)
        {
            Console.Write("You: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                break;
            }
            await foreach (var update in agent.RunStreamingAsync(input, session))
            {
                Console.Write(update.Text);
            }
            Console.WriteLine();
        }
    }
    private static async Task AgentWithToolsAsync()
    {
        // var browsertool = new HostedWebSearchTool(); // Requires an Azure OpenAI deployment with browsing tool enabled
        // Hosted MCP tools are executed by the service, which requires the Responses API.
        var mcp = new HostedMcpServerTool(
            serverName: "microsoft_learn",
            serverAddress: "https://learn.microsoft.com/api/mcp")
        {
            AllowedTools = ["microsoft_docs_search"],
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire // read-only docs search
        };

        // Tools with side effects require human approval; read-only tools do not.
        AITool[] tools =
        [
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ShellTools.ExecuteDotnetCommand)),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(FileTools.WriteFileAsync)),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(FileTools.EditFileAsync)),
            AIFunctionFactory.Create(FileTools.ReadFileAsync)
        ];

        IAgentFactory model = new GPT_5_2();
        var agent = model.CreateResponsesClient()
            .AsIChatClient(model.DEPLOYMENT_NAME)
            .AsAIAgent(
            instructions: @"You are a dotnet developer who runs commands on a local machine.
                                  You should be able to run the dotnet command to create projects.
                                  Always explain what you are doing.",
            tools: [mcp, .. tools]);

        string? userInput = @"Create a .NET console application that asks a user for input and writes the user input to the console. 
                                         Use C# as the programming language.
                                         You don't need to run the code, just create the project and code.
                                         If project exists, edit the existing code to match the requirements.
                                         write project to D:\Gen";
        var session = await agent.CreateSessionAsync();
        List<ChatMessage> input = [new ChatMessage(ChatRole.User, userInput)];

        // Keep running until the agent finishes without asking for approval.
        while (true)
        {
            List<ToolApprovalRequestContent> approvalRequests = [];
            var result = agent.RunStreamingAsync(messages: input, session: session);

            await foreach (var response in result)
            {
            foreach (AIContent content in response.Contents)
            {
                if (content is ToolApprovalRequestContent approval)
                {
                    approvalRequests.Add(approval);
                }
                // AIContent can be of type:
                //      - DataContent, "data"
                //      - ErrorContent, "error"
                //      - FunctionCallContent, "functionCall"
                //      - FunctionResultContent, "functionResult"
                //      - HostedFileContent, "hostedFile"
                //      - HostedVectorStoreContent, "hostedVectorStore"
                //      - TextContent, "text"
                //      - TextReasoningContent, "reasoning"
                //      - UriContent, "uri"
                //      - UsageContent, "usage"
                //      - ToolCallContent, "toolCall"
                //      - ToolResultContent, "toolResult"
                //      - InputRequestContent, "inputRequest"
                //      - InputResponseContent, "inputResponse"
                //      - ToolApprovalRequestContent, "toolApprovalRequest"
                //      - ToolApprovalResponseContent, "toolApprovalResponse"
                //      - McpServerToolCallContent, "mcpServerToolCall"
                //      - McpServerToolResultContent, "mcpServerToolResult"
                if (content is TextContent c)
                {
                    Console.Write(c.Text);
                }
                if (content is FunctionCallContent f)
                {
                    Console.WriteLine($"Tool called: {f.Name} with arguments {f.Arguments}");
                }
                if (content is FunctionResultContent fr)
                {
                    Console.WriteLine($"Tool response: {fr.Result}");
                }
                if (content is UriContent u)
                {
                    Console.WriteLine($"Tool Uri: {u.Uri}");
                }
            }
            }

            if (approvalRequests.Count == 0)
            {
                break;
            }

            // Ask the user to approve or reject each pending tool call, then continue the run.
            List<AIContent> answers = [];
            foreach (var request in approvalRequests)
            {
                string call = request.ToolCall is FunctionCallContent fc
                    ? $"{fc.Name}({string.Join(", ", fc.Arguments?.Select(a => $"{a.Key}={a.Value}") ?? [])})"
                    : request.ToolCall.ToString() ?? "tool call";
                Console.WriteLine();
                Console.Write($"Approve {call}? (y/n): ");
                bool approved = string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
                answers.Add(request.CreateResponse(approved));
            }
            input = [new ChatMessage(ChatRole.User, answers)];
        }
    }
    // Same four tools as AgentWithToolsAsync, but served by the DevToolsServer MCP project over stdio.
    private static async Task AgentWithDevToolsMcpAsync()
    {
        await using var mcpDevTools = await McpClient.CreateAsync(new StdioClientTransport(new()
        {
            Name = "DevTools",
            Command = "dotnet",
            // DevToolsServer.dll is copied next to this app through the project reference.
            Arguments = [Path.Combine(AppContext.BaseDirectory, "DevToolsServer.dll")],
        }));

        var mcpTools = await mcpDevTools.ListToolsAsync();
        Console.WriteLine($"Connected to server with tools: {string.Join(", ", mcpTools.Select(t => t.Name))}");

        // Tools with side effects require human approval; read-only tools do not.
        AITool[] tools = [.. mcpTools.Select(t => t.Name == "read_file" ? (AITool)t : new ApprovalRequiredAIFunction(t))];

        IAgentFactory model = new GPT_5_2();
        var agent = model.CreateResponsesClient()
            .AsIChatClient(model.DEPLOYMENT_NAME)
            .AsAIAgent(
            instructions: @"You are a dotnet developer who runs commands on a local machine.
                                  You should be able to run the dotnet command to create projects.
                                  Always explain what you are doing.",
            tools: tools);

        string userInput = @"Create a .NET console application that asks a user for input and writes the user input to the console.
                                         Use C# as the programming language.
                                         You don't need to run the code, just create the project and code.
                                         If project exists, edit the existing code to match the requirements.
                                         write project to D:\Gen";
        var session = await agent.CreateSessionAsync();
        List<ChatMessage> input = [new ChatMessage(ChatRole.User, userInput)];

        // Keep running until the agent finishes without asking for approval.
        while (true)
        {
            List<ToolApprovalRequestContent> approvalRequests = [];
            await foreach (var update in agent.RunStreamingAsync(messages: input, session: session))
            {
                foreach (AIContent content in update.Contents)
                {
                    switch (content)
                    {
                        case ToolApprovalRequestContent approval: approvalRequests.Add(approval); break;
                        case TextContent c: Console.Write(c.Text); break;
                        case FunctionCallContent f: Console.WriteLine($"Tool called: {f.Name}"); break;
                        case FunctionResultContent fr: Console.WriteLine($"Tool response: {fr.Result}"); break;
                    }
                }
            }

            if (approvalRequests.Count == 0)
            {
                break;
            }

            List<AIContent> answers = [];
            foreach (var request in approvalRequests)
            {
                string call = request.ToolCall is FunctionCallContent fc
                    ? $"{fc.Name}({string.Join(", ", fc.Arguments?.Select(a => $"{a.Key}={a.Value}") ?? [])})"
                    : request.ToolCall.ToString() ?? "tool call";
                Console.WriteLine();
                Console.Write($"Approve {call}? (y/n): ");
                bool approved = string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
                answers.Add(request.CreateResponse(approved));
            }
            input = [new ChatMessage(ChatRole.User, answers)];
        }
    }

    // Local MCP server (see the InventoryServer project) started over stdio.
    // McpClient tools are AIFunctions: their schemas and invocation are handled automatically.
    private static async Task AgentWithMCPAsync()
    {
        await using var mcpInventory = await McpClient.CreateAsync(new StdioClientTransport(new()
        {
            Name = "Inventory",
            Command = "dotnet",
            // InventoryServer.dll is copied next to this app through the project reference.
            Arguments = [Path.Combine(AppContext.BaseDirectory, "InventoryServer.dll")],
        }));

        var inventoryTools = await mcpInventory.ListToolsAsync();
        Console.WriteLine($"Connected to server with tools: {string.Join(", ", inventoryTools.Select(t => t.Name))}");

        IAgentFactory model = new GPT_5_2();
        var agent = model.CreateChatAgent().AsAIAgent(
            instructions: @"You are an inventory assistant. Here are some general guidelines:
                - Recommend restock if item inventory < 10 and weekly sales > 15
                - Recommend clearance if item inventory > 20 and weekly sales < 5",
            tools: [.. inventoryTools.Cast<AITool>()]);
        var session = await agent.CreateSessionAsync();

        foreach (string question in new[]
        {
            "Show me the current inventory levels for all products.",
            "Are there any products that should be restocked?",
            "Which products would you recommend for clearance?",
        })
        {
            Console.WriteLine($"{Environment.NewLine}YOU: {question}");
            Console.WriteLine($"AGENT: {(await agent.RunAsync(question, session)).Text}");
        }
    }

    // Requires Node.js (npx) and a GITHUB_PERSONAL_ACCESS_TOKEN environment variable.
    private static async Task AgentWithGitHubMcpAsync()
    {
        await using var mcpGitHub = await McpClient.CreateAsync(new StdioClientTransport(new()
        {
            Name = "GitHub",
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-github"],
        }));

        var githubTools = await mcpGitHub.ListToolsAsync();
        foreach (var tool in githubTools)
        {
            Console.WriteLine($"GitHub tool available: {tool.Name} ({tool.Description})");
        }

        IAgentFactory model = new GPT_5_2();
        var agent = model.CreateChatAgent().AsAIAgent(
            instructions: "You answer questions related to GitHub repositories only.",
            tools: [.. githubTools.Cast<AITool>()]);

        Console.WriteLine(await agent.RunAsync("Summarize the last four commits to the microsoft/agent-framework repository?"));
    }
    private static async Task OtherProvidersAsync()
    {
        string instruction = "You are good at telling limerics.";
        string name = "LimerickMaster";

        // OllamaSharp
        AIAgent agent1 = new OllamaApiClient(new Uri("https://bla.ai"), "modelName")
            .AsAIAgent(instructions: instruction, name: name);

        // Google GenAI
        var agent2 = new Client(apiKey: "your_api_key", vertexAI: false)
            .AsIChatClient("gemini-2.5-flash").AsAIAgent(
                instructions: instruction,
                name: name
            );

        // Anthropic
        AnthropicClient aClient = new AnthropicClient
        {
            ApiKey = "your_api_key"
        };
        var agent3 = aClient
            .AsIChatClient("claude-haiku-4-5")
            .AsAIAgent(
                instructions: instruction,
                name: name);

    }

    static string GetUserInput(string text)
    {
        Console.Write($"{text}: ");
        string? input;
        while (true)
        {
            input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Please enter valid text.");
                continue;
            }
            break;
        }
        return input;
    }
}
