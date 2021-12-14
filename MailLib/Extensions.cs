using MailKit;
using MailLib.Configuration;
using MailLib.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailLib;
public static class Extensions
{
    public static void AddMailSender(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var conf = configuration.GetSection(nameof(MailSettings));
        services.Configure<MailSettings>(conf);
        services.AddSingleton<EmailSender>();
        services.AddSingleton<IProtocolLogger, SerilogProtocolLogger>();
    }

    public static void AddWorkerMailSenderHostedService(this IServiceCollection services, IConfiguration configuration)
    {
        var config = configuration.GetSection("WorkerSettings");
        services.Configure<WorkerSettings>(config);
        services.AddScoped<SendEmailWorker>();
        services.AddHostedService<SendEmailBackgroundService>();
    }
}