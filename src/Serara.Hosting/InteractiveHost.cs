using Microsoft.Extensions.Hosting;

namespace Serara.Hosting;

public static class InteractiveHost
{
    public static int Run(Func<IHost> createHost)
    {
        var failure = new ApplicationFailure();
        try
        {
            using var host = createHost();
            failure = host.Services.GetRequiredService<ApplicationFailure>();
            host.Run();
        }
        catch (Exception ex)
        {
            failure.MarkFailed(ex);
        }

        if (!failure.HasFailed)
        {
            return 0;
        }

        if (failure.Exception is { } exception)
        {
            System.Console.Error.WriteLine($"Serara could not continue: {exception.Message}");
            if (exception.InnerException is not null)
            {
                System.Console.Error.WriteLine(exception.GetBaseException().Message);
            }
        }

        if (!System.Console.IsInputRedirected)
        {
            while (System.Console.KeyAvailable)
            {
                _ = System.Console.ReadKey(intercept: true);
            }

            System.Console.WriteLine("Press Enter to exit.");
            _ = System.Console.ReadLine();
        }

        return 1;
    }
}
