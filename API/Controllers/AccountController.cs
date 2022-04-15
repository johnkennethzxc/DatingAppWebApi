using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {

        //public readonly DataContext _context;
        public readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        // public AccountController(DataContext context, ITokenService tokenService, 
        //     IMapper mapper)
        // {
        //     _mapper = mapper;
        //     _context = context;
        //     _tokenService = tokenService;
        // }

        // This is using Identity
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, 
            IMapper mapper)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _mapper = mapper;
            _tokenService = tokenService;
        }

        // [HttpPost("register")]
        // public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        // {
        //     if (await UserExists(registerDTO.Username)) return BadRequest("Username is already taken");

        //     var user = _mapper.Map<AppUser>(registerDTO);

        //     using var hmac = new HMACSHA512();

        //     // var user = new AppUser
        //     // {
        //     //     UserName = registerDTO.Username.ToLower(),
        //     //     PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
        //     //     PasswordSalt = hmac.Key
        //     // };

        //     user.UserName = registerDTO.Username.ToLower();
        //     user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password));
        //     user.PasswordSalt = hmac.Key;

        //     _context.Users.Add(user);
        //     await _context.SaveChangesAsync();

        //     return new UserDTO
        //     {
        //         Username = user.UserName,
        //         Token = _tokenService.CreateToken(user),
        //         KnownAs = user.KnownAs,
        //         Gender = user.Gender
                
        //     };
        // }

        // This is using identity
        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        {
            if (await UserExists(registerDTO.Username)) return BadRequest("Username is already taken");

            var user = _mapper.Map<AppUser>(registerDTO);

            // var user = new AppUser
            // {
            //     UserName = registerDTO.Username.ToLower(),
            //     PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
            //     PasswordSalt = hmac.Key
            // };

            user.UserName = registerDTO.Username.ToLower();

            var results = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!results.Succeeded) return BadRequest(results.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, "Member");

            if (!roleResult.Succeeded) return BadRequest(results.Errors);

            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
                
            };
        }

        // [HttpPost("login")]
        // public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        // {
        //     var user = await _context.Users
        //         .Include(p => p.Photos)
        //         .SingleOrDefaultAsync(x => x.UserName == loginDTO.Username);

        //     if (user == null) return Unauthorized("Invalid username");

        //     using var hmac = new HMACSHA512(user.PasswordSalt);

        //     var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

        //     for (int i = 0; i < computedHash.Length; i++)
        //     {
        //         if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
        //     }

        //     return new UserDTO
        //     {
        //         Username = user.UserName,
        //         Token = _tokenService.CreateToken(user),
        //         PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
        //         KnownAs = user.KnownAs,
        //         Gender = user.Gender
        //     };
        // }

        // This is using identity
        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            var user = await _userManager.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == loginDTO.Username.ToLower());

            if (user == null) return Unauthorized("Invalid username");

            var result = await _signInManager
                .CheckPasswordSignInAsync(user, loginDTO.Password, false);

            if (!result.Succeeded) return Unauthorized();

            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}