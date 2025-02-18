using System.Threading.Tasks;
using ModelLayer;
using RepoLayer.Entity;

namespace RepoLayer.Interfaces
{
    public interface IUserRL
    {
        // User Registration
        Task<string> RegisterAsync(RegisterModel model);

        // User Login
        Task<string> LoginAsync(LoginModel model);

        // Check if user exists by email
        Task<bool> IsUserExistsAsync(string email);

        // Reset Password
        Task<bool> ResetPasswordAsync(string email, string newPassword);

        // Get user by email
        Task<User> GetUserByEmailAsync(string email);

        string GenerateJwtToken(User user);


        Task StoreRefreshTokenAsync(string email, string refreshToken);


        Task<string> GetEmailFromRefreshTokenAsync(string refreshToken);

        Task<string> GetRefreshTokenAsync(string email);
    }
}
