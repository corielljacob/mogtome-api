using System.Text;
using System.Text.Json;

namespace MogTomeApi.Services
{
    public class CursorHelper
    {
        public record EventCursor(DateTime CreatedAt, string Id);

        public static string EncodeCursor(EventCursor cursor)
        {
            var json = JsonSerializer.Serialize(cursor);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static EventCursor DecodeCursor(string cursor)
        {
            if (string.IsNullOrWhiteSpace(cursor)) return null;

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return JsonSerializer.Deserialize<EventCursor>(json);
        }
    }
}
