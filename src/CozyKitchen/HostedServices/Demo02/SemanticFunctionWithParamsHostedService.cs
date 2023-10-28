using CozyKitchen.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace CozyKitchen.HostedServices;
public class SemanticFunctionWithParamsHostedService : IHostedService
{
    private readonly IKernel _kernel;
    private readonly IDictionary<string, ISKFunction> _functions;
    private readonly ILogger _logger;

    public SemanticFunctionWithParamsHostedService(
        IKernel kernel,
        ILogger<SemanticFunctionWithParamsHostedService> logger)
    {
        _kernel = kernel;
        _logger = logger;
        _functions = _kernel.ImportSemanticFunctionsFromDirectory(
            PathExtensions.GetPluginsRootFolder(),
            "ResumeAssistantPlugin");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var variables = new ContextVariables();
        variables.Set("FullName", "John Doe");
        variables.Set("JobTitle", "Software Engineer");
        variables.Set("TotalYearsOfExperience", "20");
        variables.Set("MainArea", "Microsoft Technologies");

        var aboutMe = await _kernel.RunAsync(
            variables,
            _functions["AboutMe"]
        );

        _logger.LogInformation($"-----ABOUT ME-----\n{aboutMe.GetValue<string>()}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("HostedService Stopped");
        return Task.CompletedTask;
    }
}
