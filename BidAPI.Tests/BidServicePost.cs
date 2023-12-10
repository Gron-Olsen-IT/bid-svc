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
        Assert.That(postedBid, Is.EqualTo(bidToPost));
    }


    [Test]
    public async Task BidPostIncrementTooSmall()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 55, DateTime.Now); // New bid
        Bid currentMaxBid = new Bid(new BidDTO("2", "100", 50, DateTime.Now)); // Current max bid

        _mockmongoRepo.Setup(x => x.GetMaxBid("100"))
            .ReturnsAsync(currentMaxBid); // Existing max bid

        _mockinfraRepo.Setup(x => x.Post(It.IsAny<BidDTO>()))
            .Returns((BidDTO)null); // No bid posted due to error

        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.That(ex.Message, Is.EqualTo("Bid post increment is too small"));
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
        _mockmongoRepo.SetupSequence(x => x.GetMaxBid("100"))
            .ReturnsAsync(currentMaxBid)   // First call returns the current max bid
            .ReturnsAsync(bidToPost);      // Second call returns the expected bid to be posted


        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost));
        Assert.That(ex.Message, Is.EqualTo("Bid is not greater than current max bid"));
    }


    [Test]
    public async Task BidPostFirstOfferAccepted()
    {
        BidDTO bidDTO = new BidDTO("1", "100", 55, DateTime.Now);

        Bid bidToPost = new Bid(bidDTO);
        bidToPost.Id = "1";

        _mockmongoRepo.SetupSequence(x => x.GetMaxBid("100"))
            .ReturnsAsync((Bid)null) // Simulates no existing bids
            .ReturnsAsync(bidToPost);

        _mockinfraRepo.Setup(x => x.GetMinPrice("100"))
            .ReturnsAsync(50); // Simulates minimum price

        _mockinfraRepo.Setup(x => x.Post(bidDTO))
            .Returns(bidDTO); // Simulates posting of the bid

        var postedBid = await _service.Post(bidDTO);

        Assert.That(postedBid.BuyerId, Is.EqualTo(bidDTO.BuyerId));
        Assert.That(postedBid.Offer, Is.EqualTo(bidDTO.Offer));
    }






}







