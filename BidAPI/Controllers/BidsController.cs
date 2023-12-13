using Microsoft.AspNetCore.Mvc;
using BidAPI.Services;
using BidAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Security.Principal;

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
    /// Get all bids by auctionId
    /// </summary>
    /// <param name="auctionId"></param>
    /// <returns></returns>
    [HttpGet("{auctionId}")]
    public async Task<ActionResult<List<Bid>>> Get(string auctionId)
    {
        try
        {
            var bids = await _service.Get(auctionId);
            return Ok(bids);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in getting bids by auctionId");
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    /// <summary>
    /// Get max bids from a list of auctionIds
    /// </summary>
    /// <param name="auctionIds"></param>
    /// <returns></returns>
    [HttpPost("max")]
    public async Task<ActionResult<Bid>> GetMaxBids([FromBody]List<string> auctionIds)
    {
        try
        {
            var bids = await _service.GetMaxBids(auctionIds);
            return Ok(bids);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetMaxBid");
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    /// <summary>
    /// Create a bid
    /// </summary>
    /// <param name="bidDTO"></param>
    /// <returns></returns>
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
            _logger.LogError(e, "Error in CreateBid");
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }


    /// <summary>
    /// Check if bid exists in the database
    /// </summary>
    /// <param name="bidId"></param>
    /// <returns></returns>
    [HttpGet("is-bid-valid/{bidId}")]
    public async Task<ActionResult<HttpStatusCode>> IsBidValid(string bidId)
    {   
        try
        {
            _logger.LogInformation($"IsBidValid - bidId: {bidId}");
            var bid = await _service.DoesBidExists(bidId);
            if(bid == null){
                return NotFound();
            }
            return Ok(bid);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in getting bids by auctionId");
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}