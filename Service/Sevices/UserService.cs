using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;


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
    }
}