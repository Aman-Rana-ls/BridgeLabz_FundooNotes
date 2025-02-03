using System.Threading.Tasks;
using ModelLayer.Models;

namespace RepoLayer.Interfaces
{
    public interface IAuthRepository
    {
        Task<string> RegisterAsync(RegisterModel model); // Return raw string
        Task<string> LoginAsync(LoginModel model); // Return raw string (JWT token)
    }
}