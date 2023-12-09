using System.Net;
using BidAPI.Models;

namespace BidAPI.Services;


public interface IInfraRepo
{

    Task<HttpStatusCode> UpdateMaxBid(string auctionId, int maxBid);
    BidDTO Post(BidDTO bidDTO);
    Task<int> GetMinPrice(string auctionId);
    Task<bool> Get(string auctionId);
    
    Task<bool> GetUserId(string userId);
    
    
    
}