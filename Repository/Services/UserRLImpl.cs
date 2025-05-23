﻿using Microsoft.EntityFrameworkCore;
using Models.Entity;
using Repository.Context;
using Repository.DTO;
using Repository.Interface;
using Repository.Helper;
using Repository.Helper.CustomExceptions;
using Repository_Layer.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

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
        private readonly IDistributedCache _cache;

        public UserRLImpl(UserContext context, JwtTokenHelper jwtTokenHelper, IConfiguration configuration,RabbitMqProducer producer,RabbitMqConsumer consumer,ILogger<UserRLImpl> logger,IDistributedCache cache)
        {
            _context = context;
            _jwtHelper = jwtTokenHelper;
            _configuration = configuration;
            _producer = producer;
            _consumer = consumer;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ResponseDto<List<UserResponseDto>>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Checking Redis cache for user list");

                string cacheKey = "AllUsers";
                string cachedData = await _cache.GetStringAsync(cacheKey);

                List<UserResponseDto> userResponses;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("User list found in cache");
                    userResponses = JsonSerializer.Deserialize<List<UserResponseDto>>(cachedData);
                }
                else
                {
                    _logger.LogInformation("Cache miss: Fetching users from DB");
                    var users = await _context.Users.ToListAsync();

                    if (users == null || users.Count == 0)
                    {
                        _logger.LogWarning("No users found.");
                        throw new UserNotFoundException("No users found.");
                    }

                    userResponses = users.Select(user => new UserResponseDto
                    {
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName
                    }).ToList();

                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    };

                    string serializedData = JsonSerializer.Serialize(userResponses);
                    await _cache.SetStringAsync(cacheKey, serializedData, options);
                    _logger.LogInformation("User list cached");
                }

                return new ResponseDto<List<UserResponseDto>>
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
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("AllUsers");

                _logger.LogInformation("User deleted successfully");
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

        public async Task<ResponseDto<string>> RegisterUserAsync(UserRequestDto request)
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
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("AllUsers");

                _logger.LogInformation("User registered successfully");
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

                var token = _jwtHelper.GenerateToken(user.Email, user.ID);

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
                _logger.LogInformation($"Sending token to: {email}");
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("Email not found");
                    throw new UserNotFoundException("Email not registered.");
                }

                //var otp = new Random().Next(100000, 999999).ToString();
                var token = _jwtHelper.GenerateToken(user.Email, user.ID);

                _producer.SendOtpQueue(email, token);
                _consumer.Consume();

                _logger.LogInformation("OTP sent successfully");
                return new ResponseDto<string>
                {
                    success = true,
                    message = "token sent to your email address.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ForgetPasswordAsync");
                throw;
            }
        }

        public async Task<ResponseDto<string>> ResetPasswordAsync(ResetPasswordDto dto, string email)
        {
            try
            {
                _logger.LogInformation($"Resetting password for: {email}");
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User not found.");
                    throw new UserNotFoundException("User not found.");
                }

                if (!PasswordHelper.VerifyPassword(dto.oldPassword, user.Password))
                {
                    throw new UnauthorizedAccessException("Old password is incorrect.");
                }

                user.Password = PasswordHelper.HashPassword(dto.newPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("AllUsers");

                _logger.LogInformation("Password updated successfully");
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
