using System.Security.Claims;
using Business.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.DTO;
using Repository.Helper;
using Repository_Layer.Helper;

namespace Fundoo_Notes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserBL _userBL;
        private readonly JwtTokenHelper _jwtHelper;
        private readonly RabbitMqProducer _rabbitMqProducer;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserBL userBl, JwtTokenHelper jwtTokenHelper, RabbitMqProducer rabbitMqProducer, ILogger<UserController> logger)
        {
            _userBL = userBl;
            _jwtHelper = jwtTokenHelper;
            _rabbitMqProducer = rabbitMqProducer;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRequestDto request)
        {
            _logger.LogInformation("Register endpoint called.");
            try
            {
                var response = await _userBL.RegisterUserAsync(request);
                return response.success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Register");
                return StatusCode(500, new ResponseDto<string>
                {
                    success = false,
                    message = ex.Message,
                    data = null
                });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(string email)
        {
            _logger.LogInformation($"Delete endpoint called with email: {email}");
            try
            {
                var response = await _userBL.DeleteUserAsync(email);
                return response.success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for email: {email}");
                return StatusCode(500, new ResponseDto<string>
                {
                    success = false,
                    message = ex.Message,
                    data = null
                });
            }
        }

        [HttpGet("getAllUsers")]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("GetAllUsers endpoint called.");
            try
            {
                var response = await _userBL.GetAllUsersAsync();
                return response.success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUsers");
                return StatusCode(500, new ResponseDto<string>
                {
                    success = false,
                    message = ex.Message,
                    data = null
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            _logger.LogInformation("Login endpoint called.");
            try
            {
                var response = await _userBL.UserLoginAsync(request);
                return response.success ? Ok(response) : Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Login");
                return StatusCode(500, new ResponseDto<string>
                {
                    success = false,
                    message = ex.Message,
                    data = null
                });
            }
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            _logger.LogInformation($"ForgetPassword endpoint called with email: {email}");
            try
            {
                var result = await _userBL.ForgetPasswordAsync(email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ForgetPassword for email: {email}");
                return StatusCode(500, new ResponseDto<string>
                {
                    success = false,
                    message = ex.Message,
                    data = null
                });
            }
        }

        [Authorize]
        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            _logger.LogInformation("ResetPassword endpoint called.");
            try
            {
                var identity = HttpContext.User.Identity as ClaimsIdentity;
                if (identity == null)
                {
                    _logger.LogWarning("ResetPassword: Invalid token");
                    return Unauthorized(new ResponseDto<string>
                    {
                        success = false,
                        message = "Invalid token",
                        data = null
                    });
                }

                var emailClaim = identity.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogWarning("ResetPassword: Email claim not found");
                    return Unauthorized(new ResponseDto<string>
                    {
                        success = false,
                        message = "Email claim not found",
                        data = null
                    });
                }

                string email = emailClaim.Value;
                var result = await _userBL.ResetPasswordAsync(dto, email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPassword");
                return StatusCode(500, new ResponseDto<string>
                {
                    success = false,
                    message = ex.Message,
                    data = null
                });
            }
        }
    }
}
