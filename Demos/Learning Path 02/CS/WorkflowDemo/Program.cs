using Agents;
using Agents.Executors;
using ConnectionInfo;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.Text;


namespace WorkflowDemo;

internal class Program
{
    static AIModel model = new GPT_4o_mini();
    static string path = @"D:\Gen";

    static async Task Main(string[] args)
    {
       // await SequentialFlow();
        //await BranchFlow();
        //await SwitchFlow();
        //await LoopFlow();
        //await ConcurrentFlow();
         await WithHumanFlow();
        //await UseStateFlow();


        //await VibeCoderAttemptAsync();
        Console.ReadLine();
    }

    private static async Task UseStateFlow()
    {
        var procState = new StateProducerExecutor();
        var consState = new StateConsumerExecutor();

        var builder = new WorkflowBuilder(procState)
           .AddEdge(procState, consState)
           .WithOutputFrom(consState);
        var workflow = builder.Build();
        Console.WriteLine(workflow.ToMermaidString());
        Console.WriteLine(new string('=', 80));
        var run = await InProcessExecution.RunAsync(workflow, "Hello World");

       await ExamineRunAsync(run);

    }

    private static async Task SequentialFlow()
    {
        var realAgent = model.CreateChatAgent().AsAIAgent($"You know all about dutch history");
      
        var execA = new StringExecutor();
        var execB = new RandomBoolExecutor();
        var execD = new HandleBoolExecutor();
        var execE = new RandomDayExecuter();
        var execF = new String2ChatMessageExecuter();
        var execG = new AgentReceiverExecuter();
        var execH = new WrapAgentExecuter();

        //var builder = new WorkflowBuilder(execH)
        //    .AddEdge(execH, execA)
        //    .WithOutputFrom( execA);

        var builder = new WorkflowBuilder(execA)
            .AddEdge(execA, execB)
            .AddEdge(execB, execD)
            .AddEdge(execD, execE)
            .AddEdge(execE, execF)
            .AddEdge(execF, realAgent)
            .AddEdge(realAgent, execG);
            //.WithOutputFrom(execG);
        var workflow = builder.Build();
        Console.WriteLine(workflow.ToMermaidString());
        Console.WriteLine(new string('=', 80));
        var run = await InProcessExecution.RunAsync(workflow, "Hello World");
        // Next call MUST be done in String2ChatMessageExecuter, otherwise the agent won't run.
        // run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        // See String2ChatMessageExecuter for more info.
        Console.WriteLine(new string('=', 80));
        await ExamineRunAsync(run);
    }
    private static async Task BranchFlow()
    {
        var execA = new StringExecutor();
        var execB = new RandomBoolExecutor();
        var execLeft = new HandleBoolExecutor(id: "Left hand");
        var execRight = new HandleBoolExecutor(id: "Right hand");
        var execStrLeft = new StringExecutor(id: "Left hand continue");
        var execStrRight = new StringExecutor(id: "Right hand continue");
        
        var builder = new WorkflowBuilder(execA)
            .AddEdge(execA, execB)
            .AddEdge(execB, execLeft, condition: Condition(true))
            .AddEdge(execLeft, execStrLeft)
            .AddEdge(execB, execRight, condition: Condition(false))
            .AddEdge(execRight, execStrRight)
            .WithOutputFrom(execStrLeft, execStrRight);
        var workflow = builder.Build();
        Console.WriteLine(workflow.ToMermaidString());
        Console.WriteLine(new string('=', 80));
        
        var run = await InProcessExecution.RunAsync(workflow, "Hello World");
        
        Console.WriteLine(new string('=', 80));
        await ExamineRunAsync(run);

        Func<bool, bool> Condition(bool expected)
        {
            return input =>
            {
                Console.WriteLine($"input: {input}, expected: {expected}");
                return input == expected;
            };
        }
    }
    private static async Task SwitchFlow()
    {
        var execInit = new StringExecutor();
        var execDay = new RandomDayExecuter();
        var obj2Str = new ObjectToStringExecutor();
        var executors = new List<Executor> {
            new StringExecutor(id: "SundayExecutor"),
            new StringExecutor(id: "MondayExecutor"),
            new StringExecutor(id: "TuesdayExecutor"),
            new StringExecutor(id: "WednesdayExecutor"),
            new StringExecutor(id: "ThursdayExecutor"),
            new StringExecutor(id: "FridayExecutor"),
            new StringExecutor(id: "SaturdayExecutor")
        };

        var builder = new WorkflowBuilder(execInit)
            .AddEdge(execInit, execDay)
            .AddEdge(execDay, obj2Str)
            .AddSwitch(obj2Str, swbld => {
                swbld.AddCase(Condition(DayOfWeek.Sunday), executors[0])
                .AddCase(Condition(DayOfWeek.Monday), executors[1])
                .AddCase(Condition(DayOfWeek.Tuesday), executors[2])
                .AddCase(Condition(DayOfWeek.Wednesday), executors[3])
                .AddCase(Condition(DayOfWeek.Thursday), executors[4])
                .AddCase(Condition(DayOfWeek.Friday), executors[5])
                .AddCase(Condition(DayOfWeek.Saturday), executors[6])
                .WithDefault(new StringExecutor(id: "Default executor"));
            })
            .WithOutputFrom([..executors, execDay, execInit]);
        var workflow = builder.Build();
        Console.WriteLine(workflow.ToMermaidString());
        Console.WriteLine(new string('=', 80));

        var run = await InProcessExecution.RunAsync(workflow, "Init ...");

        Console.WriteLine(new string('=', 80));
        await ExamineRunAsync(run);

        Func<DayOfWeek, bool> Condition(DayOfWeek expected)
        {
            return input =>
            {
                return input == expected;
            };
        }
    }
    private static async Task LoopFlow()
    {
        var execA = new StringExecutor();
        var execE = new RandomDayExecuter();
        var execF = new CounterExecutor(counterMax: 3);

        var builder = new WorkflowBuilder(execA)
            .AddEdge(execA, execE)
            .AddEdge(execE, execF)
            .AddEdge(execF, execA)
            .WithOutputFrom(execF);

        var workflow = builder.Build();
        Console.WriteLine(workflow.ToMermaidString());
        Console.WriteLine(new string('=', 80));
        var run = await InProcessExecution.RunAsync(workflow, "Init");
        Console.WriteLine(new string('=', 80));
        //await ExamineRunAsync(run);
    }
    private static async Task ConcurrentFlow()
    {
        var execInit = new StringExecutor();
        var aggrExec = new AggregationExecutor();
        var executors = new List<Executor> {
            new StringExecutor(id: "LeftExecutor", Random.Shared.Next(1000, 5000)),
            new StringExecutor(id: "MiddleExecutor", Random.Shared.Next(1000, 5000)),
            new StringExecutor(id: "RightExecutor", Random.Shared.Next(1000, 5000)),
        };

        var builder = new WorkflowBuilder(execInit)
            .AddFanOutEdge(execInit, [..executors])
            .AddFanInBarrierEdge([..executors], aggrExec)
            .WithOutputFrom(aggrExec);
        var workflow = builder.Build();
        Console.WriteLine(workflow.ToMermaidString());
        Console.WriteLine(new string('=', 80));

        var run = await InProcessExecution.RunAsync(workflow, "Init ...");

        Console.WriteLine(new string('=', 80));
        await ExamineRunAsync(run);

        // Func<DayOfWeek, bool> Condition(DayOfWeek expected)
        // {
        //     return input =>
        //     {
        //         return input == expected;
        //     };
        // }
    }
    private static async Task WithHumanFlow()
    {
        var execA = new StringExecutor();
        var execB = new RandomBoolExecutor();
        var execD = new HandleBoolExecutor();
        var execE = new RandomDayExecuter();
        var execF = new StringExecutor("Final_Handler");
        var contin = new ContinuationExecutor();
        var human = RequestPort.Create<DayOfWeek, bool>("Carbon_life_form");

        var builder = new WorkflowBuilder(execA)
            .AddEdge(execA, execB)
            .AddEdge(execB, execD)
            .AddEdge(execD, execE)
            .AddEdge(execE, human)
            .AddEdge(human, contin)
            .AddEdge(contin, execF)
            .WithOutputFrom(human, contin, execF);

        var workflow = builder.Build();
        Console.WriteLine(workflow.ToMermaidString());
        Console.WriteLine(new string('=', 80));
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, "Init");
        
        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent reqInf:
                    Console.WriteLine($"Shall we continue? (y/n)");
                    var inp = Console.ReadLine();
                    var response = reqInf.Request.CreateResponse(inp?.ToLower() == "y");
                    await run.SendResponseAsync(response);
                    break;
                case ExecutorCompletedEvent comp:
                    if (comp.Data is ExternalRequest pval)
                    {
                        Console.WriteLine($"Message handled by {comp.ExecutorId}: {pval}");
                    }
                    else
                        Console.WriteLine($"Message handled by {comp.ExecutorId}: {comp.Data}");
                    break;

            }
        }

        Console.WriteLine(new string('=', 80));
        //await ExamineRunAsync(run);
    }

    private static async Task VibeCoderAttemptAsync()
    {
        var requirements = @$"
            Create a console application in .net.
            the application should perform easy calculations (add, subtract, multiply divide) and print the result to the console.
            The mathematical operation and the numbers should be defined in a separate class and the console application should use that class to perform the calculations.
            The application should be named ConsoleCalculator.
            The location where the application should be created is {path}.";
        var planAgent = new PlanningAgent(model);
        var cliAgent = new CLIAgent(model);
        var codeAgent = new CodeAgent(model);

        await cliAgent.CreateSessionAsync();

        WorkflowBuilder builder = new WorkflowBuilder(planAgent);
        builder.AddEdge(planAgent, cliAgent)
            .AddEdge(cliAgent, codeAgent)
            .WithOutputFrom(planAgent);


        var workflow = builder.Build();

        Console.WriteLine(workflow.ToMermaidString());
        var run = await InProcessExecution.RunStreamingAsync(workflow, requirements);
        await ExamineRunAsync(run);

    }

    private static async Task ExamineRunAsync(StreamingRun run)
    {
        await foreach (var wfEvt in run.WatchStreamAsync())
        {
            switch (wfEvt)
            {
                case AgentResponseUpdateEvent res:
                    Console.WriteLine($"Agent {res.ExecutorId} responded with: {res.Data}");
                    break;
                case ExecutorCompletedEvent comp:
                    Console.WriteLine($"Message handled by {comp.ExecutorId}: {comp.Data}");
                   
                    break;
                default:
                    Console.WriteLine($"Event: {wfEvt.GetType().Name}");
                    break;
            }
        }

    }

    static async Task ExamineRunAsync(Run? run)
    {
        if (run == null) return;

        foreach (var evt in run.NewEvents)
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent res:
                    //Console.WriteLine($"Agent {res.ExecutorId} responded with: {res.Data}");
                    Console.Write(res.Data);
                    break;
                case ExecutorCompletedEvent comp:
                    Console.WriteLine($"{comp.ExecutorId} has completed and returns {comp.Data}");
                    break;
                case ExecutorInvokedEvent ievt:
                    Console.WriteLine($"{ievt.ExecutorId} was invoked");
                    break;
                default:
                    Console.WriteLine();
                   // Console.WriteLine($"Event: {evt.GetType().Name}");
                    break;
            }
        }
    }
}
