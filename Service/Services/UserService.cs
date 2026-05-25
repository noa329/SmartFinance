using AutoMapper;
using Common.Dto;
using Microsoft.IdentityModel.Tokens;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using Service.Exceptions;
namespace Service.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IMapper _mapper;
        private readonly IInitialLoginService _initialLoginService;
        private readonly IConfiguration _configuration;

        public UserService(IRepository<User> userRepository, IMapper mapper,
            IInitialLoginService initialLoginService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _initialLoginService = initialLoginService;
            _configuration = configuration;
        }

        // ============================
        // GET ALL
        // ============================
        public async Task<List<UserDto>> GetAll()
        {
            var users = await _userRepository.GetAll();
            return _mapper.Map<List<UserDto>>(users);
        }

        // ============================
        // GET BY ID
        // ============================
        public async Task<UserDto?> GetById(int id)
        {
            var user = await _userRepository.GetById(id);

            if (user == null)
                return null;

            return _mapper.Map<UserDto>(user);
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }


        // ============================
        // CREATE
        // ============================
        public async Task<UserDto> Create(UserCreateDto dto)
        {
            var user = _mapper.Map<User>(dto);


            user.PasswordHash = HashPassword(dto.Password);

            await _userRepository.Add(user);
           

            return _mapper.Map<UserDto>(user);
        }

        // Add מפנה ל־Create כדי למנוע כפילות
        public async Task<UserDto> Add(UserCreateDto dto)
        {
            return await Create(dto);
        }

        // ============================
        // UPDATE
        // ============================
        public async Task<UserDto> Update(int id, UserUpdateDto dto)
        {
            var user = await _userRepository.GetById(id);

            if (user == null)
                throw new Exception("User not found");

            var existing = await _userRepository.Find(u => u.Email.Equals(dto.Email, StringComparison.CurrentCultureIgnoreCase) && u.Id != id);
       
            if (existing.Any())
                throw new InvalidOperationException("Email already exists");


            _mapper.Map(dto, user);

            await _userRepository.Update(user.Id, user);

            return _mapper.Map<UserDto>(user);
        }

        // ============================
        // DELETE
        // ============================
        public async Task Delete(int id)
        {
            var user = await _userRepository.GetById(id);

            if (user == null)
                throw new Exception("User not found");

            await _userRepository.Delete(user.Id);
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };
            var keyStr = _configuration["JwtSettings:Key"];
            if (string.IsNullOrEmpty(keyStr)) throw new Exception("JWT Key is missing in config");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponseDto> GoogleLoginOrRegister(string googleToken)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                googleToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] ??
                throw new Exception("Google ClientId missing") }
                });

            var email = (payload.Email ?? throw new Exception("Email missing from Google token"))
                .ToLower().Trim();

            var users = await _userRepository.Find(u => u.Email == email);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FullName = payload.Name ?? email,
                    PasswordHash = "GOOGLE_AUTH_USER",
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    PictureUrl = payload.Picture,
                };

                await _userRepository.Add(user);
                await _initialLoginService.InitializeUserCategories(user.Id);
            }
            else
            {
                if (!string.IsNullOrEmpty(payload.Picture))
                {
                    user.PictureUrl = payload.Picture;
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.Update(user.Id, user);
            }

            var appToken = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                Token = appToken,
                GoogleAccessToken = googleToken
            };
        }
        public async Task<AuthResponseDto> Register(UserCreateDto dto)
        {
            dto.Email = dto.Email.ToLower().Trim();
            var existing = await _userRepository.Find(u => u.Email == dto.Email);
            if (existing.Any())
            {
                throw new BadRequestException("כתובת האימייל הזו כבר רשומה במערכת.");
            }

            var user = _mapper.Map<User>(dto);
            user.PasswordHash = HashPassword(dto.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.LastLoginAt = DateTime.UtcNow;

            await _userRepository.Add(user);
            await _initialLoginService.InitializeUserCategories(user.Id);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                Token = token
            };
        }
        public async Task<AuthResponseDto?> Login(UserLoginDto dto)
        {
            dto.Email = dto.Email.ToLower().Trim();
            var users = await _userRepository.Find(u => u.Email == dto.Email);
            var user = users.FirstOrDefault();

            if (user == null)
                return null;

            var hashed = HashPassword(dto.Password);
            if (user.PasswordHash != hashed)
                return null;

            user.LastLoginAt = DateTime.UtcNow;

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                Token = token
            };
        }
        public async Task SaveGmailRefreshToken(int userId, string refreshToken)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null) throw new Exception("User not found");
            user.GmailToken = refreshToken;
            await _userRepository.Update(userId, user);
        }
    }
}