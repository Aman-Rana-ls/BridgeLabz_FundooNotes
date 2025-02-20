using System;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.Interfaces;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ModelLayer;
using RepoLayer.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services
{
    public class UserBL : IUserBL
    {
        private readonly IUserRL _userRL;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserBL> _logger;
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly string _smtpUser = "gsh401111@gmail.com";
        private readonly string _smtpPassword = "xxxx xxxx xxxx xxxx";
        private readonly int _smtpPort = 587;

        public UserBL(IUserRL authRepository, IConfiguration configuration, ILogger<UserBL> logger)
        {
            _userRL = authRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
            try
            {
                _logger.LogInformation($"Registering user with email: {model.Email}");
                return await _userRL.RegisterAsync(model);
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
                _logger.LogInformation($"User attempting login with email: {model.Email}");
                return await _userRL.LoginAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login for email {model.Email}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> SendVerificationEmailAsync(EmailModel model)
        {
            try
            {
                _logger.LogInformation($"Sending verification email to: {model.Email}");
                var userExists = await _userRL.IsUserExistsAsync(model.Email);
                if (!userExists)
                {
                    _logger.LogWarning($"User with email {model.Email} not found.");
                    throw new Exception("User not found");
                }

                var token = GenerateToken(model.Email);
                await SendVerificationEmailAsync(model.Email, token);
                _logger.LogInformation($"Verification email sent successfully to {model.Email}");
                return "Token has been sent to your email successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending verification email to {model.Email}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordModel model, string token)
        {
            try
            {
                _logger.LogInformation("Resetting password");
                var isTokenValid = ValidateToken(token);
                if (!isTokenValid)
                {
                    _logger.LogWarning("Invalid token provided for password reset.");
                    throw new Exception("Invalid token");
                }

                var claims = DecodeToken(token);
                var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

                if (emailClaim == null)
                {
                    _logger.LogWarning("Email not found in token for password reset.");
                    throw new Exception("Email not found in token");
                }

                var result = await _userRL.ResetPasswordAsync(emailClaim.Value, model.NewPassword);
                if (result)
                {
                    _logger.LogInformation($"Password reset successful for email {emailClaim.Value}");
                    return "Password reset successful";
                }

                _logger.LogError("Password reset failed.");
                throw new Exception("Password reset failed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resetting password: {ex.Message}");
                throw;
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                _logger.LogInformation($"Sending email to {to} with subject: {subject}");
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                using var smtpClient = new SmtpClient(_smtpServer)
                {
                    Port = _smtpPort,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {to}: {ex.Message}");
                throw;
            }
        }

        public async Task SendVerificationEmailAsync(string to, string token)
        {
            try
            {
                _logger.LogInformation($"Sending verification email to: {to}");
                var subject = "Verify Your Email Address";
                var body = $"Please use the following token to verify your email:\n{token}";
                await SendEmailAsync(to, subject, body);
                _logger.LogInformation($"Verification email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending verification email to {to}: {ex.Message}");
                throw;
            }
        }

        public string GenerateToken(string email)
        {
            try
            {
                _logger.LogInformation($"Generating token for email: {email}");
                var jwtSettings = _configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"])),
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogInformation($"Token generated successfully for email: {email}");
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating token for email {email}: {ex.Message}");
                throw;
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                _logger.LogInformation($"Validating token: {token.Substring(0, 10)}..."); // Log part of the token to keep it secure
                var jwtSettings = _configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                _logger.LogInformation($"Token validation result: {validatedToken != null}");
                return validatedToken != null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating token: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<Claim> DecodeToken(string token)
        {
            try
            {
                _logger.LogInformation($"Decoding token: {token.Substring(0, 10)}..."); // Log part of the token to keep it secure
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
                var claims = jwtToken?.Claims ?? Enumerable.Empty<Claim>();
                _logger.LogInformation($"Decoded token with {claims.Count()} claims");
                return claims;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error decoding token: {ex.Message}");
                throw;
            }
        }
    }
}
