using BidAPI.Models;

namespace BidAPI.Services;

public class BidService : IBidService
{
    private readonly IBidRepo _bidRepo;
    private readonly ILogger<BidService> _logger;
    private readonly IInfraRepo _infraRepo;

    public BidService(IBidRepo bidRepo, ILogger<BidService> logger, IInfraRepo infraRepo)
    {
        _bidRepo = bidRepo;
        _logger = logger;
        _infraRepo = infraRepo;
    }

    public async Task<Bid> Post(BidDTO bidDTO)
    {
        try
        {
            _infraRepo.Post(bidDTO);

            for (int i = 0; i < 4; i++)
            {
                _logger.LogInformation("Bid was attempted " + i);
                Task.Delay(250).Wait();
                Bid refreshedMaxBid = await _bidRepo.GetMaxBid(bidDTO.AuctionId);
                if (refreshedMaxBid.BuyerId == bidDTO.BuyerId && refreshedMaxBid.Offer == bidDTO.Offer)
                {
                    await _infraRepo.UpdateMaxBid(bidDTO.AuctionId, refreshedMaxBid.Offer);
                    return refreshedMaxBid;
                    
                }
            }
                throw new Exception("Bid was not accepted");
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