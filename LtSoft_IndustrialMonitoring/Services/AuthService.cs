using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LtSoft_IndustrialMonitoring.Interfaces;
using LtSoft_IndustrialMonitoring.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using static LtSoft_IndustrialMonitoring.Models.LoginModels;

namespace LtSoft_IndustrialMonitoring.Services
{
    public class AuthService : IAuthService
    {
        private readonly List<UserConfig> _users;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IOptionsSnapshot<List<UserConfig>> users, IOptions<JwtSettings> jwtSettings)
        {
            _users = users.Value;
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        /// 验证用户登录
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<LoginResponse> AuthenticateAsync(LoginRequest request)
        {
            UserConfig? user = _users.FirstOrDefault(u => u.Username == request.Username);
            if (user == null)
                return null;

            // TODO: 实际应用中应该使用密码哈希比较
            if (!VerifyPassword(request.Password, user.PasswordHash))
                return null;

            string token = GenerateJwtToken(user);
            return new LoginResponse
            {
                Token = token,
                Role = user.Role
            };
        }

        /// <summary>
        /// 生成 JWT 令牌
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private string GenerateJwtToken(UserConfig user)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.Now.AddMinutes((double)_jwtSettings.ExpirationMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        /// <param name="password"></param>
        /// <param name="hashedPassword"></param>
        /// <returns></returns>
        private static bool VerifyPassword(string password, string hashedPassword)
        {
            // 这里应该使用真正的密码哈希验证，例如 BCrypt
            // 示例简化处理，实际应使用 proper hashing
            //return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            return password == hashedPassword;
        }

        /// <summary>
        /// 临时工具方法，用于生成哈希密码
        /// </summary>
        public static void GenerateHash()
        {
            string password = "password";
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            Console.WriteLine(hashedPassword); // 将输出结果放入配置文件
        }
    }
}
