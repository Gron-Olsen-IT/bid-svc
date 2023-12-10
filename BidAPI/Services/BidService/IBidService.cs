using BidAPI.Models;


namespace BidAPI.Services;
public interface IBidService{

    Task<Bid?> DoesBidExists(string bidId);
    Task<Bid> Post(BidDTO bidDTO);
    Task<Bid?> GetMaxBid(string auctionId);
    Task<List<Bid>> Get(string auctionId);

}