using static LtSoft_IndustrialMonitoring.Models.LoginModels;

namespace LtSoft_IndustrialMonitoring.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> AuthenticateAsync(LoginRequest request);
    }
}
