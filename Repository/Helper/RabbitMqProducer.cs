using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Repository.Helper
{
    public class RabbitMqProducer
    {
        private readonly IConfiguration? _configuration;
        public RabbitMqProducer(IConfiguration? configuration   )
        {
            _configuration = configuration;
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
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"RabbitMQ send failed: {ex.Message}");
                throw;
            }
        }

    }
}
