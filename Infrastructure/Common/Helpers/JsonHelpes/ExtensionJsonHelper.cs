using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Common.Helpers.JsonHelpes
{
    public static class ExtensionJsonHelper
    {
        /// <summary>
        /// Serialize object to json use Newtonsoft.Json.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns json.</returns>
        public static string ToJSON(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto }); ;
        }

        /// <summary>
        /// Replaces the top-level "security" property (login/password sent to the credit bureau)
        /// with a placeholder before the JSON is written to logs or Telegram. Never use this on
        /// the value actually sent over the wire — only on copies destined for logging/notifications.
        /// </summary>
        public static string RedactSecurity(this string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json ?? string.Empty;
            }

            try
            {
                var obj = JObject.Parse(json);
                if (obj["security"] is not null)
                {
                    obj["security"] = "REDACTED";
                }
                return obj.ToString(Formatting.None);
            }
            catch
            {
                return json;
            }
        }
    }
}
