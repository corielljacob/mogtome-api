using MongoDB.Bson.Serialization.Attributes;

namespace MogTomeApi.Data
{
    [BsonIgnoreExtraElements]
    public class FreeCompanyStaffMember : FreeCompanyMember
    {
        public string Biography { get; set; }
        public DateTime? PromotionDate { get; set; }
        public bool RecentlyPromoted { get { return PromotionDate != null && PromotionDate >= DateTime.Now.AddDays(-30); } }
    }
}
