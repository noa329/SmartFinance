using Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.interfaces
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAll();
        Task<UserDto?> GetById(int id);
        Task<UserDto> Add(UserCreateDto dto);
        Task<UserDto> Create(UserCreateDto dto);
        Task<UserDto> Update(int id, UserUpdateDto dto);
        Task Delete(int id);
        Task<AuthResponseDto> Register(UserCreateDto dto);
        Task<AuthResponseDto?> Login(UserLoginDto dto);
        Task<AuthResponseDto> GoogleLoginOrRegister(string googleToken);
        Task SaveGmailRefreshToken(int userId, string refreshToken);

    }
}
