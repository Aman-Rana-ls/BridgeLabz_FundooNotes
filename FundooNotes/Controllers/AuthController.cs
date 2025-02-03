using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Interfaces;
using ModelLayer.Models;
using System.Threading.Tasks;

namespace FundooNotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var result = await _authService.RegisterAsync(model);
                return Ok(new ResponseModel<string> { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var token = await _authService.LoginAsync(model);
                return Ok(new ResponseModel<string> { Success = true, Message = "Login successful", Data = token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }
    }
}