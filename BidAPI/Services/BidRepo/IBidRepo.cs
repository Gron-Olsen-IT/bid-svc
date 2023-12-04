using BidAPI.Models;


namespace BidAPI.Services;
public interface IBidRepo{

    Task<Bid> Post(Bid bid);
    Task<Bid> GetMaxBid(string auctionId);
    Task<List<Bid>> Get(string auctionId);

}