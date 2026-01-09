namespace LtSoft_IndustrialMonitoring.Models
{
    public class LoginModels
    {
        public class LoginRequest
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        public class LoginResponse
        {
            public string? Token { get; set; }
            public string? Role { get; set; }
        }
    }
}
