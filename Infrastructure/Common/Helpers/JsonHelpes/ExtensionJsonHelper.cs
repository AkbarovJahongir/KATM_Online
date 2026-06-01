using Newtonsoft.Json;

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
    }
}
