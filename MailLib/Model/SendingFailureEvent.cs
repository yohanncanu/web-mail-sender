using MailLib.Configuration;
using System;

namespace MailLib.Model;
public class SendingFailureEvent : IEvent
{
    public DateTime CreationDate { get; init; } = DateTime.UtcNow;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ErrorMessage { get; init; }
    public int FailedCount { get; init; }
    public MailSettings MailSettings { get; init; }
    public string BusinessId { get; init; }
    public EmailMessage Message { get; init; }
    public SendingFailureEvent(
        MailSettings mailSettings,
        string businessId,
        EmailMessage message,
        string errorMessage,
        int failedCount = 1)
    {
        MailSettings = mailSettings;
        BusinessId = businessId;
        Message = message;
        ErrorMessage = errorMessage;
        FailedCount = failedCount;
    }

}
