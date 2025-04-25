using Repository.DTO;

namespace Repository.Interface
{
    public interface IUserRL
    {
        Task<ResponseDto<string>> RegisterUserAsync(UserRequestDto request);
        Task<ResponseDto<string>>DeleteUserAsync(string email);
        Task<ResponseDto<List<UserResponseDto>>> GetAllUsersAsync();
        Task<ResponseDto<LoginResponseDto>> UserLoginAsync(LoginDto request);
        Task<ResponseDto<string>> ForgetPasswordAsync(string email);
        Task<ResponseDto<string>> ResetPasswordAsync(ResetPasswordDto dto, string email);
    }
}
