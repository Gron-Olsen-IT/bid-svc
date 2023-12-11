using BidAPI.Models;


namespace BidAPI.Services;
public interface IBidRepo{

    Task<Bid?> DoesBidExists(string bidId);
    Task<Bid> Post(Bid bid);
    Task<List<Bid?>> GetMaxBids(List<string> auctionIds);
    Task<List<Bid>> Get(string auctionId);


}