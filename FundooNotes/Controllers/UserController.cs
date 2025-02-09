using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Interfaces;
using RepoLayer.Entity;
using System.Threading.Tasks;
using ModelLayer;

namespace FundooNotes.Controllers
{

    [Route("/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserBL _userBL;

        public UserController(IUserBL authBL)
        {
            _userBL = authBL;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var result = await _userBL.RegisterAsync(model);
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
                var token = await _userBL.LoginAsync(model);
                return Ok(new ResponseModel<string> { Success = true, Message = "Login successful", Data = token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }

        
        [HttpPost("forget-password")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] EmailModel model)
        {
            try
            {

                var result = await _userBL.SendVerificationEmailAsync(model);
                return Ok(new ResponseModel<string> { Success = true, Message = "Verification email sent", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("set-new-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            { 
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                var result = await _userBL.ResetPasswordAsync(model, token);

                return Ok(new ResponseModel<string> { Success = true, Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
        }

    }
}
