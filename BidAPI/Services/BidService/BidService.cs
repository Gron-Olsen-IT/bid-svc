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

            if (string.IsNullOrEmpty(bidDTO.AuctionId))
            {
                throw new ArgumentException("AuctionId is empty or is null");
            }
            
            bool auctionExistsInDB = await _infraRepo.Get(bidDTO.AuctionId);

            if (!auctionExistsInDB)
            {
                _logger.LogError("AuctionId does not exist in the DB");
                throw new ArgumentException("The auctionId does not match an existing auction");
                
            }

            bool userIdExistsInDB = await _infraRepo.GetUserId(bidDTO.BuyerId);
            if (!userIdExistsInDB)
            {
                throw new ArgumentException("The buyerId does not match the active use");
            }
            
                
            
            
            // Henter det aktuelle maksimumsbud for auktionen
            Bid? MaxBid = await _bidRepo.GetMaxBid(bidDTO.AuctionId);
            

            // Håndterer scenarier, hvor der ikke er noget eksisterende bud
            if (MaxBid == null)
            {
                Console.WriteLine("MaxBid er null");
                // Henter den aktuelle mindstepris for auktionen
                int minPrice = await _infraRepo.GetMinPrice(bidDTO.AuctionId);

                // Validerer, om det nye bud er lavere end mindsteprisen
                if (bidDTO.Offer < minPrice)
                {
                    throw new ArgumentException("Bid is lower than min price");
                }

                _logger.LogInformation("Calling _infraRepo.Post in BidService.Post1");
                _infraRepo.Post(bidDTO);


            }
            else
            {
                // Finder det aktuelle maksimumsbud
                int currentMaxBid = MaxBid.Offer;

                // Validerer, om det nye bud er større eller lig med det aktuelle maksimumsbud
                if (bidDTO.Offer <= currentMaxBid)
                {
                    throw new ArgumentException("Bid is not greater than current max bid");
                }

                // Beregner minimumsforøgelsen baseret på det aktuelle maksimumsbud
                int minIncrement = CalculateMinIncrement(currentMaxBid);

                // Validerer, om forøgelsen af det nye bud er tilstrækkelig i forhold til minimumsforøgelsen
                if (bidDTO.Offer - currentMaxBid < minIncrement)
                {
                    throw new ArgumentException("Bid post increment is too small");
                }

                _logger.LogInformation("Calling _infraRepo.Post in BidService.Post2");
                _infraRepo.Post(bidDTO);
            }





            // Tjekker 20 gange om det nye bud blev accepteret, med 250 ms ventetid mellem hvert forsøg
            for (int i = 0; i < 20; i++)
            {
                _logger.LogInformation("Bid was attempted " + i);
                Task.Delay(250).Wait();

                try
                {
                    _logger.LogInformation("Kommer ned i try i BidService.Post");
                    // Opdaterer det aktuelle maksimumsbud og tjekker om det nye bud blev accepteret
                    Bid refreshedMaxBid = await _bidRepo.GetMaxBid(bidDTO.AuctionId);
                    if (refreshedMaxBid != null && refreshedMaxBid.BuyerId == bidDTO.BuyerId &&
                        refreshedMaxBid.Offer == bidDTO.Offer)
                    {
                        // Opdaterer det maksimale bud i infrastrukturen og returnerer det accepterede bud
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

            // Kaster en undtagelse, hvis det nye bud ikke blev accepteret efter alle forsøg
            throw new Exception("Bid was not accepted");
        }
        catch (Exception e)
        {
            // Logger fejlmeddelelsen og kaster en ArgumentException videre
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