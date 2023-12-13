using BidAPI.Controllers;
using BidAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BidAPI.Services;

public class BidRepoMongo : IBidRepo
{
    private readonly ILogger<BidsController> _logger;
    private readonly IMongoCollection<Bid> _collection;

    public BidRepoMongo(ILogger<BidsController> logger)
    {
        _logger = logger;
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
    public async Task<List<Bid>?> GetMaxBids(List<string> auctionIds)
    {
        try
        {
            _logger.LogInformation("Getting max bids from auctionIds");
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
            _logger.LogInformation("Got max bids from auctionIds " + aggregatedBids.ToString() + " " + aggregatedBids.Count);
            if (aggregatedBids.Count == 0){
                _logger.LogInformation("No bids found - returning null");
                return null;
            }
            _logger.LogInformation("Returning max bids from auctionIds");
            return aggreg