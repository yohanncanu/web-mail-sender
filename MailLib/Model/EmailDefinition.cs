using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;

namespace MailLib.Model
{
    public class EmailDefinition
    {
        public string? EmailTemplateId { get; init; }
        public string? EmailTemplate { get; init; }
        public string BusinessId { get; init; }
        public List<EmailHeader> Emails { get; init; }
        public JsonElement ViewJson { get; init; }
        public string TenantId { get; init; }

        internal ExpandoObject ViewData =>
            JsonConvert.DeserializeObject<ExpandoObject>(ViewJson.GetRawText(), new ExpandoObjectConverter());
    }
}
