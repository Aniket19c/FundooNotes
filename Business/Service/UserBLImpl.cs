using Business.Interface;
using Microsoft.Extensions.Logging; 
using Repository.DTO;
using Repository.Interface;

namespace Business.Service
{
    public class UserBLImpl : IUserBL
    {
        public IUserRL _user;
        private readonly ILogger<UserBLImpl> _logger; 

        public UserBLImpl(IUserRL userRL, ILogger<UserBLImpl> logger)
        {
            _user = userRL;
            _logger = logger;
        }

        public async Task<ResponseDto<string>> RegisterUserAsync(UserRequestDto request)
        {
            _logger.LogInformation("RegisterUserAsync called.");
            try
            {
                return await _user.RegisterUserAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in RegisterUserAsync.");
                throw;
            }
        }

        public async Task<ResponseDto<string>> DeleteUserAsync(string email)
        {
            _logger.LogInformation($"DeleteUserAsync called for email: {email}");
            try
            {
                return await _user.DeleteUserAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred in DeleteUserAsync for email: {email}");
                throw;
            }
        }

        public async Task<ResponseDto<List<UserResponseDto>>> GetAllUsersAsync()
        {
            _logger.LogInformation("GetAllUsersAsync called.");
            try
            {
                return await _user.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in GetAllUsersAsync.");
                throw;
            }
        }

        public async Task<ResponseDto<LoginResponseDto>> UserLoginAsync(LoginDto request)
        {
            _logger.LogInformation("UserLoginAsync called.");
            try
            {
                return await _user.UserLoginAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in UserLoginAsync.");
                throw;
            }
        }

        public Task<ResponseDto<string>> ForgetPasswordAsync(string email)
        {
            _logger.LogInformation($"ForgetPasswordAsync called for email: {email}");
            try
            {
                return _user.ForgetPasswordAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred in ForgetPasswordAsync for email: {email}");
                throw;
            }
        }

        public async Task<ResponseDto<string>> ResetPasswordAsync(ResetPasswordDto dto, string email)
        {
            _logger.LogInformation($"ResetPasswordAsync called for email: {email}");
            try
            {
                return await _user.ResetPasswordAsync(dto, email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred in ResetPasswordAsync for email: {email}");
                throw;
            }
        }
    }
}
