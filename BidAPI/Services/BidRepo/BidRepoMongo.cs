using BidAPI.Controllers;
using BidAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BidAPI.Services;

public class BidRepoMongo : IBidRepo
{

    private readonly IMongoCollection<Bid> _collection;

    public BidRepoMongo()
    {
        string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "mongodb://admin:1234@localhost:27017/";
        var mongoDatabase = new MongoClient(connectionString).GetDatabase("bid_db");
        _collection = mongoDatabase.GetCollection<Bid>("bids");
    }

    public async Task<Bid?> DoesBidExists(string bidId)
    {
        try
        {
            return await _collection.Find(bid => bid.Id == bidId).FirstAsync();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task<Bid> Post(Bid bid)
    {
        try
        {
            await _collection.InsertOneAsync(bid);
            return bid;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    public async Task<List<Bid?>> GetMaxBids(List<string> auctionIds)
    {
        try
        {
            List<Bid> aggregatedBids = await _collection.Aggregate()
                                            .Match(bid => auctionIds.Contains(bid.AuctionId))
                                            .SortByDescending(bid => bid.Offer)
                                            .Group(bid => bid.AuctionId, bids => new
                                            {
                                                AuctionId = bids.Key,
                                                HighestBid = bids.First() 
                                            })
                                            .ReplaceRoot(bid => bid.HighestBid)
                                            .ToListAsync();
            if (aggregatedBids.Count == 0){
                return new List<Bid?>{null};
            }
            return aggregatedBids.Select(bid => (Bid?)bid).ToList();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task<List<Bid>> Get(string auctionId)
    {
        try
        {
            return await _collection.Find(bid => bid.AuctionId == auctionId).ToListAsync();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }


}