using FinanceManagement.Api.Data;
using FinanceManagement.Api.Models.DTOs;
using FinanceManagement.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace FinanceManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null || !VerifyPasswordHash(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "用户名或密码不正确" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "账户已禁用，请联系管理员" });
            }

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    realName = user.RealName,
                    role = user.Role
                }
            });
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return BadRequest(new { message = "用户名已存在" });
            }

            var user = new User
            {
                Username = registerDto.Username,
                PasswordHash = HashPassword(registerDto.Password),
                RealName = registerDto.RealName,
                Role = registerDto.Role
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "用户创建成功",
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    realName = user.RealName,
                    role = user.Role
                }
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // 由于JWT是无状态的，服务端无法直接使客户端的JWT失效
            // 客户端负责删除存储的token
            // 这里只返回登出成功的消息
            return Ok(new { message = "登出成功" });
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                realName = user.RealName,
                role = user.Role
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Name, user.RealName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expirationHours = int.Parse(jwtSettings["ExpirationHours"]);
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            var hashWithSalt = new byte[hash.Length + salt.Length];
            Array.Copy(hash, 0, hashWithSalt, 0, hash.Length);
            Array.Copy(salt, 0, hashWithSalt, hash.Length, salt.Length);

            return Convert.ToBase64String(hashWithSalt);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var hashWithSalt = Convert.FromBase64String(storedHash);
            
            // 从存储的哈希中提取盐值
            var hashLength = 64; // HMACSHA512 哈希长度
            var salt = new byte[hashWithSalt.Length - hashLength];
            Array.Copy(hashWithSalt, hashLength, salt, 0, salt.Length);

            // 使用相同的盐值重新计算密码哈希
            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // 比较计算出的哈希与存储的哈希
            for (int i = 0; i < hashLength; i++)
            {
                if (computedHash[i] != hashWithSalt[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
} 