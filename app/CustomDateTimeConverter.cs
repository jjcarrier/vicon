using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerControllerApp
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                DateTime.ParseExact(reader.GetString()!,
                    "HH:mm:ss.fffffff", CultureInfo.InvariantCulture);

        public override void Write(
            Utf8JsonWriter writer,
            DateTime dateTimeValue,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(dateTimeValue.ToString(
                    "HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
    }
}
