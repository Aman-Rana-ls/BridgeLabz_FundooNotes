using BusinessLayer.Interfaces;
using ModelLayer.Models;
using RepoLayer.Interfaces;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;

        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
            return await _authRepository.RegisterAsync(model); // Return raw string
        }

        public async Task<string> LoginAsync(LoginModel model)
        {
            return await _authRepository.LoginAsync(model); // Return raw JWT token
        }
    }
}