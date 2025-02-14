using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Interfaces;
using RepoLayer.Entity;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using ModelLayer;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace FundooNotes.Controllers
{
    [Route("/notes")]
    [ApiController]
    [Authorize]
    public class NoteController : ControllerBase
    {
        private readonly INoteBL _noteBL;
        private readonly ILogger<NoteController> _logger;
        private readonly IDatabase _cache;

        public NoteController(INoteBL noteBL, ILogger<NoteController> logger, IConnectionMultiplexer connectionMultiplexer)
        {
            _noteBL = noteBL;
            _logger = logger;
            _cache = connectionMultiplexer.GetDatabase(); // Initialize Redis cache
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NoteInputModel model)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is creating a new note", userId);

                var createdNote = await _noteBL.CreateNoteAsync(model, userId);
                _logger.LogInformation("Note created successfully for User {UserId}", userId);

                return Ok(new ResponseModel<Note> { Success = true, Message = "Note created successfully", Data = createdNote });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = "Please Enter a valid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating a note: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is fetching their notes", userId);

                // Check Redis cache for the user's notes
                var cacheKey = $"user:{userId}:notes";
                var cachedNotes = await _cache.ListRangeAsync(cacheKey);

                if (cachedNotes.Any())
                {
                    _logger.LogInformation("Retrieved notes from Redis cache.");
                    var notes = cachedNotes
                        .Select(noteJson => JsonSerializer.Deserialize<Note>(noteJson))
                        .ToList();

                    return Ok(new ResponseModel<List<Note>>
                    {
                        Success = true,
                        Message = "Notes fetched from cache successfully",
                        Data = notes
                    });
                }

                // If not found in cache, fetch notes from the business layer
                var notesFromDb = await _noteBL.GetNotesByUserAsync(userId);
                _logger.LogInformation("Fetched {NoteCount} notes for User {UserId} from database", notesFromDb.Count, userId);

                return Ok(new ResponseModel<List<Note>>
                {
                    Success = true,
                    Message = "Notes fetched successfully",
                    Data = notesFromDb
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while fetching notes: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string>
                {
                    Success = false,
                    Message = "An error occurred: " + ex.Message
                });
            }
        }

        [HttpGet("{noteId}")]
        public async Task<IActionResult> GetNoteById(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is fetching note with ID {NoteId}", userId, noteId);

                var cacheKey = $"user:{userId}:notes";
                _logger.LogInformation("Checking Redis cache for key: {CacheKey}", cacheKey);

                var cachedNotes = await _cache.ListRangeAsync(cacheKey);

                if (cachedNotes.Any())
                {
                    var noteJson = cachedNotes.FirstOrDefault(n =>
                    {
                        var note = JsonSerializer.Deserialize<Note>(n);
                        return note.NoteId == noteId;
                    });

                    if (!noteJson.IsNullOrEmpty)
                    {
                        var note = JsonSerializer.Deserialize<Note>(noteJson);
                        _logger.LogInformation("Note with ID {NoteId} retrieved from Redis cache.", noteId);
                        return Ok(new ResponseModel<Note>
                        {
                            Success = true,
                            Message = "Note fetched from cache successfully",
                            Data = note
                        });
                    }
                }

                // If not in cache, fetch from database
                _logger.LogInformation("Note with ID {NoteId} not found in cache. Fetching from DB.", noteId);
                var noteFromDb = await _noteBL.GetNoteByIdAsync(noteId);

                if (noteFromDb == null)
                {
                    _logger.LogWarning("Note with ID {NoteId} does not exist", noteId);
                    return Ok(new ResponseModel<string>
                    {
                        Success = false,
                        Message = "Note doesn't exist"
                    });
                }

                if (noteFromDb.CreatedBy != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to access a note created by another user", userId);
                    return Forbid();
                }

                return Ok(new ResponseModel<Note>
                {
                    Success = true,
                    Message = "Note fetched successfully",
                    Data = noteFromDb
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while fetching the note: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string>
                {
                    Success = false,
                    Message = "An error occurred: " + ex.Message
                });
            }
        }


        [HttpPut("{noteId}")]
        public async Task<IActionResult> Update([FromBody] UpdateNote note, int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is updating note with ID {NoteId}", userId, noteId);

                var existingNote = await _noteBL.GetNoteByIdAsync(noteId);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note with ID {NoteId} not found", noteId);
                    return NotFound(new ResponseModel<string> { Success = false, Message = "Note not found" });
                }

                if (existingNote.CreatedBy != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update a note created by another user", userId);
                    return Forbid();
                }

                var updatedNote = await _noteBL.UpdateNoteAsync(note, noteId, userId);
                _logger.LogInformation("Note with ID {NoteId} updated successfully for User {UserId}", noteId, userId);

                return Ok(new ResponseModel<Note> { Success = true, Message = "Note updated successfully", Data = updatedNote });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while updating the note: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPut("{noteId}/archive")]
        public async Task<IActionResult> Archive(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is archiving note with ID {NoteId}", userId, noteId);

                var existingNote = await _noteBL.GetNoteByIdAsync(noteId);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note with ID {NoteId} not found", noteId);
                    return NotFound(new ResponseModel<string> { Success = false, Message = "Note not found" });
                }

                if (existingNote.CreatedBy != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to archive a note created by another user", userId);
                    return Forbid();
                }

                await _noteBL.ArchiveNoteAsync(noteId, userId);
                _logger.LogInformation("Note with ID {NoteId} archived successfully for User {UserId}", noteId, userId);

                return Ok(new ResponseModel<string> { Success = true, Message = "Note archived successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while archiving the note: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchivedNotes()
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is fetching archived notes", userId);

                // Check Redis cache for the user's archived notes
                var cacheKey = $"user:{userId}:archivedNotes";
                var cachedArchivedNotes = await _cache.ListRangeAsync(cacheKey);

                if (cachedArchivedNotes.Any())
                {
                    _logger.LogInformation("Retrieved archived notes from Redis cache.");
                    var notes = cachedArchivedNotes
                        .Select(noteJson => JsonSerializer.Deserialize<Note>(noteJson))
                        .ToList();

                    return Ok(new ResponseModel<List<Note>>
                    {
                        Success = true,
                        Message = "Archived notes fetched from cache successfully",
                        Data = notes
                    });
                }

                // If not found in cache, fetch archived notes from the business layer
                var notesFromDb = await _noteBL.GetArchiveNotes(userId);
                _logger.LogInformation("Fetched {NoteCount} archived notes for User {UserId}", notesFromDb.Count, userId);

                // Cache the notes as a list in Redis
                foreach (var note in notesFromDb)
                {
                    await _cache.ListRightPushAsync(cacheKey, JsonSerializer.Serialize(note));
                }

                // Set an expiration time for the cache (optional)
                await _cache.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(10));

                return Ok(new ResponseModel<List<Note>>
                {
                    Success = true,
                    Message = "Archived notes fetched successfully",
                    Data = notesFromDb
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while fetching archived notes: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string>
                {
                    Success = false,
                    Message = "An error occurred: " + ex.Message
                });
            }
        }

        [HttpPut("{noteId}/unarchive")]
        public async Task<IActionResult> Unarchive(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is unarchiving note with ID {NoteId}", userId, noteId);

                var existingNote = await _noteBL.GetNoteByIdAsync(noteId);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note with ID {NoteId} not found", noteId);
                    return NotFound(new ResponseModel<string> { Success = false, Message = "Note not found" });
                }

                if (existingNote.CreatedBy != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to unarchive a note created by another user", userId);
                    return Forbid();
                }

                await _noteBL.UnArchiveNoteAsync(noteId, userId);
                _logger.LogInformation("Note with ID {NoteId} unarchived successfully for User {UserId}", noteId, userId);

                return Ok(new ResponseModel<string> { Success = true, Message = "Note unarchived successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while unarchiving the note: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpDelete("{noteId}")]
        public async Task<IActionResult> Delete(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is deleting note with ID {NoteId}", userId, noteId);

                var existingNote = await _noteBL.GetNoteByIdAsync(noteId);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note with ID {NoteId} not found", noteId);
                    return NotFound(new ResponseModel<string> { Success = false, Message = "Note not found" });
                }

                if (existingNote.CreatedBy != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete a note created by another user", userId);
                    return Forbid();
                }

                await _noteBL.DeleteNoteAsync(noteId, userId);
                _logger.LogInformation("Note with ID {NoteId} deleted successfully for User {UserId}", noteId, userId);

                return Ok(new ResponseModel<string> { Success = true, Message = "Note deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while deleting the note: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("bin")]
        public async Task<IActionResult> GetNotesFromBin()
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is fetching notes from bin", userId);

                // Check Redis cache for the user's bin notes
                var cacheKey = $"user:{userId}:binNotes";
                var cachedNotes = await _cache.ListRangeAsync(cacheKey);

                if (cachedNotes.Any())
                {
                    _logger.LogInformation("Retrieved notes from Redis cache.");
                    var notes = cachedNotes
                        .Select(noteJson => JsonSerializer.Deserialize<Note>(noteJson))
                        .ToList();

                    return Ok(new ResponseModel<List<Note>>
                    {
                        Success = true,
                        Message = "Notes fetched from bin successfully",
                        Data = notes
                    });
                }

                // If not found in cache, fetch from the database
                var notesFromBin = await _noteBL.GetNotesFromBin(userId);
                _logger.LogInformation("Fetched {NoteCount} notes from bin for User {UserId}", notesFromBin.Count, userId);

              

                // Set an expiration time for the cache (optional)
               // await _cache.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(10));

                return Ok(new ResponseModel<List<Note>>
                {
                    Success = true,
                    Message = "Notes fetched successfully",
                    Data = notesFromBin
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while fetching notes from bin: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string>
                {
                    Success = false,
                    Message = "An error occurred: " + ex.Message
                });
            }
        }
        [HttpPut("{noteId}/restore-from-bin")]
        public async Task<IActionResult> Restore(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is restoring note with ID {NoteId} from bin", userId, noteId);

                await _noteBL.RestoreFromBin(noteId, userId);
                _logger.LogInformation("Note with ID {NoteId} restored successfully for User {UserId}", noteId, userId);

                return Ok(new ResponseModel<string> { Success = true, Message = "Note restored successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while restoring the note from bin: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }

        [HttpDelete("{noteId}/delete-permanently")]
        public async Task<IActionResult> DeletePermanently(int noteId)
        {
            try
            {
                var userId = _noteBL.GetUserIdFromToken(User);
                _logger.LogInformation("User {UserId} is deleting note with ID {NoteId} permanently", userId, noteId);

                var result = await _noteBL.DeletePermNoteAsync(noteId, userId);
                if (result)
                {
                    _logger.LogInformation("Note with ID {NoteId} permanently deleted for User {UserId}", noteId, userId);

                    return Ok(new ResponseModel<string> { Success = true, Message = "Note deleted successfully" });
                }
                else
                {
                    _logger.LogWarning("Note with ID {NoteId} not found or unauthorized for User {UserId}", noteId, userId);
                    return NotFound(new ResponseModel<string> { Success = false, Message = "Note not found or unauthorized" });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
                return Unauthorized(new ResponseModel<string> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while permanently deleting the note: {Message}", ex.Message);
                return StatusCode(500, new ResponseModel<string> { Success = false, Message = "An error occurred: " + ex.Message });
            }
        }



    }
}
