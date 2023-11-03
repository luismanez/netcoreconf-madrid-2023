using System.Text.Json;
using Azure.Identity;
using CozyKitchen.Extensions;
using CozyKitchen.Plugins.Native;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Beta;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Events;
using Microsoft.SemanticKernel.Planners;

namespace CozyKitchen.HostedServices;
public class FunctionHooksHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IKernel _kernel;

    public FunctionHooksHostedService(

        ILogger<NestedFunctionHostedService> logger,
        IConfiguration configuration,
        IKernel kernel)
    {
        _logger = logger;
        _configuration = configuration;
        _kernel = kernel;
        _kernel.ImportSemanticFunctionsFromDirectory(
            PathExtensions.GetPluginsRootFolder(),
            "ResumeAssistantPlugin", "TravelAgentPlugin");

        // hooks init
        kernel.FunctionInvoking += (object? sender, FunctionInvokingEventArgs e) => {
            _logger.LogInformation($"Function invoking... {e.FunctionView.Name}. {e.FunctionView.PluginName}. SKContext.Result: {e.SKContext.Result}");
        };

        kernel.FunctionInvoked += (object? sender, FunctionInvokedEventArgs e) => {
            _logger.LogInformation($"Function invoked... {e.FunctionView.Name}. {e.FunctionView.PluginName}. SKContext.Result: {e.SKContext.Result}");
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var graphClient = GetGraphServiceClient();

        // need to import the Native pluggin, as is used as nested function of another Semantic function
        var graphSkillsPlugin = new GraphUserProfileSkillsPlugin(graphClient);
        _kernel.ImportFunctions(graphSkillsPlugin, "GraphSkillsPlugin");

        Console.WriteLine("How can I help:");
        var ask = Console.ReadLine();

        var planner = new SequentialPlanner(_kernel);

        try
        {
            var plan = await planner.CreatePlanAsync(ask!, cancellationToken: cancellationToken);
            _logger.LogInformation("Plan:\n");
            _logger.LogInformation(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

            var result = await _kernel.RunAsync(plan);
            _logger.LogInformation("Plan results:\n");
            _logger.LogInformation(result.GetValue<string>()!.Trim());
            _logger.LogInformation("======================================");
        }
        catch (SKException e)
        {
            // Create plan error: Not possible to create plan for goal with available functions.
            // Goal: ...
            // Functions: ...
            Console.WriteLine(e.Message);
        }
    }

    private GraphServiceClient GetGraphServiceClient()
    {
        var scopes = new[] { "User.Read" };
        var clientId = _configuration.GetValue<string>("AzureAd:ClientId");
        var tenantId = _configuration.GetValue<string>("AzureAd:TenantId");

        var options = new InteractiveBrowserCredentialOptions
        {
            TenantId = tenantId,
            ClientId = clientId,
            RedirectUri = new Uri("http://localhost"),
        };

        var interactiveCredential = new InteractiveBrowserCredential(options);
        var graphClient = new GraphServiceClient(interactiveCredential, scopes);

        return graphClient;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("HostedService Stopped");
        return Task.CompletedTask;
    }
}
