using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Interfaces;
using RepoLayer.Entity;
using System.Threading.Tasks;
using ModelLayer;
using Microsoft.Extensions.Logging;
using System;

namespace FundooNotes.Controllers
{
    [Route("/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserBL _userBL;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserBL userBL, ILogger<UserController> logger)
        {
            _userBL = userBL;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new ResponseModel<string> { Success = false, Message = "Invalid request" });
                }

                _logger.LogInformation("Attempting to register user with email: {Email}", model.Email);
                var result = await _userBL.RegisterAsync(model);
                _logger.LogInformation("User with email: {Email} registered successfully", model.Email);
                return Ok(new ResponseModel<string> { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering user with email: {Email}", model.Email);
                return BadRequest(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new ResponseModel<string> { Success = false, Message = "Invalid request" });
                }

                _logger.LogInformation("Attempting to log in user with email: {Email}", model.Email);

                // Call the business layer to login and get both tokens
                var tokens = await _userBL.LoginAsync(model);

                if (tokens == null)
                {
                    return Unauthorized(new ResponseModel<string> { Success = false, Message = "Invalid credentials" });
                }

                _logger.LogInformation("User with email: {Email} logged in successfully", model.Email);

                // Construct the response with both access and refresh tokens
                var tokenResponse = new TokenResponseModel
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                };

                return Ok(new ResponseModel<TokenResponseModel>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = tokenResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging in user with email: {Email}", model.Email);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }


        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.RefreshToken))
                {
                    return BadRequest(new ResponseModel<string> { Success = false, Message = "Invalid request" });
                }

                _logger.LogInformation("Attempting to refresh access token using refresh token: {RefreshToken}", model.RefreshToken);

                var newAccessToken = await _userBL.RefreshTokenAsync(model.RefreshToken);

                if (string.IsNullOrEmpty(newAccessToken))
                {
                    return Unauthorized(new ResponseModel<string> { Success = false, Message = "Invalid or expired refresh token" });
                }

                _logger.LogInformation("Access token refreshed successfully");

                return Ok(new ResponseModel<string>
                {
                    Success = true,
                    Message = "Access token refreshed successfully",
                    Data = newAccessToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while refreshing token");
                return BadRequest(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }


        [HttpPost("forget-password")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] EmailModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new ResponseModel<string> { Success = false, Message = "Invalid request" });
                }

                _logger.LogInformation("Sending verification email to: {Email}", model.Email);
                var result = await _userBL.SendVerificationEmailAsync(model);
                _logger.LogInformation("Verification email sent successfully to: {Email}", model.Email);
                return Ok(new ResponseModel<string> { Success = true, Message = "Verification email sent", Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending verification email to: {Email}", model.Email);
                return BadRequest(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("set-new-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            try
            {
                if (model == null)
                {
                    return BadRequest(new ResponseModel<string> { Success = false, Message = "Invalid request" });
                }

                _logger.LogInformation("Attempting to reset password for user with token: {Token}", token);

                var result = await _userBL.ResetPasswordAsync(model, token);
                _logger.LogInformation("Password reset successfully for user with token: {Token}", token);

                return Ok(new ResponseModel<string> { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting password for user with token: {Token}", token);
                return BadRequest(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }
    }
}