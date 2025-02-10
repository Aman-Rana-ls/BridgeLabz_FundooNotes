using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FundooNotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollaboratorController : ControllerBase
    {
        private readonly ICollaboratorBL _collaboratorBL;

        public CollaboratorController(ICollaboratorBL collaboratorBL)
        {
            _collaboratorBL = collaboratorBL;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddCollaborator(int noteId, string collaboratorEmail)
        {
            var result = await _collaboratorBL.AddCollaborator(noteId, collaboratorEmail);
            if (result)
                return Ok("Collaborator added successfully.");
            return BadRequest("Failed to add collaborator.");
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveCollaborator(int noteId, string collaboratorEmail)
        {
            var result = await _collaboratorBL.RemoveCollaborator(noteId, collaboratorEmail);
            if (result)
                return Ok("Collaborator removed successfully.");
            return BadRequest("Failed to remove collaborator.");
        }

        [HttpGet("get/{noteId}")]
        public async Task<IActionResult> GetCollaboratorsByNoteId(int noteId)
        {
            var collaborators = await _collaboratorBL.GetCollaboratorsByNoteId(noteId);
            return Ok(collaborators);
        }
    }
}