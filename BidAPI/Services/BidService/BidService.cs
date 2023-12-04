using BidAPI.Models;

namespace BidAPI.Services;

public class BidService : IBidService
{
    private readonly IBidRepo _bidRepo;
    private readonly ILogger<BidService> _logger;
    private readonly IInfraRepo _infraRepo;

    public BidService(IBidRepo bidRepo, ILogger<BidService> logger , IInfraRepo infraRepo)
    {
        _bidRepo = bidRepo;
        _logger = logger;
        _infraRepo = infraRepo;
    }

    public Task<Bid> Post(BidDTO bidDTO)
    {
        try
        {
            return _bidRepo.Post(new(bidDTO));
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }
    }
    public Task<Bid> GetMaxBid(string auctionId)
    {
        try
        {
            return _bidRepo.GetMaxBid(auctionId);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }
    }
    public Task<List<Bid>> Get(string auctionId)
    {
        try
        {
            return _bidRepo.Get(auctionId);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }
    }


}