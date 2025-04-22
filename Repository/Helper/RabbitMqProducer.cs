using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Repository.Helper
{
    public class RabbitMqProducer
    {
        private readonly IConfiguration? _configuration;
        private readonly ILogger<RabbitMqProducer> _logger;

        public RabbitMqProducer(IConfiguration? configuration, ILogger<RabbitMqProducer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void SendOtpQueue(string email, string otp)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"],
                    Port = int.Parse(_configuration["RabbitMQ:Port"]),
                    UserName = _configuration["RabbitMQ:Username"],
                    Password = _configuration["RabbitMQ:Password"]
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: _configuration["RabbitMQ:QueueName"],
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var message = JsonSerializer.Serialize(new { Email = email, Otp = otp });
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: _configuration["RabbitMQ:QueueName"],
                                     basicProperties: null,
                                     body: body);

                _logger.LogInformation($"OTP message published to queue for {email}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RabbitMQ publish failed for email: {email}");
                throw;
            }
        }
    }
}
