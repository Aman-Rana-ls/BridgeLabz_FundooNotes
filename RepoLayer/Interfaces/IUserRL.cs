using System.Threading.Tasks;
using ModelLayer;

namespace RepoLayer.Interfaces
{
    public interface IUserRL
    {
        Task<string> RegisterAsync(RegisterModel model);
        Task<string> LoginAsync(LoginModel model);
        Task<bool> IsUserExistsAsync(string email); 
        Task<bool> ResetPasswordAsync(string email, string newPassword);
    }
}
