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
    public async Task<HttpStatusCode> UpdateMaxBid(string auctionId, int maxBid, string token)
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", token);
            var response = await httpClient.PatchAsync($"{INFRA_CONN}/auctions/{auctionId}/?maxBid={maxBid}", null);
            return response.StatusCode;
        }
        catch(Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }

    }

    public async Task<int> GetMinPrice(string auctionId, string token)
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", token);
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
    
    public async Task<bool> AuctionIdExists(string auctionId, string token)
    {
        try
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", token);
            var response = await httpClient.GetAsync($"{INFRA_CONN}/auctions/{auctionId}");
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
    
    public async Task<bool> UserIdExists(string userId, string token)
    {
        try
        {
            _logger.LogInformation("attempting to get user");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", token);
            var response = await httpClient.GetAsync($"{INFRA_CONN}/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw new Exception(e.Message);
        }

    }
    
}