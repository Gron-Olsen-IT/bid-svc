using BidAPI.Models;
using System.Text;
using RabbitMQ.Client;
using System.Text.Json;

namespace BidAPI.Services;

public class RabbitController : IRabbitController
{

    private readonly string? _MQPath;
    private readonly ILogger<RabbitController> _logger;
    public RabbitController(ILogger<RabbitController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _MQPath = configuration["RABBITMQ_HOSTNAME"];
        
    }
    public BidDTO SendBid(BidDTO bidDTO)
    {
        _logger.LogInformation($"Sending bid with value: {bidDTO.Offer} to RabbitMQ");
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