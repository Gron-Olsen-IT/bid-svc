using BidAPI.Models;


namespace BidAPI.Services;
public interface IBidService{

    Task<Bid?> DoesBidExists(string bidId);
    Task<Bid> Post(BidDTO bidDTO, string token);
    Task<List<Bid?>> GetMaxBids(List<string> auctionIds);
    Task<List<Bid>> Get(string auctionId);

}