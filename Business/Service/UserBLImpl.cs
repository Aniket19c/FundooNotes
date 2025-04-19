using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Business.Interface;
using Microsoft.AspNetCore.Identity.Data;
using Repository.DTO;
using Repository.Interface;

namespace Business.Service
{
    public class UserBLImpl :IUserBL
    {
        public IUserRL _user;
      

        public UserBLImpl(IUserRL userRL)
        {
            _user = userRL;
        }

        public async Task<ResponseDto<string>> RegisterUserAsync(UserRequest request)
        {
            return await _user.RegisterUserAsync(request);
        }
        public async Task<ResponseDto<string>> DeleteUserAsync(string email)
        {
            return await _user.DeleteUserAsync(email);
        }
        public async Task<ResponseDto<List<UserResponse>>> GetAllUsersAsync()
        {
            return await _user.GetAllUsersAsync();
        }
        public async Task<ResponseDto<LoginResponseDto>> UserLoginAsync(LoginDto request)
        {
            return await _user.UserLoginAsync(request);
        }
        public Task<ResponseDto<string>> ForgetPasswordAsync(string email) => _user.ForgetPasswordAsync(email);

        public async Task<ResponseDto<string>> ResetPasswordAsync(ResetPassword dto, string email)
        {
            return await _user.ResetPasswordAsync(dto, email);
        }

    }
}
