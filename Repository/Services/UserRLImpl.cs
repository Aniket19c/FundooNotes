using Microsoft.EntityFrameworkCore;
using Models.Entity;
using Repository.Context;
using Repository.DTO;
using Repository.Interface;
using Repository.Helper;
using Repository.Helper.CustomExceptions;
using Repository_Layer.Helper;
using static Repository.Helper.CustomExceptions.UserAlreadyExistsException;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Repository.Services
{
    public class UserRLImpl : IUserRL
    {
        private readonly UserContext _context;
        private readonly JwtTokenHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly RabbitMqProducer _producer;
        private readonly RabbitMqConsumer _consumer;
        private readonly ILogger<UserRLImpl> _logger;

        public UserRLImpl(UserContext context, JwtTokenHelper jwtTokenHelper, IConfiguration configuration, RabbitMqProducer producer, RabbitMqConsumer consumer,ILogger<UserRLImpl> logger)
        {
            _context = context;
            _jwtHelper = jwtTokenHelper;
            _configuration = configuration;
            _producer = producer;
            _consumer = consumer;
            _logger = logger;
        }

        public async Task<ResponseDto<List<UserResponse>>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all users from database");
                var users = await _context.Users.ToListAsync();

                if (users == null || users.Count == 0)
                {
                    _logger.LogWarning("No users found.");
                    throw new UserNotFoundException("No users found.");
                }

                var userResponses = users.Select(user => new UserResponse
                {
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName
                }).ToList();
                _logger.LogInformation("Users fetched successfully");
                return new ResponseDto<List<UserResponse>>
                {
                    success = true,
                    message = "Users fetched successfully",
                    data = userResponses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetAllUsersAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> DeleteUserAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Attempting to delete user with email: {email}");
                var user = await _context.Users.SingleOrDefaultAsync(e => e.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User not found during deletion");

                    throw new UserNotFoundException();
                }

                _context.Users.Remove(user);
                _logger.LogInformation("User deleted successfully");
                await _context.SaveChangesAsync();

                return new ResponseDto<string>
                {
                    success = true,
                    message = "User deleted successfully",
                    data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in DeleteUserAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> RegisterUserAsync(UserRequest request)
        {
            try
            {
                _logger.LogInformation("Registering new user");
                var user = await _context.Users.SingleOrDefaultAsync(e => e.Email == request.email);
                if (user != null)
                {
                    _logger.LogWarning("User already exists");
                    throw new UserAlreadyExistsException();
                }

                var userEntity = new UserEntity
                {
                    Email = request.email,
                    Password = PasswordHelper.HashPassword(request.password),
                    FirstName = request.firstName,
                    LastName = request.lastName
                };

                await _context.Users.AddAsync(userEntity);
                _logger.LogInformation("User registered successfully");
                await _context.SaveChangesAsync();

                return new ResponseDto<string>
                {
                    success = true,
                    message = "User registered successfully",
                    data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in RegisterUserAsync");
                throw;
            }
        }

        public async Task<ResponseDto<LoginResponseDto>> UserLoginAsync(LoginDto request)
        {
            try
            {
                _logger.LogInformation($"Login attempt for user: {request.email}");
                var user = await _context.Users.SingleOrDefaultAsync(e => e.Email == request.email);

                if (user == null || !PasswordHelper.VerifyPassword(request.password, user.Password))
                {
                    _logger.LogWarning("Invalid email or password");
                    throw new UnauthorizedAccessException("Invalid email or password.");
                }

                var token = _jwtHelper.GenerateToken(user.Email,user.ID);

                var loginResponse = new LoginResponseDto
                {
                    Email = user.Email,
                    Token = token
                };
                _logger.LogInformation("Login successful");

                return new ResponseDto<LoginResponseDto>
                {
                    success = true,
                    message = "Login successful",
                    data = loginResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UserLoginAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> ForgetPasswordAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Sending OTP to: {email}");
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("Email not found");
                    throw new UserNotFoundException("Email not registered.");
                }

                var otp = new Random().Next(100000, 999999).ToString();

                _producer.SendOtpQueue(email, otp);
                _logger.LogInformation("OTP sent successfully");
                _consumer.Consume();

                return new ResponseDto<string>
                {
                    success = true,
                    message = "OTP sent to your email address.",
                    data = null
                };
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Exception in ForgetPasswordAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> ResetPasswordAsync(ResetPassword dto, string email)
        {
            try
            {
                _logger.LogInformation($"Resetting password for: {email}");
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("Incorrect old password");


                    throw new UserNotFoundException("User not found.");
                }

                if (!PasswordHelper.VerifyPassword(dto.oldPassword, user.Password))
                {
                    throw new UnauthorizedAccessException("Old password is incorrect.");
                }

                user.Password = PasswordHelper.HashPassword(dto.newPassword);
                _context.Users.Update(user);
                _logger.LogInformation("Password updated successfully");
                await _context.SaveChangesAsync();

                return new ResponseDto<string>
                {
                    success = true,
                    message = "Password updated successfully",
                    data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ResetPasswordAsync");
                throw;
            }
        }
    }
}