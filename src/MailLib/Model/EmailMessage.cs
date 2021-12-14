using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MailLib.Model;


public class EmailMessage
{
    public DateTime InitialSendingDate { get; init; } = DateTime.UtcNow;
    public string Subject { get; set; }
    public string HtmlBody { get; set; }

    public MailboxAddress FromAddress { get; set; }
    public List<MailboxAddress> ToAddresses { get; set; } = new List<MailboxAddress>();


    public EmailMessage(string subject, string htmlBody)
    {
        Subject = subject;
        HtmlBody = htmlBody;
    }

    public string GetRecipients()
    {
        return string.Join(',', ToAddresses);
    }

    public MimeMessage GetMimeMessage() => GetMimeMessage(this);

    public static void DisposeMimeFileStreams(MimeMessage mimeMessage)
    {
        if (mimeMessage == null) return;

        // Dispose the streams of file attachments
        foreach (var mimePart in mimeMessage.Attachments?.Where(mp => mp is MimePart)?.Cast<MimePart>())
        {
            mimePart?.Content?.Stream?.Dispose();
        }

        // Dispose the streams of HTML inline file attachments
        foreach (var mimePart in mimeMessage.BodyParts?.Where(mp => mp is MimePart)?.Cast<MimePart>())
        {
            mimePart?.Content?.Stream?.Dispose();
        }
    }

    private static MimeMessage GetMimeMessage(EmailMessage email)
    {
        var message = new MimeMessage
        {
            Subject = email.Subject
        };

        var mailFrom = email.FromAddress;
        message.From.Add(mailFrom);

        foreach (var toEmail in email.ToAddresses)
        {
            message.To.Add(toEmail);
        }

        message.Body = new TextPart("html")
        {
            Text = email.HtmlBody
        };
        return message;
    }
}
