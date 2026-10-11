using Microsoft.Extensions.Hosting;
using Serara.ConsoleApp;
using Serara.Hosting;

return InteractiveHost.Run(CreateHost);

static IHost CreateHost()
{
    var builder = Host.CreateApplicationBuilder();

    builder.Configuration.Sources.Clear();
    builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);
    builder.Configuration.AddJsonFile("config.debug.json", optional: true, reloadOnChange: false);

    builder.Services.AddSeraraConsoleApp(builder.Configuration);

    return builder.Build();
}
