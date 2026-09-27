using System.Diagnostics;

namespace RideHailing.Api;

public sealed class FrontendDevelopmentServer(
    IHostEnvironment environment,
    ILogger<FrontendDevelopmentServer> logger) : BackgroundService
{
    private Process? process;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var frontendDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "frontend", "ridehailing-web"));
        var packageFile = Path.Combine(frontendDirectory, "package.json");

        if (!File.Exists(packageFile))
        {
            logger.LogWarning("Frontend package.json was not found at {FrontendDirectory}; the frontend server was not started.", frontendDirectory);
            return;
        }

        var npmCommand = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "npm.cmd")
            : "npm";
        if (OperatingSystem.IsWindows() && !File.Exists(npmCommand))
        {
            npmCommand = "npm.cmd";
        }
        process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = npmCommand,
                Arguments = "run dev -- --host localhost",
                WorkingDirectory = frontendDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                logger.LogInformation("Frontend: {Message}", args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                logger.LogWarning("Frontend: {Message}", args.Data);
            }
        };
        process.Exited += (_, _) =>
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Frontend development server exited with code {ExitCode}.", process.ExitCode);
            }
        };

        try
        {
            if (!process.Start())
            {
                logger.LogError("The frontend development server could not be started.");
                return;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            logger.LogInformation("Frontend development server started at http://localhost:5173.");

            await process.WaitForExitAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to start the frontend development server. Ensure Node.js and npm are installed.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (process is { HasExited: false })
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        process?.Dispose();
        base.Dispose();
    }
}
