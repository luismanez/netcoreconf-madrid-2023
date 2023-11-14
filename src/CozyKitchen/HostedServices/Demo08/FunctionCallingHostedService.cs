using CozyKitchen.Plugins.Native;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Plugins.Core;

namespace CozyKitchen.HostedServices;
public class FunctionCallingHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IKernel _kernel;
    private readonly HttpClient _client;

    public FunctionCallingHostedService(
        ILogger<NestedFunctionHostedService> logger,
        IConfiguration configuration,
        IKernel kernel,
        HttpClient client)
    {
        _logger = logger;
        _configuration = configuration;
        _kernel = kernel;
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _kernel.ImportFunctions(new TimePlugin(), "TimePlugin");
        _kernel.ImportFunctions(new MyIpAddressPlugin(_client), "MyIpPlugin");
        _kernel.ImportFunctions(new UniversityFinderPlugin(_client), "UniversityFinderPlugin");

        OpenAIRequestSettings requestSettings = new()
        {
            // Include all functions registered with the kernel.
            // Alternatively, you can provide your own list of OpenAIFunctions to include.
            Functions = _kernel.Functions.GetFunctionViews().Select(f => f.ToOpenAIFunction()).ToList(),
            FunctionCall = OpenAIRequestSettings.FunctionCallAuto // Let the model choose the best function to use.
            // FunctionCall = "TimePlugin-Date"; // Force the model to use that function
        };

        var chatCompletion = _kernel.GetService<IChatCompletion>();
        var chatHistory = chatCompletion.CreateNewChat();

        while (true)
        {
            Console.WriteLine("How can I help: (type 'exit' to finish)");
            var ask = Console.ReadLine();

            if (ask!.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("... Chat closed. CTRL+C to exit program ...");
                break;
            }

            chatHistory.AddUserMessage(ask!);

            var chatResult = (await chatCompletion.GetChatCompletionsAsync(chatHistory, requestSettings))[0];

            var chatMessage = await chatResult.GetChatMessageAsync();
            if (!string.IsNullOrEmpty(chatMessage.Content))
            {
                Console.WriteLine();
                Console.WriteLine(chatMessage.Content);
                Console.WriteLine();
            }

            // Check for function response
            var functionResponse = chatResult.GetOpenAIFunctionResponse();
            if (functionResponse is not null)
            {
                // Print function response details
                Console.WriteLine("Function name: " + functionResponse.FunctionName);
                Console.WriteLine("Plugin name: " + functionResponse.PluginName);
                Console.WriteLine("Arguments: ");
                foreach (var parameter in functionResponse.Parameters)
                {
                    Console.WriteLine($"- {parameter.Key}: {parameter.Value}");
                }

                // If the function returned by OpenAI is an SKFunction registered with the kernel,
                // you can invoke it using the following code.
                if (_kernel.Functions.TryGetFunctionAndContext(
                    functionResponse,
                    out ISKFunction? func,
                    out ContextVariables? context))
                {
                    var kernelResult = await _kernel.RunAsync(func, context, cancellationToken: cancellationToken);

                    var resultMessage = kernelResult.GetValue<string>();

                    if (!string.IsNullOrEmpty(resultMessage))
                    {
                        chatHistory.AddAssistantMessage(resultMessage);
                        Console.WriteLine();
                        Console.WriteLine(resultMessage);
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("Result from Plugin not available... shoul not happen :)");
                    }
                }
                else
                {
                    Console.WriteLine($"Error: Function {functionResponse.PluginName}.{functionResponse.FunctionName} not found.");
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("HostedService Stopped");
        return Task.CompletedTask;
    }
}
