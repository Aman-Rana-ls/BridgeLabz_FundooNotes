using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLayer.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelLayer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RepoLayer.Entity;
using RepoLayer.Interfaces;

namespace BusinessLayer.Services
{
    public class UserBL : IUserBL, IDisposable
    {
        private readonly IUserRL _userRL;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserBL> _logger;
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly string _smtpUser = "gsh401111@gmail.com";
        private readonly string _smtpPassword = "awsl zxui xxzx gvcu";
        private readonly int _smtpPort = 587;
        private readonly IConnection _rabbitMqConnection;
        private readonly IModel _rabbitMqChannel;

        public UserBL(IUserRL userRL, IConfiguration configuration, ILogger<UserBL> logger)
        {
            _userRL = userRL;
            _configuration = configuration;
            _logger = logger;

            // Initialize RabbitMQ connection and channel
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _rabbitMqConnection = factory.CreateConnection();
            _rabbitMqChannel = _rabbitMqConnection.CreateModel();

            // Declare the queue
            _rabbitMqChannel.QueueDeclare(queue: "email_queue",
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

            // Start consuming messages
            StartConsuming();
        }

        private async Task<(string accessToken, string refreshToken)> GenerateRefreshToken(string email)
        {
            try
            {
                _logger.LogInformation($"Generating refresh token for email: {email}");

                // Generate a refresh token using a longer expiry time
                var jwtSettings = _configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var refreshToken = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["RefreshTokenExpiryInDays"])),
                    signingCredentials: creds
                );

                var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);

                User u = await _userRL.GetUserByEmailAsync(email);

                var accessToken = _userRL.GenerateJwtToken(u);


                _logger.LogInformation($"Refresh token generated successfully for email: {email}");

                // Store the refresh token in the database using _userRL
                await _userRL.StoreRefreshTokenAsync(email, refreshTokenString);

                return (accessToken, refreshTokenString);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating refresh token for email {email}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation($"Refreshing access token using refresh token: {refreshToken.Substring(0, 10)}...");

                var jwtSettings = _configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                var tokenHandler = new JwtSecurityTokenHandler();
                ClaimsPrincipal principal;

                try
                {
                    principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidateAudience = true,
                        ValidAudience = jwtSettings["Audience"],
                        ValidateLifetime = true,  // Check expiration
                        ClockSkew = TimeSpan.Zero
                    }, out var validatedToken);
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogWarning($"Refresh token has expired: {refreshToken}");
                    throw new Exception("Refresh token has expired.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Invalid refresh token: {ex.Message}");
                    throw new Exception("Invalid refresh token.");
                }

                if (principal == null)
                {
                    throw new Exception("Unable to validate the refresh token.");
                }

                var emailClaim = principal.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogWarning("Email not found in refresh token.");
                    throw new Exception("Email claim not found in refresh token.");
                }

                // Retrieve the stored refresh token
                var storedRefreshToken = await _userRL.GetRefreshTokenAsync(emailClaim.Value);
                if (storedRefreshToken != refreshToken)
                {
                    _logger.LogWarning("Refresh token mismatch.");
                    throw new Exception("Refresh token mismatch.");
                }

                // Generate and return a new access token
               
                User u = await _userRL.GetUserByEmailAsync(emailClaim.Value);
                var newAccessToken = _userRL.GenerateJwtToken(u);

                _logger.LogInformation($"New access token generated for email: {emailClaim.Value}");

                return newAccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing token: {ex.Message}");
                throw;
            }
        }



        public async Task<string> RegisterAsync(RegisterModel model)
        {
            try
            {
                _logger.LogInformation($"Registering user with email: {model.Email}");

                var result = await _userRL.RegisterAsync(model);

                if (result != null)
                {
                    var email = new SendEmailModel
                    {
                        To = model.Email,
                        Subject = "Registration Successful",
                        Body = $"Hello, {model.Email}. You have successfully registered your account."
                    };

                    SendEmailToQueue(email);

                    _logger.LogInformation($"Registration successful for email: {model.Email}, email sent to queue.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration for email {model.Email}: {ex.Message}");
                throw;
            }
        }

        public async Task<TokenResponseModel> LoginAsync(LoginModel model)
        {
            try
            {
                _logger.LogInformation($"User attempting login with email: {model.Email}");

                // Call the user repository to validate the login
                var loginResult = await _userRL.LoginAsync(model);

                if (loginResult == null)
                {
                    _logger.LogWarning($"Invalid credentials for email: {model.Email}");
                    return null; // If login fails, return null
                }

                // Send success email to the user
                var email = new SendEmailModel
                {
                    To = model.Email,
                    Subject = "Login Successful",
                    Body = $"Hello, {model.Email}. You have successfully logged in to your account."
                };
                SendEmailToQueue(email);

                _logger.LogInformation($"Login successful for {model.Email}, email sent to queue.");
                var tokens = await GenerateRefreshToken(model.Email); 

                
                return new TokenResponseModel
                {
                    AccessToken = loginResult,
                    RefreshToken = tokens.refreshToken
                };
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
                var email = new SendEmailModel
                {
                    To = model.Email,
                    Subject = "Verify Your Email Address",
                    Body = $"Please use the following token to verify your email:\n{token}"
                };

                SendEmailToQueue(email);

                _logger.LogInformation($"Verification email sent to queue for {model.Email}");
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

                    var email = new SendEmailModel
                    {
                        To = emailClaim.Value,
                        Subject = "Password Reset Successful",
                        Body = $"Your password has been successfully reset."
                    };

                    SendEmailToQueue(email);

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

        private void SendEmailToQueue(SendEmailModel email)
        {
            try
            {
                var emailJson = JsonSerializer.Serialize(email);
                var body = Encoding.UTF8.GetBytes(emailJson);

                _rabbitMqChannel.BasicPublish(exchange: "",
                                              routingKey: "email_queue",
                                              basicProperties: null,
                                              body: body);

                _logger.LogInformation($"Email sent to RabbitMQ queue: {email.To}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to queue: {ex.Message}");
                throw;
            }
        }

        private void StartConsuming()
        {
            var consumer = new EventingBasicConsumer(_rabbitMqChannel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var emailJson = Encoding.UTF8.GetString(body);
                    var email = JsonSerializer.Deserialize<SendEmailModel>(emailJson);

                    _logger.LogInformation($"Received email from RabbitMQ: {email.To}");

                    // Send the email
                    await SendEmailAsync(email.To, email.Subject, email.Body);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing email from RabbitMQ: {ex.Message}");
                }
            };

            _rabbitMqChannel.BasicConsume(queue: "email_queue",
                                          autoAck: true,
                                          consumer: consumer);
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(_smtpServer)
                {
                    Port = _smtpPort,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Email sent to {to} with subject: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {to}: {ex.Message}");
                throw;
            }
        }

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
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["AccessTokenExpiryInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]))
                }, out var validatedToken);

                return validatedToken != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public IEnumerable<Claim> DecodeToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            return jsonToken?.Claims ?? new List<Claim>();
        }

        public void Dispose()
        {
            _rabbitMqChannel?.Close();
            _rabbitMqConnection?.Close();
        }
    }
}
