using BidAPI.Models;
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
    public async Task<Bid?> GetMaxBid(string auctionId)
    {
        try
        {
            var bid = await _collection.Find(bid => bid.AuctionId == auctionId).SortByDescending(bid => bid.Offer).FirstOrDefaultAsync();
            Console.WriteLine($"LÆS EFTER DET HER"+bid);
            return bid;
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