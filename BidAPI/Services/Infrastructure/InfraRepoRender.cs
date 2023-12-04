using System.Net;
using BidAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace BidAPI.Services;

public class InfraRepoRender : IInfraRepo
{
    private readonly string INFRA_CONN = Environment.GetEnvironmentVariable("INFRA_CONN") ?? "http://localhost:5000/api/infra/";
    public async Task<HttpStatusCode> UpdateMaxBid(string auctionId, int maxBid)
    {
        Console.WriteLine(INFRA_CONN + auctionId);
        Console.WriteLine("UpdateMaxBid" + auctionId + " " + maxBid);
        
        HttpClient httpClient = new HttpClient();
        var body = "{\"currentMaxBid\":" + maxBid + "}";
        var response = await httpClient.PutAsJsonAsync(INFRA_CONN + auctionId, body);
        return response.StatusCode;
    }
}