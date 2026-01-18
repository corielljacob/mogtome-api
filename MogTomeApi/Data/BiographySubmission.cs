using MongoDB.Bson;

namespace MogTomeApi.Data
{
    public class BiographySubmission
    {
        public ObjectId Id { get; set; }
        public Guid SubmissionId { get; set; }
        public string SubmittedByDiscordId { get; set; }
        public string Biography { get; set; }
        public string Status { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
