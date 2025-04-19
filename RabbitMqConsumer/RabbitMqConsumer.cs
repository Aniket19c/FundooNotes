using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace Repository.Helper
{
    public class RabbitMqConsumer
    {
        private readonly IConfiguration _configuration;

        public RabbitMqConsumer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Consume()
        {
            try
            {
                
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:Host"],
                    UserName = _configuration["RabbitMQ:Username"],
                    Password = _configuration["RabbitMQ:Password"],
                    Port = int.Parse(_configuration["RabbitMQ:Port"])
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: _configuration["RabbitMQ:QueueName"],
                                     durable: false,    
                                     exclusive: false,
                                     autoDelete: false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);

                        var toEmail = data["Email"];
                        var otp = data["Otp"];

                        SendEmail(toEmail, "Fundoo Notes - OTP", $"Your OTP is: {otp}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }
                };



                channel.BasicConsume(queue: _configuration["RabbitMQ:QueueName"], autoAck: true, consumer: consumer);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
               
                Console.WriteLine($"Error initializing RabbitMQ Consumer: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                
                var smtpClient = new SmtpClient(_configuration["SmtpSettings:Host"])
                {
                    Port = int.Parse(_configuration["SmtpSettings:Port"]),
                    Credentials = new NetworkCredential(_configuration["SmtpSettings:Email"], _configuration["SmtpSettings:AppPassword"]),
                    EnableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"])
                };

               
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["SmtpSettings:Email"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

               
                smtpClient.Send(mailMessage);
                Console.WriteLine($"Email sent successfully to {toEmail}.");
            }
            catch (Exception ex)
            {
               
                Console.WriteLine($"Error sending email: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            
        }
    }
}
