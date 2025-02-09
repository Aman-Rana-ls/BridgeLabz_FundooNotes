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

namespace BusinessLayer.Services
{
    public class UserBL : IUserBL
    {
        private readonly IUserRL _userRL;
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly string _smtpUser = "gsh401111@gmail.com";
        private readonly string _smtpPassword = "awsl zxui xxzx gvcu";
        private readonly int _smtpPort = 587;

        public UserBL(IUserRL authRepository, IConfiguration configuration)
        {
            _userRL = authRepository;
            _configuration = configuration;
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
            return await _userRL.RegisterAsync(model);
        }

        public async Task<string> LoginAsync(LoginModel model)
        {
            return await _userRL.LoginAsync(model);
        }

        public async Task<string> SendVerificationEmailAsync(EmailModel model)
        {
            var userExists = await _userRL.IsUserExistsAsync(model.Email);
            if (!userExists) throw new Exception("User not found");

            var token = GenerateToken(model.Email);
            await SendVerificationEmailAsync(model.Email, token);
            return "Token has been sent to your email successfully.";
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordModel model, string token)
        {
            var isTokenValid = ValidateToken(token);
            if (!isTokenValid) throw new Exception("Invalid token");

            var claims = DecodeToken(token);
            var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            if (emailClaim == null) throw new Exception("Email not found in token");

            var result = await _userRL.ResetPasswordAsync(emailClaim.Value, model.NewPassword);
            if (result) return "Password reset successful";

            throw new Exception("Password reset failed");
        }

        // Email-related methods
        public async Task SendEmailAsync(string to, string subject, string body)
        {
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
        }

        public async Task SendVerificationEmailAsync(string to, string token)
        {
            var subject = "Verify Your Email Address";
            var body = $"Please use the following token to verify your email:\n{token}";
            await SendEmailAsync(to, subject, body);
        }

        // Token-related methods
        public string GenerateToken(string email)
        {
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

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidateToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
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

                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<Claim> DecodeToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            return jwtToken?.Claims ?? Enumerable.Empty<Claim>();
        }
    }
}
