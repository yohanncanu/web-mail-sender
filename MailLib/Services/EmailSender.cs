using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MailLib.Configuration;
using MailLib.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using Stubble.Core.Builders;
using Stubble.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MailLib.Services;

//https://imar.spaanjaars.com/614/improving-your-aspnet-core-sites-e-mailing-capabilities
public class EmailSender
{
    private static JsonSerializerSettings serializerSettings => new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
        Converters = new List<JsonConverter> {
                    new MailboxAddressJsonConverter()
                }

    };

    private readonly IHostEnvironment _env;
    private readonly ILogger<EmailSender> _logger;
    private readonly IProtocolLogger _mailLogger;



    private readonly MailSettings _mailSettings;

    public EmailSender(
        IHostEnvironment environment,
        ILogger<EmailSender> logger,
        IProtocolLogger mailLogger,
        IOptions<MailSettings> mailSettings)
    {
        _env = environment;
        _logger = logger;
        _mailLogger = mailLogger;
        _mailSettings = mailSettings.Value;
    }

    public void SendEmails(MailSettings mailSettings, EmailDefinition maillistdef)
    {
        var mailList = PrepareEmailsWithTemplate(mailSettings.MessageSettings, maillistdef);
        ProcessEmailMessageList(maillistdef.BusinessId, mailList, mailSettings.SendingLogic, mailSettings.ServerConnection);
    }



    private List<EmailMessage> PrepareEmailsWithTemplate(MessageSettings settings, EmailDefinition emailDefinition)
    {
        string emailTemplate = emailDefinition.EmailTemplate;

        if (!string.IsNullOrWhiteSpace(emailDefinition.EmailTemplateId) &&
            string.IsNullOrWhiteSpace(emailDefinition.EmailTemplate))
        {
            emailTemplate = EmailTemplate.Read(_env, settings.TenantId, emailDefinition.EmailTemplateId);
        }
        object viewData = emailDefinition.ViewData;
        if (viewData == null)
        {
            var argumentNullException = new ArgumentNullException(nameof(viewData));
            throw argumentNullException;
        }

        string body = CreateEmailBody(viewData, emailTemplate);

        List<EmailHeader> emails = emailDefinition.Emails;
        var messageList = new List<EmailMessage>();
        foreach (var mailHeader in emails)
        {
            var subject = CreateEmailSubject(mailHeader.EmailSubject, viewData);
            var message = CreateMessage(mailHeader.EmailTo, subject, body, settings);
            messageList.Add(message);
        }
        return messageList;
    }

    private void SaveNotSentEvents(List<SentEvent> notSentEventList)
        => SaveEvents(notSentEventList, Constant.DIR_EVENTS_NOTSENT, _env);
    private void SaveSentEvents(List<SentEvent> sentEvents)
        => SaveEvents(sentEvents, Constant.DIR_EVENTS_SENT, _env);
    private void SaveSendingFailureEvents(List<SendingFailureEvent> failedList)
        => SaveEvents(failedList, Constant.DIR_EVENTS_FAILURE, _env);

    public static void SaveSendingFailureEvents(IHostEnvironment environment, List<SendingFailureEvent> failedList)
        => SaveEvents(failedList, Constant.DIR_EVENTS_FAILURE, environment);
    public static void SaveSentEvents(IHostEnvironment environment, List<SentEvent> eventList)
        => SaveEvents(eventList, Constant.DIR_EVENTS_SENT, environment);
    private void ProcessEmailMessageList(string businessId, List<EmailMessage> messageList, SendingLogic logic, ServerConnection connectionSettings)
    {
        Exception sendingError = null;
        if (!logic.DoNotSendEmail)
        {
            try
            {
                SendEmails(messageList.ToArray(), businessId, connectionSettings);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "unable to send e-mail");
                sendingError = e;
            }
        }
        else
        {
            var notSentEventList = new List<SentEvent>();
            foreach (var msg in messageList)
            {
                notSentEventList.Add(new SentEvent(_mailSettings, businessId, msg));
            }
            SaveNotSentEvents(notSentEventList);

        }
        if (logic.SaveEmail == true || sendingError != null)
        {
            var dir = sendingError != null ? Constant.DIR_EMAIL_NOTSENT : Constant.DIR_EMAIL_SENT;
            SaveEmails(messageList.ToArray(), businessId, dir);
        }

        if (sendingError != null)
        {
            throw sendingError;
        }
    }


    private static string CreateEmailSubject(string subjectTemplate, object viewData)
    {
        var stubble = new StubbleBuilder()
         .Configure(settings =>
         {
             settings.SetIgnoreCaseOnKeyLookup(true);
             settings.SetMaxRecursionDepth(512);
         })
         .Build();
        var output = stubble.Render(subjectTemplate, viewData);
        return output;
    }

    private EmailMessage CreateMessage(string toEmail, string subject, string body, MessageSettings mailSettings)
    {
        string[] toList = null;
        if (!string.IsNullOrWhiteSpace(mailSettings.ForceRecipientForDemo))
        {
            subject = $"[TO {toEmail}] - " + subject;
            if (mailSettings.ForceRecipientForDemo.Contains(","))
            {
                toList = mailSettings.ForceRecipientForDemo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            }
            else
            {
                toEmail = mailSettings.ForceRecipientForDemo;
            }
            _logger.LogInformation($"Prepare to send email to force recipients {toList?.Length} '{mailSettings.ForceRecipientForDemo}' '{toEmail}' subject:'{subject}'");
        }
        else
        {
            _logger.LogInformation($"Prepare to send email to '{toEmail}' subject:'{subject}'");
        }

        EmailMessage email = CreateEmailMessage(mailSettings.Sender, toEmail, subject, body, toList);

        return email;
    }

    private EmailMessage CreateEmailMessage(string sender, string toEmail, string subject, string body, string[] forceRecipients)
    {
        var email = new EmailMessage(subject, body);

        email.FromAddress = MailboxAddress.Parse(sender);
        if (forceRecipients != null && forceRecipients.Length > 0)
        {
            foreach (var item in forceRecipients)
            {
                email.ToAddresses.Add(MailboxAddress.Parse(item));
            }
        }
        else
        {
            email.ToAddresses.Add(MailboxAddress.Parse(toEmail));
        }

        return email;
    }

    private void SaveEmails(EmailMessage[] messages, string businessId, string dir)
    {
        Exception lastError = null;

        var emailFolder = Path.Combine(_env.ContentRootPath, dir);
        Directory.CreateDirectory(emailFolder);

        foreach (var mmm in messages)
        {
            try
            {
                SaveEmail(businessId, emailFolder, mmm);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error occured while saving email {businessId}");
                lastError = e;
            }
        }

        if (lastError != null)
        {
            throw lastError;
        }
    }



    private void SendEmails(EmailMessage[] messages, string businessId, ServerConnection settings)
    {
        var sendingFailures = new List<SendingFailureEvent>();
        var sentEventList = new List<SentEvent>();

        Exception lastError = null;

        if (settings.Debug)
            LogSmtpCapabilities(settings.SmtpServer, settings.SmtpPort, settings.Username, settings.Password);

        foreach (var msg in messages)
        {
            try
            {
                var to = msg.GetRecipients();
                _logger.LogInformation("email sending {businessId} - {to}", businessId, to);
                SendEmail(msg, settings);
                _logger.LogInformation($"email sent {businessId} - {to}");
                sentEventList.Add(new SentEvent(_mailSettings, businessId, msg));
            }
            catch (Exception e)
            {
                sendingFailures.Add(new SendingFailureEvent(_mailSettings, businessId, msg, e.ToString()));
                _logger.LogError(e, "Error occured while sending email {businessId}", businessId);
                lastError = e;
            }
        }
        SaveSentEvents(sentEventList);
        SaveSendingFailureEvents(sendingFailures);
        if (lastError != null)
        {
            throw lastError;
        }
    }

    private static void SaveEvents(IEnumerable<IEvent> events, string directory, IHostEnvironment env)
    {
        var eventDirectory = Path.Combine(env.ContentRootPath, directory);
        Directory.CreateDirectory(eventDirectory);
        foreach (var e in events)
        {
            string json = JsonConvert.SerializeObject(e, Formatting.Indented, serializerSettings);
            var filename = Path.Combine(eventDirectory, $"{e.Id}.json");
            File.WriteAllText(filename, json);
        }
    }


    private void SaveEmail(string businessId, string emailFolder, EmailMessage email)
    {
        string to = email.GetRecipients();
        _logger.LogInformation("email saving  {businessId} - {to}", businessId, to);
        MimeMessage mime = email.GetMimeMessage();
        mime.WriteTo(Path.Combine(emailFolder, $"{to}#{businessId}.eml"));
        EmailMessage.DisposeMimeFileStreams(mime);
        _logger.LogInformation($"email saved  {businessId} - {to}");
    }

    public static SendingFailureEvent GetNextFailure(IHostEnvironment env)
    {

        var failureDirectory = Path.Combine(env.ContentRootPath, Constant.DIR_EVENTS_FAILURE);
        Directory.CreateDirectory(failureDirectory);
        string pattern = "*.json";
        var dirInfo = new DirectoryInfo(failureDirectory);
        var file = (from f in dirInfo.GetFiles(pattern) orderby f.LastWriteTime descending select f).FirstOrDefault();
        if (file == null)
            return null;
        string filename = file.FullName + ".lock";
        file.MoveTo(filename);
        string json = File.ReadAllText(filename);

        var evt = JsonConvert.DeserializeObject<SendingFailureEvent>(json, serializerSettings);
        return evt;
    }

    internal static void RemoveEvent(IHostEnvironment env, Guid eventId)
    {
        var failureDirectory = Path.Combine(env.ContentRootPath, Constant.DIR_EVENTS_FAILURE);
        File.Delete(Path.Combine(failureDirectory, $"{eventId}.json.lock"));
    }

    private void SendEmail(EmailMessage email, ServerConnection settings) =>
        SendEmail(email, settings, _mailLogger);

    internal static void SendEmail(EmailMessage email, ServerConnection settings, IProtocolLogger mailLogger = null)
    {
        // create message
        MimeMessage message = email.GetMimeMessage();
        var debugMode = mailLogger == null ? false : settings.Debug;
        using var client = debugMode ? new SmtpClient(mailLogger) : new SmtpClient();
        client.CheckCertificateRevocation = false;
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        client.Timeout = settings.Timeout;
        client.Connect(settings.SmtpServer, settings.SmtpPort, !settings.NoSsl);


        if (!string.IsNullOrEmpty(settings.Username))
            client.Authenticate(settings.Username, settings.Password);

        client.Send(FormatOptions.Default, message);

        client.ProtocolLogger?.Dispose();
        client.Disconnect(true);

    }



    private string CreateEmailBody(object form, string htmlTemplate)
    {
        var d = DateTime.Now;
        _logger.LogDebug($"formatter date will convert '{d}' into '{d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}'");

        var helpers = new Helpers()
            .Register("FormatDate", (HelperContext context, DateTime? d) =>
            {
                if (d == null)
                {
                    return "None";
                }
                return d.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            })
            .Register("FormatBoolean", (HelperContext context, bool? b) =>
            {
                if (b == null)
                {
                    return "None";
                }
                return b.Value ? "Yes" : "No";
            });

        var stubble = new StubbleBuilder()
            .Configure(settings =>
            {
                settings.SetIgnoreCaseOnKeyLookup(true);
                settings.SetMaxRecursionDepth(512);
                settings.AddHelpers(helpers);
            })
            .Build();
        var output = stubble.Render(htmlTemplate, form);
        return output;
    }


    internal static void LogSmtpCapabilities(string smtpServer, int smtpPort, string username, string password)
    {
        var settings = new
        {
            SmtpServer = smtpServer,
            SmtpPort = smtpPort,
            Username = username,
            Password = password
        };
        using (var client = new SmtpClient())
        {
            client.CheckCertificateRevocation = false;
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(settings.SmtpServer, settings.SmtpPort, SecureSocketOptions.StartTlsWhenAvailable);

            if (client.Capabilities.HasFlag(SmtpCapabilities.Authentication))
            {
                var mechanisms = string.Join(", ", client.AuthenticationMechanisms);
                Console.WriteLine("The SMTP server supports the following SASL mechanisms: {0}", mechanisms);
                if (settings.Username != null)
                {
                    client.Authenticate(settings.Username, settings.Password);
                }
            }

            if (client.Capabilities.HasFlag(SmtpCapabilities.Size))
                Console.WriteLine("The SMTP server has a size restriction on messages: {0}.", client.MaxSize);

            if (client.Capabilities.HasFlag(SmtpCapabilities.Dsn))
                Console.WriteLine("The SMTP server supports delivery-status notifications.");

            if (client.Capabilities.HasFlag(SmtpCapabilities.EightBitMime))
                Console.WriteLine("The SMTP server supports Content-Transfer-Encoding: 8bit");

            if (client.Capabilities.HasFlag(SmtpCapabilities.BinaryMime))
                Console.WriteLine("The SMTP server supports Content-Transfer-Encoding: binary");

            if (client.Capabilities.HasFlag(SmtpCapabilities.UTF8))
                Console.WriteLine("The SMTP server supports UTF-8 in message headers.");

            client.Disconnect(true);
        }
    }
}
