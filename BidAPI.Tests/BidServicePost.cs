using System.Net;
using Moq;
using MongoDB.Driver;
using BidAPI.Models;
using BidAPI.Services;
using Microsoft.Extensions.Logging;


namespace BidAPI.Tests;


public class BidServicePost
{
    private Mock<IInfraRepo> _mockinfraRepo;
    private Mock<IBidRepo> _mockmongoRepo;
    private Mock<ILogger<BidService>> _mockLogger;
    private Mock<IRabbitController> _mockRabbitController;
    private BidService _service;


    [SetUp]
    public void Setup()
    {
        _mockinfraRepo = new Mock<IInfraRepo>();
        _mockmongoRepo = new Mock<IBidRepo>();
        _mockLogger = new Mock<ILogger<BidService>>();
        _mockRabbitController = new Mock<IRabbitController>();
        _service = new BidService(_mockmongoRepo.Object, _mockLogger.Object, _mockinfraRepo.Object, _mockRabbitController.Object);
    }


    

[Test]
   public async Task BidPostSuccesfull()
   {
   // Creating a BidDTO to simulate the data sent for posting a bid
   BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now);
   
   // Creating a Bid object that represents the expected posted bid
   Bid bidToPost = new Bid(bidDtoPost);
   bidToPost.Id = "1";
   
   // Creating a BidDTO representing the current maximum bid
   BidDTO bidDtoCurrentMax = new BidDTO("1", "100", 50, DateTime.Now);
   Bid currentMaxBid = new Bid(bidDtoCurrentMax);
   
   // Setting up the mock behavior for the repository's GetMaxBid method
   // Using SetupSequence to return different values on consecutive calls
   _mockmongoRepo.SetupSequence(bidRepo => bidRepo.GetMaxBid("100"))
   .ReturnsAsync(currentMaxBid)  
   .ReturnsAsync(bidToPost);       
   
   // Setting up the mock behavior for the repository's Post method
   _mockinfraRepo.Setup(infraRepo => infraRepo.Post(bidDtoPost)).Returns(bidDtoPost);
   _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId)).ReturnsAsync(true);
   _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId)).ReturnsAsync(true);
   
   // Invoking the method being tested - posting a bid
   var postedBid = await _service.Post(bidDtoPost);
   
   
   
   
   // Asserting that the postedBid matches the expected bid to be posted
   Assert.AreEqual(bidToPost, postedBid);
   }
   
   
    
    
    
    [Test]
    public async Task BidPostOfferIsLessThanCurrentMaxBid()
    {
    
        // Creating a BidDTO to simulate the data sent for posting a bid
        BidDTO bidDtoPost = new BidDTO("1", "100", 45, DateTime.Now);
    
        // Creating a Bid object that represents the expected posted bid
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";
        
        // Creating a BidDTO representing the current maximum bid
        BidDTO bidDtoCurrentMax = new BidDTO("1", "100", 50, DateTime.Now);
        Bid currentMaxBid = new Bid(bidDtoCurrentMax);

        
        // Setting up the mock behavior for the repository's GetMaxBid method
        // Using SetupSequence to return different values on consecutive calls
        _mockmongoRepo.SetupSequence(bidRepo => bidRepo.GetMaxBid("100"))
            .ReturnsAsync(currentMaxBid)  
            .ReturnsAsync(bidToPost);       
   
        // Setting up the mock behavior for the repository's Post method
        _mockinfraRepo.Setup(infraRepo => infraRepo.Post(bidDtoPost)).Returns(bidDtoPost);
        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId)).ReturnsAsync(true);



        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.AreEqual("Bid is not greater than current max bid", ex.Message);
    }
    
    
    
    [Test]
    public async Task BidPostFirstOfferAccepted()
    {
        // Opretter en BidDTO for at simulere data sendt til oprettelse af et bud
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now);

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";

     
        _mockinfraRepo.SetupSequence(infraRepo => infraRepo.GetMinPrice("100"))
            .ReturnsAsync(10);

        // Setting up the mock behavior for the repository's GetMaxBid method to return a value
        
        _mockmongoRepo.SetupSequence(bidRepo => bidRepo.GetMaxBid("100"))
            .ReturnsAsync((Bid?)null)
            .ReturnsAsync(bidToPost);

        

        // Setting up the mock behavior for other necessary methods
        _mockinfraRepo.Setup(infraRepo => infraRepo.Post(bidDtoPost)).Returns(bidDtoPost);
        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UpdateMaxBid(bidDtoPost.AuctionId, bidDtoPost.Offer)).ReturnsAsync(HttpStatusCode.OK);

        // Handling
        var postedBid = await _service.Post(bidDtoPost);

        // Assertion
        Assert.AreEqual(bidToPost, postedBid);
    }

    [Test]

    public async Task BidPostUserDoesntExist()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now);
        
        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";

        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId)).ReturnsAsync(false);
        
        //act

        //assert
     
        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.AreEqual("The buyerId does not match a user in db", ex.Message);
        
    }
    
    
    [Test]
    public async Task BidPostAuctionDoesntExist()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now);
        
        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";

        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId)).ReturnsAsync(false);
        
        //act
        
        
        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.AreEqual("The auctionId does not match an existing auction", ex.Message);
        
        
    }

    [Test]
    public async Task PostBidCreatedAtIsWrongTime()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now.AddMinutes(-10));
        
        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";
        
        
        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId)).ReturnsAsync(true);

        
        //act
        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.AreEqual("Bid was posted with a wrong timestamp (not within 5 minutes of current time)", ex.Message);

        
    }


    [Test]
    public async Task PostBidOfferIsMissingFromRequest()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 0, DateTime.Now);

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";
        
       

        
        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.AreEqual("Offer is 0", ex.Message);
        
        
        
    }
    
  
}
    
   
    

    
    


    

    
