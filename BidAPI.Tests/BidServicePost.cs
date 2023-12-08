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
        _mockmongoRepo.SetupSequence(x => x.GetMaxBid("100"))
            .ReturnsAsync(currentMaxBid)    // First call returns the current max bid
            .ReturnsAsync(bidToPost);       // Second call returns the expected bid to be posted
    
        // Setting up the mock behavior for the repository's Post method
        _mockinfraRepo.Setup(x => x.Post(bidDtoPost)).Returns(bidDtoPost);
    
        // Invoking the method being tested - posting a bid
        var postedBid = await _service.Post(bidDtoPost);
        


        // Asserting that the postedBid matches the expected bid to be posted
        Assert.AreEqual(bidToPost, postedBid);
    }


    [Test]
    public async Task BidPostIncrementToSmall()
    {
    
    // Creating a BidDTO to simulate the data sent for posting a bid
    BidDTO bidDtoPost = new BidDTO("1", "100", 55, DateTime.Now);
    
    // Creating a Bid object that represents the expected posted bid
    Bid bidToPost = new Bid(bidDtoPost);
    bidToPost.Id = "1";
    
    // Creating a BidDTO representing the current maximum bid
    BidDTO bidDtoCurrentMax = new BidDTO("1", "100", 50, DateTime.Now);
    Bid currentMaxBid = new Bid(bidDtoCurrentMax);
    
    // Setting up the mock behavior for the repository's GetMaxBid method
    // Using SetupSequence to return different values on consecutive calls
    _mockmongoRepo.SetupSequence(x => x.GetMaxBid("100"))
        .ReturnsAsync((Bid)null)
        .ReturnsAsync(currentMaxBid);   // First call returns the current max bid

    var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
    Assert.AreEqual("Bid post increment is too small", ex.Message);
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
        _mockmongoRepo.Setup(x => x.GetMaxBid("100"))
            .ReturnsAsync(currentMaxBid);   // First call returns the current max bid


        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.AreEqual("Bid is not greater than current max bid", ex.Message);
    }
    
    
      
    [Test]
    public async Task BidPostFirstOfferAccepted()
    {
        // Opretter en BidDTO for at simulere data sendt til oprettelse af et bud
        BidDTO bidDtoPost = new BidDTO("1", "100", 45, DateTime.Now);

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";

        // Opsætter en sekvens for at returnere forskellige værdier ved på hinanden følgende kald
        _mockmongoRepo.Setup(x => x.GetMaxBid("100"))
            .ReturnsAsync((Bid)null);   // Første kald returnerer det aktuelle maksimumsbud

        _mockinfraRepo.Setup(x => x.GetMinPrice("100"))
            .ReturnsAsync(50);   // Første kald returnerer den aktuelle mindstepris

        _mockinfraRepo.Setup(x => x.Post(bidDtoPost)).Returns(bidDtoPost);

        // Handling
        var postedBid = await _service.Post(bidDtoPost); // Udfører handlingen ved at forsøge at oprette budet

        // Assertion
        
        Assert.AreEqual(bidToPost, postedBid); // Asserting that the postedBid matches the expected bid to be posted
    }
    
    
    
    
    
}
    
    


    

    
