using System.Net;
using BidAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace BidAPI.Services;

public class InfraRepoRender : IInfraRepo
{
    private readonly string INFRA_CONN = Environment.GetEnvironmentVariable("INFRA_CONN") ?? "http://localhost:5000/api/infra/";
    public async Task<HttpStatusCode> UpdateMaxBid(string auctionId, int maxBid)
    {        
        HttpClient httpClient = new HttpClient();
        
        var response = await httpClient.PatchAsync($"{INFRA_CONN}/auctions/{auctionId}/?maxBid={maxBid}", null);
        return response.StatusCode;
    }
}