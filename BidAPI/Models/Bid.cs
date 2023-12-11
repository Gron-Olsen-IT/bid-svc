using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BidAPI.Models
{
    public class Bid
    {

        public Bid(BidDTO bidDTO)
        {
            BuyerId = bidDTO.BuyerId;
            AuctionId = bidDTO.AuctionId;
            Offer = bidDTO.Offer;
            CreatedAt = bidDTO.CreatedAt;
        }
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string BuyerId { get; set; }
        public string AuctionId { get; set; }
        public int Offer { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record BidDTO
    {
        public BidDTO(string buyerId, string auctionId, int offer, DateTime createdAt)
        {
            BuyerId = buyerId;
            AuctionId = auctionId;
            Offer = offer;
            CreatedAt = createdAt;
        }
        public string BuyerId { get; init; }
        public string AuctionId { get; init; }
        public int Offer { get; init; } 
        public DateTime CreatedAt { get; init; }
    }
}
