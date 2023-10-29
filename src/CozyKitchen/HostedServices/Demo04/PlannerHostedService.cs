using System.Text.Json;
using Azure.Identity;
using CozyKitchen.Extensions;
using CozyKitchen.Plugins.Native;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Beta;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;

namespace CozyKitchen.HostedServices;
public class PlannerHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IKernel _kernel;
    private readonly IDictionary<string, ISKFunction> _functions;

    public PlannerHostedService(
        ILogger<NestedFunctionHostedService> logger,
        IConfiguration configuration,
        IKernel kernel)
    {
        _logger = logger;
        _configuration = configuration;
        _kernel = kernel;
        _functions = _kernel.ImportSemanticFunctionsFromDirectory(
            PathExtensions.GetPluginsRootFolder(),
            "ResumeAssistantPlugin", "TravelAgentPlugin");
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
        var plan = await planner.CreatePlanAsync(ask!, cancellationToken: cancellationToken);

        _logger.LogInformation("Plan:\n");
        _logger.LogInformation(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

        var result = await _kernel.RunAsync(plan);
        _logger.LogInformation("Plan results:\n");
        _logger.LogInformation(result.GetValue<string>()!.Trim());
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
