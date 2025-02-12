using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;  // Add this namespace for logging
using System.Threading.Tasks;

namespace FundooNotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollaboratorController : ControllerBase
    {
        private readonly ICollaboratorBL _collaboratorBL;
        private readonly ILogger<CollaboratorController> _logger; // Injecting ILogger

        public CollaboratorController(ICollaboratorBL collaboratorBL, ILogger<CollaboratorController> logger)
        {
            _collaboratorBL = collaboratorBL;
            _logger = logger;  // Assign logger to the field
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddCollaborator(int noteId, string collaboratorEmail)
        {
            try
            {
                _logger.LogInformation("Attempting to add collaborator with email: {Email} to note with ID: {NoteId}", collaboratorEmail, noteId);

                var result = await _collaboratorBL.AddCollaborator(noteId, collaboratorEmail);

                if (result)
                {
                    _logger.LogInformation("Collaborator with email: {Email} added successfully to note with ID: {NoteId}", collaboratorEmail, noteId);
                    return Ok("Collaborator added successfully.");
                }

                _logger.LogWarning("Failed to add collaborator with email: {Email} to note with ID: {NoteId}", collaboratorEmail, noteId);
                return BadRequest("Failed to add collaborator.");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding collaborator with email: {Email} to note with ID: {NoteId}", collaboratorEmail, noteId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveCollaborator(int noteId, string collaboratorEmail)
        {
            try
            {
                _logger.LogInformation("Attempting to remove collaborator with email: {Email} from note with ID: {NoteId}", collaboratorEmail, noteId);

                var result = await _collaboratorBL.RemoveCollaborator(noteId, collaboratorEmail);

                if (result)
                {
                    _logger.LogInformation("Collaborator with email: {Email} removed successfully from note with ID: {NoteId}", collaboratorEmail, noteId);
                    return Ok("Collaborator removed successfully.");
                }

                _logger.LogWarning("Failed to remove collaborator with email: {Email} from note with ID: {NoteId}", collaboratorEmail, noteId);
                return BadRequest("Failed to remove collaborator.");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred while removing collaborator with email: {Email} from note with ID: {NoteId}", collaboratorEmail, noteId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("get/{noteId}")]
        public async Task<IActionResult> GetCollaboratorsByNoteId(int noteId)
        {
            try
            {
                _logger.LogInformation("Fetching collaborators for note with ID: {NoteId}", noteId);

                var collaborators = await _collaboratorBL.GetCollaboratorsByNoteId(noteId);

                if (collaborators == null || collaborators.Count == 0)
                {
                    _logger.LogInformation("No collaborators found for note with ID: {NoteId}", noteId);
                }

                return Ok(collaborators);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching collaborators for note with ID: {NoteId}", noteId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
