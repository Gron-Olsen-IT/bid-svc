using Microsoft.AspNetCore.Mvc;
using BidAPI.Services;
using BidAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Security.Principal;
using Swashbuckle.AspNetCore.Annotations;

namespace BidAPI.Controllers;

[Authorize]
[ApiController]
[Route("bids")]
public class BidsController : ControllerBase
{

    private readonly ILogger<BidsController> _logger;
    private readonly IBidService _service;



    public BidsController(ILogger<BidsController> logger, IBidService service)
    {
        _service = service;
        _logger = logger;
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var ips = System.Net.Dns.GetHostAddresses(hostName);
            var _ipaddr = ips.First().MapToIPv4().ToString();
            _logger.LogInformation(1, $"BidService responding from {_ipaddr}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in BidsController");
        }

    }

    /// <summary>
    /// Get all bids on provided auctionId
    /// </summary>
    /// <param name="auctionId"></param>
    [SwaggerResponse(200, "Returns a list of bids", typeof(List<Bid>))]
    [SwaggerResponse(400, "Invalid paramater input")]
    [SwaggerResponse(404, "Missing resource or data")]
    [SwaggerResponse(500, "Internal Server Error")]
    [HttpGet("{auctionId}")]
    public async Task<ActionResult<List<Bid>>> Get(string auctionId)
    {
        if (string.IsNullOrEmpty(auctionId) || auctionId.Length != 24)
        {
            _logger.LogInformation($"Invalid auctionId: {auctionId}");
            return StatusCode(StatusCodes.Status400BadRequest, "Invalid auctionId");
        }
        try
        {
            var bids = await _service.Get(auctionId);
            return Ok(bids);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in BidController: Get{id} ", e.Message);
            if (e is ArgumentNullException || e is ArgumentException)
            {
                _logger.LogError("400: Bad Requestion in BidController: Get{id} ", e.Message);
                return StatusCode(StatusCodes.Status400BadRequest, e.Message);
            }
            if (e is WebException)
            {
                _logger.LogError("404: Not Found in BidController: Get{id} ", e.Message);
                return StatusCode(StatusCodes.Status404NotFound, e.Message);
            }
            {
                _logger.LogError("500: Internal Server Error in BidController: Get{id} ", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }

    /// <summary>
    /// Get max bids from a list of auctionIds
    /// </summary>
    /// <param name="auctionIds"></param>
    [SwaggerResponse(200, "Returns a list containing the highest bid for each input auctionId", typeof(List<Bid>))]
    [SwaggerResponse(400, "Invalid paramater input")]
    [SwaggerResponse(404, "Missing resource or data")]
    [SwaggerResponse(500, "Internal Server Error")]
    [HttpPost("max")]
    public async Task<ActionResult<Bid>> GetMaxBids([FromBody] List<string> auctionIds)
    {
        if (auctionIds == null || auctionIds.Count == 0)
        {
            _logger.LogInformation($"AuctionIds is null or empty");
            return StatusCode(StatusCodes.Status400BadRequest, "AuctionIds is null or empty");
        }
        if (auctionIds.Any(a => string.IsNullOrEmpty(a) || a.Length != 24))
        {
            _logger.LogInformation($"Invalid auctionId: {auctionIds}");
            return StatusCode(StatusCodes.Status400BadRequest, "Invalid auctionId");
        }
        try
        {
            var bids = await _service.GetMaxBids(auctionIds);
            if (bids == null || bids.Count == 0)
            {
                return StatusCode(404, "No bids found");
            }
            return Ok(bids);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in BidController: Max", e.Message);
            if (e is ArgumentNullException || e is ArgumentException)
            {
                _logger.LogError("400: Bad Requestion in BidController: Max ", e.Message);
                return StatusCode(400, e.Message);
            }
            if (e is WebException)
            {
                _logger.LogError("404: Not Found in BidController: Max ", e.Message);
                return StatusCode(404, e.Message);
            }
            {
                _logger.LogError("500: Internal Server Error in BidController: Max ", e.Message);
                return StatusCode(500, e.Message);
            }
        }
    }

    /// <summary>
    /// Create a bid
    /// </summary>
    /// <param name="bidDTO"></param>
    [SwaggerResponse(200, "Create a bid", typeof(Bid))]
    [SwaggerResponse(400, "Invalid paramater input")]
    [SwaggerResponse(404, "Missing resource or data")]
    [SwaggerResponse(500, "Internal Server Error")]
    [HttpPost]
    public async Task<ActionResult<Bid>> Post([FromBody] BidDTO bidDTO)
    {
        string? token;
        try
        {
            token = Request.Headers["Authorization"];
            _logger.LogInformation($"Controller.Post - BidDTO: {bidDTO}");
            var bid = await _service.Post(bidDTO, token!);
            return Ok(bid);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in BidController: Post", e.Message);
            if (e is ArgumentNullException || e is ArgumentException)
            {
                _logger.LogError("400: Bad Requestion in BidController: Post ", e.Message);
                return StatusCode(StatusCodes.Status400BadRequest, e.Message);
            }
            if (e is WebException)
            {
                _logger.LogError("404: Not Found in BidController: Post ", e.Message);
                return StatusCode(StatusCodes.Status404NotFound, e.Message);
            }
            {
                _logger.LogError("500: Internal Server Error in BidController: Post ", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }


    /// <summary>
    /// Check if bid exists in the database
    /// </summary>
    /// <param name="bidId"></param>
    [SwaggerResponse(200, "Returns the validated bid", typeof(Bid))]
    [SwaggerResponse(400, "Invalid paramater input")]
    [SwaggerResponse(404, "Missing resource or data")]
    [SwaggerResponse(500, "Internal Server Error")]
    [HttpGet("is-bid-valid/{bidId}")]
    public async Task<ActionResult<HttpStatusCode>> IsBidValid(string bidId)
    {
        if (string.IsNullOrEmpty(bidId) || bidId.Length != 24)
        {
            _logger.LogInformation($"Invalid auctionId: {bidId}");
            return StatusCode(StatusCodes.Status400BadRequest, "Invalid bidId");
        }
        try
        {
            _logger.LogInformation($"IsBidValid - bidId: {bidId}");
            var bid = await _service.DoesBidExist(bidId);
            if (bid == null)
            {
                return NotFound();
            }
            return Ok(bid);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in BidController: IsBidValid", e.Message);
            if (e is ArgumentNullException || e is ArgumentException)
            {
                _logger.LogError($"400: Bad Requestion in BidController: IsBidValid ", e.Message);
                return StatusCode(StatusCodes.Status400BadRequest, e.Message);
            }
            if (e is WebException)
            {
                _logger.LogError($"404: Not Found in BidController: IsBidValid ", e.Message);
                return StatusCode(StatusCodes.Status404NotFound, e.Message);
            }
            {
                _logger.LogError($"500: Internal Server Error in BidController: IsBidValid ", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }

}