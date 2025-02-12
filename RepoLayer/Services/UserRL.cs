using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RepoLayer.Interfaces;
using RepoLayer.Context;
using RepoLayer.Entity;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using ModelLayer;
using Microsoft.Extensions.Logging;

namespace RepoLayer.Services
{
    public class UserRL : IUserRL
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRL> _logger;

        public UserRL(ApplicationDbContext context, IConfiguration configuration, ILogger<UserRL> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
            try
            {
                _logger.LogInformation($"Attempting to register user with email: {model.Email}");

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    _logger.LogWarning($"Email {model.Email} already exists.");
                    throw new Exception("Email already exists");
                }

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password) // Hashing password
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User with email {model.Email} successfully registered.");
                return "Registration successful";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration for email {model.Email}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> LoginAsync(LoginModel model)
        {
            try
            {
                _logger.LogInformation($"Attempting to login user with email: {model.Email}");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Invalid credentials for email {model.Email}");
                    throw new Exception("Invalid credentials");
                }

                var token = GenerateJwtToken(user);
                _logger.LogInformation($"User with email {model.Email} successfully logged in.");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login for email {model.Email}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsUserExistsAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Checking if user exists with email: {email}");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                bool exists = user != null;
                _logger.LogInformation($"User exists with email {email}: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking if user exists for email {email}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            try
            {
                _logger.LogInformation($"Attempting to reset password for email: {email}");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning($"User with email {email} not found.");
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password for user with email {email} reset successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resetting password for email {email}: {ex.Message}");
                throw;
            }
        }

        private string GenerateJwtToken(User user)
        {
            try
            {
                _logger.LogInformation($"Generating JWT token for user with ID: {user.Id}");

                var jwtSettings = _configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"])),
                    signingCredentials: creds);

                var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation($"JWT token generated for user with ID: {user.Id}");
                return jwtToken;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating JWT token: {ex.Message}");
                throw;
            }
        }
    }
}
