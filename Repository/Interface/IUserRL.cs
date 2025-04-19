using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Data;
using Repository.DTO;


namespace Repository.Interface
{
    public interface IUserRL
    {

        Task<ResponseDto<string>> RegisterUserAsync(UserRequest request);
        Task<ResponseDto<string>>DeleteUserAsync(string email);
        Task<ResponseDto<List<UserResponse>>> GetAllUsersAsync();
        Task<ResponseDto<LoginResponseDto>> UserLoginAsync(LoginDto request);
        Task<ResponseDto<string>> ForgetPasswordAsync(string email);
        Task<ResponseDto<string>> ResetPasswordAsync(ResetPassword dto, string email);




    }
}
