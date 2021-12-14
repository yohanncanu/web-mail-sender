using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailLib.Model;

public static class EmailTemplate
{
    private static Dictionary<string, string> CacheEmailTemplates = new Dictionary<string, string>();
    private static Stopwatch stopWatch;
    private static int cacheDurationInSeconds = 60 * 10; // 10 min

    public static string Read(IHostEnvironment environment, string tenant, string templateId)
    {
        stopWatch = new Stopwatch();
        stopWatch.Start();
        string rootPath = environment.ContentRootPath;
        string prefix = "ApplicationData/Templates";
        var template = $"{tenant}-{templateId}.html";
        if (!CacheEmailTemplates.ContainsKey(template) || stopWatch.ElapsedMilliseconds > cacheDurationInSeconds * 1000)
        {
            var file = Path.Combine(rootPath, prefix, template);
            var bodyTemplate = File.ReadAllText(file);
            CacheEmailTemplates.Add(template, bodyTemplate);
        }
        return CacheEmailTemplates[template];
    }

}
