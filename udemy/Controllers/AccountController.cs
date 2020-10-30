using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using udemy.Data;
using udemy.DTOs;
using udemy.Entities;
using udemy.Services;

namespace udemy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly DataContext context;

        private readonly ITokenService tokenService;

        public AccountController(DataContext context, ITokenService tokenservice)
        {
            this.context = context;
            this.tokenService = tokenservice;
        }

        [HttpPost("register")]

        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            using var hmac = new HMACSHA512();
            {
                var user = new AppUser
                {
                    UserName = registerDto.Username,
                    PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                    PasswordSalt = hmac.Key
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
                return new UserDto
                {
                    Username = user.UserName,
                    Token = tokenService.CreateToken(user)
                };
            }
        }


        [HttpPost("Login")]

        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await context.Users
                .SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

            if (user == null) return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                    return Unauthorized("Invalid username");
            }

            return new UserDto
            {
                Username = user.UserName,
                Token = tokenService.CreateToken(user)
            };
        }
    }

}
