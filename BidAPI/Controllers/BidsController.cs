using Microsoft.AspNetCore.Mvc;
using BidAPI.Services;
using BidAPI.Models;

namespace BidAPI.Controllers;

[ApiController]
[Route("[controller]")]
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

    [HttpGet("max/{auctionId}")]
    public async Task<ActionResult<Bid>> GetMaxBid(string auctionId)
    {
        try
        {
            var bid = await _service.GetMaxBid(auctionId);
            return Ok(bid);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetMaxBid");
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<Bid>> Post([FromBody] BidDTO bidDTO)
    {
        try
        {
            _logger.LogInformation($"Controller.Post - BidDTO: {bidDTO}");
            var bid = await _service.Post(bidDTO);
            return Ok(bid);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in CreateBid");
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}