using BidAPI.Models;


namespace BidAPI.Services;
public interface IBidService{

    Task<Bid> Post(BidDTO bidDTO);
    Task<Bid> GetMaxBid(string auctionId);
    Task<List<Bid>> Get(string auctionId);
    Task<Bid> UpdateMaxBid(string auctionId);

}