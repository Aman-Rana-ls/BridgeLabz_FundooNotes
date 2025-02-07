using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Interfaces;
using RepoLayer.EntityOne;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using RepoLayer;

namespace FundooNotes.Controllers
{
    [Route("/notes")]
    [ApiController]
    [Authorize]
    public class NoteController : ControllerBase
    {
        private readonly INoteBL _noteBL;
        //private readonly ILabelService _labelService;

        public NoteController(INoteBL noteBL)
        {
            _noteBL = noteBL;
            //_labelService = labelService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NoteInputModel model)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                var createdNote = await _noteBL.CreateNoteAsync(model, userId);
                return Ok(new ResponseModel<Note> { Success = true, Message = "Note created successfully", Data = createdNote });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                var notes = await _noteBL.GetNotesByUserAsync(userId);
                return Ok(new ResponseModel<List<Note>> { Success = true, Message = "Notes fetched successfully", Data = notes });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }
        [HttpGet("{noteId}")]
        public async Task<IActionResult> GetNoteById(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                var note = await _noteBL.GetNoteByIdAsync(noteId);

                if (note == null)
                {
                    return NotFound(new ResponseModel<string> { Success = false, Message = "Note not found" });
                }

                if (note.CreatedBy != userId)
                {
                    return Forbid();
                }

                return Ok(new ResponseModel<Note> { Success = true, Message = "Note fetched successfully", Data = note });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }


        [HttpPut("{noteId}")]
        public async Task<IActionResult> Update([FromBody] UpdateNote note, int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                var existingNote = await _noteBL.GetNoteByIdAsync(noteId);

                if (existingNote == null)
                {
                    return NotFound(new ResponseModel<string> { Success = false, Message = "Note not found" });
                }

                if (existingNote.CreatedBy != userId)
                {
                    return Forbid();
                }

                var updatedNote = await _noteBL.UpdateNoteAsync(note, noteId, userId);
                return Ok(new ResponseModel<Note> { Success = true, Message = "Note updated successfully", Data = updatedNote });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }
        [HttpDelete("{noteId}")]
        public async Task<IActionResult> Delete(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                var result = await _noteBL.DeleteNoteAsync(noteId, userId);
                return result
                    ? Ok(new ResponseModel<string> { Success = true, Message = "Note deleted successfully" })
                    : NotFound(new ResponseModel<string> { Success = false, Message = "Note not found or unauthorized" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }
    }
}
