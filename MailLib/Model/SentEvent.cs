using MailLib.Configuration;
using System;

namespace MailLib.Model;
    public class SentEvent : IEvent
{
    public DateTime CreationDate { get; init; } = DateTime.UtcNow;
    public Guid Id { get; init; } = Guid.NewGuid();
    public MailSettings MailSettings { get; init; }
    public string BusinessId { get; init; }
    public EmailMessage Message { get; init; }
    public SentEvent(MailSettings mailSettings, string businessId, EmailMessage message)
    {
        MailSettings = mailSettings;
        BusinessId = businessId;
        Message = message;
    }
}
