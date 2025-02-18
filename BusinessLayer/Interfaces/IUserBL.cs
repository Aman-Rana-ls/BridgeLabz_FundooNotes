using ModelLayer;
using System.Security.Claims;

public interface IUserBL
{
    Task<string> RegisterAsync(RegisterModel model);
    Task<TokenResponseModel> LoginAsync(LoginModel model);
    Task<string> ResetPasswordAsync(ResetPasswordModel model, string token);
    Task<string> SendVerificationEmailAsync(EmailModel model);

    string GenerateToken(string email);
    bool ValidateToken(string token);
    IEnumerable<Claim> DecodeToken(string token);
    Task<string> RefreshTokenAsync(string refreshToken);
}
