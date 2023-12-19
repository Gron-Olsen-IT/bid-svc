using System.Net;
using BidAPI.Models;

namespace BidAPI.Services;

public class BidService : IBidService
{
    private readonly IBidRepo   _bidRepo;
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

    public async Task<Bid> Post(BidDTO bidDTO, string token)
    {
        try
        {
            if (bidDTO.Offer == 0)
            {
                throw new ArgumentException("Offer is 0");
            }
            if (string.IsNullOrEmpty(bidDTO.AuctionId))
            {
                throw new ArgumentException("AuctionId is empty or is null");
            }
            if (!await _infraRepo.AuctionIdExists(bidDTO.AuctionId, token))
            {
                throw new WebException("The auctionId does not match an existing auction");
            }
            if (!await _infraRepo.UserIdExists(bidDTO.BuyerId, token))
            {
                throw new WebException("The buyerId does not match a user in db");
            }
            if (bidDTO.CreatedAt < DateTime.Now.AddMinutes(-5) || bidDTO.CreatedAt > DateTime.Now.AddMinutes(5))
            {
                throw new ArgumentException("Bid was posted with a wrong timestamp (not within 5 minutes of current time)");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in post bid (validate bid part)" + e.Message);
            throw;
        }
        Bid? MaxBid;
        try
        {
            // Gets the current max bid based on a list of auctionIds
            var data = await _bidRepo.GetMaxBids(new List<string> { bidDTO.AuctionId });
            if (data == null)
            {
                MaxBid = null;
            }
            else
            {
                MaxBid = data.First();
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in post bid (get max bid part)" + e.Message);
            throw new WebException(e.Message);
        }
        try{


            // Handles scenarios where there is no current max bid
            if (MaxBid == null)
            {
                Console.WriteLine("MaxBid er null");
                // Gets the minimum price for the auction
                int minPrice = await _infraRepo.GetMinPrice(bidDTO.AuctionId, token);

                // Validates that the new bid is greater than or equal to the minimum price
                if (bidDTO.Offer < minPrice)
                {
                    throw new ArgumentException($"Bid is lower than min price of {minPrice}", "bidDTO.Offer < minPrice");
                }

                _logger.LogInformation("Calling _infraRepo.Post in BidService.Post1");
                _infraRepo.Post(bidDTO);
            }
            else
            {
                int currentMaxBid = MaxBid.Offer;

                if (bidDTO.Offer <= currentMaxBid)
                {
                    throw new ArgumentException("Bid is not greater than current max bid");
                }

                // Calculates the minimum increment for the new bid
                int minIncrement = CalculateMinIncrement(currentMaxBid);

                // Validates that the new bid is greater than the current max bid plus the minimum increment
                if (bidDTO.Offer - currentMaxBid < minIncrement)
                {
                    throw new ArgumentException("Bid post increment is too small");
                }

                _logger.LogInformation("Calling _infraRepo.Post in BidService.Post2");
                _infraRepo.Post(bidDTO);
            }
        }catch(Exception e){
            _logger.LogError("Error in post bid (post bid part)" + e.Message);
            throw;
        }

        // Checks if the new bid was accepted, and if not, tries again 20 times with a 250 ms delay
        for (int i = 0; i < 20; i++)
        {
            _logger.LogInformation("Bid was attempted " + i);
            Task.Delay(250).Wait();

            try
            {
                // Validates that the new bid was accepted. If not, the loop continues
                List<Bid>? data = new List<Bid>();
                _logger.LogInformation("auction id: " + bidDTO.AuctionId);
                data = await _bidRepo.GetMaxBids(new List<string> { bidDTO.AuctionId });
                if (data == null)
                {
                    throw new WebException("Bid was not accepted");
                }
                Bid? refreshedMaxBid = data.First();
                if (refreshedMaxBid != null && refreshedMaxBid.BuyerId == bidDTO.BuyerId && refreshedMaxBid.Offer == bidDTO.Offer)
                {
                    // If the new bid was accepted, the max bid is updated in the database and returned
                    await _infraRepo.UpdateMaxBid(bidDTO.AuctionId, refreshedMaxBid.Offer, token);
                    return refreshedMaxBid;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Couldn't find posted bid in database" + e.Message);
                continue;
            }
        }
        // Throws an exception if the new bid was not accepted after 20 tries
        throw new Exception("Bid was not accepted");
    }

    public async Task<List<Bid>?> GetMaxBids(List<string> auctionIds)
    {
        try
        {
            return await _bidRepo.GetMaxBids(auctionIds);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in BidService: GetMaxBids " + e.Message);
            throw new Exception(e.Message);
        }
    }

    public async Task<List<Bid>> Get(string auctionId)
    {
        try
        {
            return await _bidRepo.Get(auctionId);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }
    }

public async Task<Bid?> DoesBidExist(string bidId)
    {
        try
        {
            return await _bidRepo.DoesBidExist(bidId);
        }
        catch (Exception e)
        {
            if(e.Message == "Sequence contains no elements")
            {
                throw new WebException("Bid does not exist");
            }
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