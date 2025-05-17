using System.Security.Claims;
using Business.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.DTO;
using Repository.Helper;
using Repository_Layer.Helper;

namespace Fundoo_Notes.Controllers
{
    /// <summary>
    /// Controller for managing user-related operations in the Fundoo Notes application.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserBL _userBL;
        private readonly JwtTokenHelper _jwtHelper;
        private readonly RabbitMqProducer _rabbitMqProducer;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Constructor to initialize UserController with dependencies.
        /// </summary>
        /// <param name="userBl">Business logic interface for users.</param>
        /// <param name="jwtTokenHelper">Helper for generating JWT tokens.</param>
        /// <param name="rabbitMqProducer">Producer for RabbitMQ messaging.</param>
        /// <param name="logger">Logger instance for logging messages.</param>
        public UserController(IUserBL userBl, JwtTokenHelper jwtTokenHelper, RabbitMqProducer rabbitMqProducer, ILogger<UserController> logger)
        {
            _userBL = userBl;
            _jwtHelper = jwtTokenHelper;
            _rabbitMqProducer = rabbitMqProducer;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="request">User registration data.</param>
        /// <returns>Success or error response.</returns>
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

        /// <summary>
        /// Deletes a user by email.
        /// </summary>
        /// <param name="email">Email of the user to delete.</param>
        /// <returns>Success or error response.</returns>
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

        /// <summary>
        /// Retrieves a list of all users.
        /// </summary>
        /// <returns>List of users or error response.</returns>
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

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="request">Login credentials.</param>
        /// <returns>JWT token or unauthorized response.</returns>
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

        /// <summary>
        /// Sends a password reset link to the user's email.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <returns>Response indicating result of operation.</returns>
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

        /// <summary>
        /// Resets the user's password. Requires authentication.
        /// </summary>
        /// <param name="dto">Reset password data.</param>
        /// <returns>Success or error response.</returns>
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
