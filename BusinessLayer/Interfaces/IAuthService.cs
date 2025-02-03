using System.Threading.Tasks;
using ModelLayer.Models;

namespace BusinessLayer.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterModel model); // Return raw string
        Task<string> LoginAsync(LoginModel model); // Return raw string (JWT token)
    }
}