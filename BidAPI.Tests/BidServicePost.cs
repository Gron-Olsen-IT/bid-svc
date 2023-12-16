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
    private string _token;


    [SetUp]
    public void Setup()
    {
        _mockinfraRepo = new Mock<IInfraRepo>();
        _mockmongoRepo = new Mock<IBidRepo>();
        _mockLogger = new Mock<ILogger<BidService>>();
        _mockRabbitController = new Mock<IRabbitController>();
        _service = new BidService(_mockmongoRepo.Object, _mockLogger.Object, _mockinfraRepo.Object, _mockRabbitController.Object);
        _token = "token";
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
        _mockmongoRepo.SetupSequence(bidRepo => bidRepo.GetMaxBids(new List<string> { "100" }))
        .ReturnsAsync(new List<Bid?> { currentMaxBid })
        .ReturnsAsync(new List<Bid?> { bidToPost });

        // Setting up the mock behavior for the repository's Post method
        _mockinfraRepo.Setup(infraRepo => infraRepo.Post(bidDtoPost)).Returns(bidDtoPost);
        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId, _token)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId, _token)).ReturnsAsync(true);

        // Invoking the method being tested - posting a bid
        var postedBid = await _service.Post(bidDtoPost, _token);




        // Asserting that the postedBid matches the expected bid to be posted
        Assert.That(postedBid, Is.EqualTo(bidToPost));
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
        _mockmongoRepo.SetupSequence(bidRepo => bidRepo.GetMaxBids(new List<string> { "100" })).ReturnsAsync(new List<Bid?> { currentMaxBid }).ReturnsAsync(new List<Bid?> { bidToPost });

        // Setting up the mock behavior for the repository's Post method
        _mockinfraRepo.Setup(infraRepo => infraRepo.Post(bidDtoPost)).Returns(bidDtoPost);
        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId, _token)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId, _token)).ReturnsAsync(true);



        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost, _token));
        Assert.That(ex.Message, Is.EqualTo("Bid is not greater than current max bid"));
    }

    [Test]
    public async Task BidPostFirstOfferAccepted()
    {
        // Opretter en BidDTO for at simulere data sendt til oprettelse af et bud
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now);

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost)
        {
            Id = "0"
        };

        // Setting up the mock behavior for the repository's GetMaxBid method to return a value
        List<Bid>? maxBid = null;
        List<string> auctionIds = new List<string> { "100" };
        
        // Using SetupSequence to return different values on consecutive calls
        _mockmongoRepo.SetupSequence(bidRepo => bidRepo.GetMaxBids(auctionIds)).
        ReturnsAsync(maxBid).
        ReturnsAsync(new List<Bid?> { bidToPost }!);



        // Setting up the mock behavior for other necessary methods
        _mockinfraRepo.Setup(infraRepo => infraRepo.Post(bidDtoPost)).Returns(bidDtoPost);
        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId, _token)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId, _token)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UpdateMaxBid(bidDtoPost.AuctionId, bidDtoPost.Offer, _token)).ReturnsAsync(HttpStatusCode.OK);
        _mockinfraRepo.Setup(infraRepo => infraRepo.GetMinPrice(bidDtoPost.AuctionId, _token)).ReturnsAsync(10);

        // Handling
        var postedBid = await _service.Post(bidDtoPost, _token);

        // Assertion
        Assert.That(postedBid, Is.EqualTo(bidToPost));
    }

    [Test]

    public async Task BidPostUserDoesntExist()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now);

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";

        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId, _token)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId, _token)).ReturnsAsync(false);

        //act

        //assert

        var ex = Assert.ThrowsAsync<WebException>(() => _service.Post(bidDtoPost, _token));
        Assert.That(ex.Message, Is.EqualTo("The buyerId does not match a user in db"));

    }


    [Test]
    public async Task BidPostAuctionDoesntExist()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now);

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";

        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId, _token)).ReturnsAsync(false);

        //act


        var ex = Assert.ThrowsAsync<WebException>(() => _service.Post(bidDtoPost, _token));
        Assert.That(ex.Message, Is.EqualTo("The auctionId does not match an existing auction"));


    }

    [Test]
    public async Task PostBidCreatedAtIsWrongTime()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 60, DateTime.Now.AddMinutes(-10));

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";


        _mockinfraRepo.Setup(infraRepo => infraRepo.AuctionIdExists(bidDtoPost.AuctionId, _token)).ReturnsAsync(true);
        _mockinfraRepo.Setup(infraRepo => infraRepo.UserIdExists(bidDtoPost.BuyerId, _token)).ReturnsAsync(true);


        //act
        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost, _token));
        Assert.That(ex.Message, Is.EqualTo("Bid was posted with a wrong timestamp (not within 5 minutes of current time)"));


    }


    [Test]
    public async Task PostBidOfferIsMissingFromRequest()
    {
        BidDTO bidDtoPost = new BidDTO("1", "100", 0, DateTime.Now);

        // Opretter et Bid-objekt, der repræsenterer det forventede oprettede bud
        Bid bidToPost = new Bid(bidDtoPost);
        bidToPost.Id = "1";




        var ex = Assert.ThrowsAsync<ArgumentException>(() => _service.Post(bidDtoPost, _token));
        Assert.That(ex.Message, Is.EqualTo("Offer is 0"));



    }


}