using BidAPI.Models;

namespace BidAPI.Services;

public class BidService : IBidService
{
    private readonly IBidRepo _bidRepo;
    private readonly ILogger<BidService> _logger;
    private readonly IInfraRepo _infraRepo;
    private readonly IRabbitController _rabbitController;

    public BidService(IBidRepo bidRepo, ILogger<BidService> logger, IInfraRepo infraRepo,
        IRabbitController rabbitController)
    {
        _bidRepo = bidRepo;
        _logger = logger;
        _infraRepo = infraRepo;
        _rabbitController = rabbitController;

    }

    public async Task<Bid> Post(BidDTO bidDTO)
    {
        try
        {
            int currentMaxBid = _bidRepo.GetMaxBid(bidDTO.AuctionId).Result.Offer;

            if (currentMaxBid == null)
            {
                int minPrice = _infraRepo.GetMinPrice(bidDTO.AuctionId).Result;
                if (bidDTO.Offer < minPrice)
                {
                    throw new ArgumentException("Bid is lower than min price");
                }
            }
            
            if (bidDTO.Offer <= currentMaxBid)
            {
                throw new ArgumentException("Bid is not greater than current max bid");
            }
            int minIncrement = CalculateMinIncrement(currentMaxBid);
            if (bidDTO.Offer - currentMaxBid < minIncrement)
            {
                throw new ArgumentException("Bid post increment is too small");
            }

            _logger.LogInformation("Calling _infraRepo.Post in BidService.Post");
            _infraRepo.Post(bidDTO);


            //Checking 4 times if the bid was accepted, 500ms between each attempt
            for (int i = 0; i < 20; i++)
            {
                _logger.LogInformation("Bid was attempted " + i);
                Task.Delay(250).Wait();
                try
                {
                    Bid refreshedMaxBid = await _bidRepo.GetMaxBid(bidDTO.AuctionId);
                    if (refreshedMaxBid.BuyerId == bidDTO.BuyerId && refreshedMaxBid.Offer == bidDTO.Offer)
                    {
                        await _infraRepo.UpdateMaxBid(bidDTO.AuctionId, refreshedMaxBid.Offer);
                        return refreshedMaxBid;
                    }
                }
                catch (ArgumentException e)
                {
                    _logger.LogError(e.Message);
                    continue;
                }


            }

            throw new Exception("Bid was not accepted");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new ArgumentException(e.Message);
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

    public int CalculateMinIncrement(int currentMaxBid)
    {
        if (currentMaxBid <= 100)
        {
            return 10;
        }
        else if (currentMaxBid <= 1000)
        {
            return 50;
        }

        else if (currentMaxBid <= 10000)
        {
            return 250;
        }
        else return 1000;
    
    }

}