using AutoMapper;
using Common.Dto;
using Microsoft.IdentityModel.Tokens;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Service.sevices
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IMapper _mapper;

        public UserService(IRepository<User> userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
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

            var existing = await _userRepository.Find(u => u.Email.ToLower() == dto.Email.ToLower() &&u.Id != id);
       
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

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("THIS_IS_MY_SUPER_SECRET_KEY_12345")
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "SmartFinance",
                audience: "SmartFinance",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<AuthResponseDto> Register(UserCreateDto dto)
        {
            var existing = await _userRepository.Find(u => u.Email == dto.Email);
            if (existing.Any())
                throw new Exception("Email already exists");

            var user = _mapper.Map<User>(dto);
            user.PasswordHash = HashPassword(dto.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.LastLoginAt = DateTime.UtcNow;

            await _userRepository.Add(user);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                Token = token
            };
        }
        public async Task<AuthResponseDto?> Login(UserLoginDto dto)
        {
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
    }
}