using MimeKit;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace MailLib;
internal class MailboxAddressJsonConverter : JsonConverter<MailboxAddress>
{

    public override MailboxAddress ReadJson(
        JsonReader reader,
        Type objectType,
        [AllowNull] MailboxAddress existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
        )
    {
        return MailboxAddress.Parse((string)reader.Value);
    }

    public override void WriteJson(JsonWriter writer, [AllowNull] MailboxAddress value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Address);
    }
}
