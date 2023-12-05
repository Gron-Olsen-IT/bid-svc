using BidAPI.Models;
using System.Text;
using RabbitMQ.Client;
using System.Text.Json;

namespace BidAPI.Services;

public class RabbitController : IRabbitController
{

    private readonly string? _MQPath = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME");
    public RabbitController(string MQPath)
    {
        _MQPath = MQPath;
    }
    public BidDTO SendBid(BidDTO bidDTO)
    {
        var message = JsonSerializer.SerializeToUtf8Bytes(bidDTO);

        var factory = new ConnectionFactory { HostName = _MQPath };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "bid",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        channel.BasicPublish(exchange: string.Empty,
                             routingKey: "bid",
                             basicProperties: null,
                             body: message);

        return bidDTO;
    }

}