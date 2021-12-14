using MailLib.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailLib.Services;

public sealed class SendEmailBackgroundService : BackgroundService
{
    private readonly ILogger<SendEmailBackgroundService> _logger;
    private readonly WorkerSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;

    public SendEmailBackgroundService(
                    ILogger<SendEmailBackgroundService> logger,
                    IOptions<WorkerSettings> settings,
                    IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug($"SendEmailBackgroundService is starting.");

        stoppingToken.Register(() =>
            _logger.LogDebug($" SendEmailBackgroundService background task is stopping."));

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug($"SendEmailBackgroundService task doing background work.");
            using var scope = _scopeFactory.CreateScope();
            var scopedWorker = scope.ServiceProvider.GetRequiredService<SendEmailWorker>();
            scopedWorker.ProcessEvents(_settings.MaxNumberToProcess, stoppingToken);

            await Task.Delay(_settings.IdleTimeInMinutes * 60 * 1000, stoppingToken);
        }

        _logger.LogDebug($"SendEmailBackgroundService background task is stopping.");
    }
}