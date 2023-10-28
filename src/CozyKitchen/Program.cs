using CozyKitchen.Extensions;
using CozyKitchen.HostedServices;
using CozyKitchen.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(configHost =>
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        configHost.SetBasePath(currentDirectory);
        configHost.AddJsonFile("hostsettings.json", optional: false);
        configHost.AddCommandLine(args);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        services.AddOptions();
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SettingsSectionName));

        services.AddLogging(configure => configure.AddConsole());

        services.AddSemanticKernelWithChatAndTextCompletions();

        //services.AddHostedService<HelloSemanticWorldHostedService>();
        //services.AddHostedService<SemanticFunctionWithParamsHostedService>();
        services.AddHostedService<NativeFunctionHostedService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<IHost>>();
try
{
    host.Run();
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        logger!.LogError(failure);
    }
}