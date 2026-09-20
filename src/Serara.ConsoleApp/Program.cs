using Microsoft.Extensions.Hosting;
using Serara.ConsoleApp;

var builder = Host.CreateApplicationBuilder();

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile("config.debug.json", optional: true, reloadOnChange: false);

builder.Services.AddSeraraConsoleApp(builder.Configuration);

using var host = builder.Build();

host.Run();
