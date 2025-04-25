using Repository.DTO;

namespace Business.Interface
{
    public interface IUserBL
    {
        Task<ResponseDto<string>> RegisterUserAsync(UserRequestDto request);
        Task<ResponseDto<string>> DeleteUserAsync(string email);
        Task<ResponseDto<List<UserResponseDto>>> GetAllUsersAsync();

        Task<ResponseDto<LoginResponseDto>> UserLoginAsync(LoginDto loginRequest);
        Task<ResponseDto<string>> ForgetPasswordAsync(string email);
        Task<ResponseDto<string>> ResetPasswordAsync(ResetPasswordDto dto, string email);


    }
}
