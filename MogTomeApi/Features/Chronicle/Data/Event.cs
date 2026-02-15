using MongoDB.Bson;

namespace MogTomeApi.Features.Chronicle
{
    public class Event
    {
        public ObjectId Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
    }

    public enum EventType
    {
        MemberJoined,
        MemberRejoined,
        Announcement,
        RankPromoted,
        NameChanged
    }
}
