using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using ModelLayer;

namespace BusinessLayer.Interfaces
{
    public interface IUserBL
    {
        Task<string> RegisterAsync(RegisterModel model);
        Task<string> LoginAsync(LoginModel model);
        Task<string> ResetPasswordAsync(ResetPasswordModel model, string token);

        Task SendEmailAsync(string to, string subject, string body);
        Task<string> SendVerificationEmailAsync(EmailModel model);

        string GenerateToken(string email);
        bool ValidateToken(string token);
        IEnumerable<Claim> DecodeToken(string token);
    }
}
