using System.Text;

namespace CommonUtils
{
    public class SerializerHelper
    {
        public static async Task<string> GetSerializedObject(object obj)
        {
            using var memStream = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(memStream, obj);
            var json = await Task.Run(() => Encoding.UTF8.GetString(memStream.GetBuffer()));
            return json;
        }
    }
}