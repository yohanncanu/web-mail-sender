using MailLib.Configuration;
using MailLib.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailLib.Services;

internal class SendEmailWorker
{
    private readonly ILogger<SendEmailWorker> _logger;
    private readonly IHostEnvironment _environment;
    private MailSettings _mailSettings;


    public SendEmailWorker(ILogger<SendEmailWorker> logger, IOptions<MailSettings> mailSettings, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _mailSettings = mailSettings.Value;
    }

    public void ProcessEvents(int maxNumberToProcess, CancellationToken stoppingToken)
    {
        int processedCount = 0;
        SendingFailureEvent failureEvent;
        var failedList = new List<SendingFailureEvent>();
        var stopwatch = Stopwatch.StartNew();
        while (
            (failureEvent = EmailSender.GetNextFailure(_environment)) != null
            && processedCount < maxNumberToProcess
            && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("retry sending email {businessId} - EventId {Id}", failureEvent.BusinessId, failureEvent.Id);
                var stopwatchSending = Stopwatch.StartNew();
                EmailSender.SendEmail(failureEvent.Message, _mailSettings.ServerConnection);
                stopwatchSending.Stop();
                _logger.LogInformation("retry email sent {businessId} - EventId {Id} - during {stopwatchElapsed}",
                    failureEvent.BusinessId,
                    failureEvent.Id,
                    stopwatchSending.Elapsed);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "retry email failed {businessId} - EventId {Id}", failureEvent.BusinessId, failureEvent.Id);
                var failureCount = failureEvent.FailedCount + 1;
                if (failureCount < _mailSettings.SendingLogic.MaxFailures
                    && failureEvent.Message.InitialSendingDate > DateTime.Now.AddDays(-_mailSettings.SendingLogic.MaxNrDays))
                {
                    failedList.Add(
                    new SendingFailureEvent(
                        _mailSettings,
                        failureEvent.BusinessId,
                        failureEvent.Message,
                        e.ToString(),
                        failureCount)
                    );
                }

            }
            EmailSender.RemoveEvent(_environment, failureEvent.Id);
            processedCount++;
        }
        EmailSender.SaveSendingFailureEvents(_environment, failedList);
        stopwatch.Stop();
        _logger.LogInformation("processed: {processedCount} Elapsed time:  {stopwatchElapsed}", processedCount, stopwatch.Elapsed);
    }
}