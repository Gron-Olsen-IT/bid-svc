using BidAPI.Models;
using System.Text;
using RabbitMQ.Client;
using System.Text.Json;

namespace BidAPI.Services;

public interface IRabbitController
{
    public BidDTO SendBid(BidDTO bid);
}