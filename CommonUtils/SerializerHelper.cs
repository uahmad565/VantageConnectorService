using System.Text;
using System.Text.Json;

namespace CommonUtils
{
    public class SerializerHelper
    {
        public static async Task<string> GetSerializedObject(object obj, JsonSerializerOptions? options = null)
        {
            using var memStream = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(memStream, obj, options);
            var json = await Task.Run(() => Encoding.UTF8.GetString(memStream.GetBuffer()));
            return json;
        }
    }
}