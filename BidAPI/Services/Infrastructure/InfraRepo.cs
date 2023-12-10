using System.Net;
using BidAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Expressions;

namespace BidAPI.Services;

public class InfraRepo : IInfraRepo
{
    private readonly string INFRA_CONN = Environment.GetEnvironmentVariable("INFRA_CONN") ?? "http://localhost:5000/api/infra/";

    private readonly ILogger<InfraRepo> _logger;
    private readonly IRabbitController _rabbitController;

    public InfraRepo(ILogger<InfraRepo> logger, IRabbitController rabbitController)
    {
        _logger = logger;
        _rabbitController = rabbitController;
    }
    public async Task<HttpStatusCode> UpdateMaxBid(string auctionId, int maxBid)
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.PatchAsync($"{INFRA_CONN}/auctions/{auctionId}/?maxBid={maxBid}", null);
            return response.StatusCode;
        }
        catch(Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }

    }

    public async Task<int> GetMinPrice(string auctionId)
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{INFRA_CONN}/auctions/minprice/{auctionId}");
            Console.WriteLine($"Response!!: {response}");
            return int.Parse(await response.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }

    }
    
    public async Task<bool> AuctionIdExists(string auctionId)
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{INFRA_CONN}/auctions/{auctionId}");
            Console.WriteLine($"Response!!: {response}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }

    }



    public BidDTO Post(BidDTO bidDTO)
    {
        try
        {
            _logger.LogInformation("Calling _rabbitController.SendBid in InfraRepo.Post");
            return _rabbitController.SendBid(bidDTO);
        }
        catch(Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }
        
    }
    
    public async Task<bool> UserIdExists(string userId)
    {
        try
        {
            _logger.LogInformation("attempting to get user");
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{INFRA_CONN}/users/{userId}");
            _logger.LogInformation($"get response" + response); 
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }

    }
    
}