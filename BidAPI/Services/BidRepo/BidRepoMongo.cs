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
    public async Task<Bid> GetMaxBid(string auctionId)
    {
        try
        {
            var filter = Builders<Bid>.Filter.Eq("Id", auctionId);
            var sort = Builders<Bid>.Sort.Descending("offer");
            var bid = await _collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
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
            var filter = Builders<Bid>.Filter.Eq("Id", auctionId);
            var bids = await _collection.Find(filter).ToListAsync();
            return bids;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }


}