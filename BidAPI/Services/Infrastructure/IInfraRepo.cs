using System.Net;
using BidAPI.Models;

namespace BidAPI.Services;


public interface IInfraRepo
{

    Task<HttpStatusCode> UpdateMaxBid(string auctionId, int maxBid, string token);
    BidDTO Post(BidDTO bidDTO);
    Task<int> GetMinPrice(string auctionId, string token);
    Task<bool> AuctionIdExists(string auctionId, string token);
    
    Task<bool> UserIdExists(string userId, string token);
    
    
    
}