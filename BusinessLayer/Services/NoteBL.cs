using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessLayer.Interfaces;
using ModelLayer;
using RepoLayer.Entity;
using RepoLayer.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services
{
    public class NoteBL : INoteBL
    {
        private readonly INoteRL _noteRL;
        private readonly ILogger<NoteBL> _logger;

        public NoteBL(INoteRL noteRL, ILogger<NoteBL> logger)
        {
            _noteRL = noteRL;
            _logger = logger;
        }

        public async Task<Note> CreateNoteAsync(NoteInputModel model, int userId)
        {
            try
            {
                _logger.LogInformation($"Creating note with title: {model.Title}");
                var note = new Note
                {
                    Title = model.Title,
                    Description = model.Description,
                    Color = model.Color,
                    CreatedBy = userId,
                    IsDeleted = false,
                    IsArchived = false
                };

                var createdNote = await _noteRL.CreateNoteAsync(note);
                _logger.LogInformation($"Note with title {model.Title} created successfully.");
                return createdNote;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error creating note: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Note>> GetNotesByUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Fetching notes for user with ID: {userId}");
                var notes = await _noteRL.GetNotesByUserAsync(userId);
                if (notes == null || !notes.Any())
                {
                    _logger.LogWarning($"No notes found for user with ID: {userId}");
                }
                return notes;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching notes for user with ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Note>> GetArchiveNotes(int userId)
        {
            try
            {
                _logger.LogInformation($"Fetching archived notes for user with ID: {userId}");
                var notes = await _noteRL.GetArchiveNotes(userId);
                if (notes == null || !notes.Any())
                {
                    _logger.LogWarning($"No archived notes found for user with ID: {userId}");
                }
                return notes;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching archived notes for user with ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Note> ArchiveNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Archiving note with ID: {noteId} for user with ID: {userId}");
                var archivedNote = await _noteRL.ArchiveNoteAsync(noteId, userId);
                if (archivedNote == null)
                {
                    _logger.LogWarning($"Failed to archive note with ID: {noteId} for user with ID: {userId}");
                }
                return archivedNote;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error archiving note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Updating note with ID: {noteId} for user with ID: {userId}");
                var updatedNote = await _noteRL.UpdateNoteAsync(note, noteId, userId);
                if (updatedNote == null)
                {
                    _logger.LogWarning($"Failed to update note with ID: {noteId} for user with ID: {userId}");
                }
                return updatedNote;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error updating note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Deleting note with ID: {noteId} for user with ID: {userId}");
                var result = await _noteRL.DeleteNoteAsync(noteId, userId);
                if (result)
                {
                    _logger.LogInformation($"Note with ID: {noteId} deleted successfully.");
                }
                else
                {
                    _logger.LogWarning($"Failed to delete note with ID: {noteId} for user with ID: {userId}");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error deleting note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Note> GetNoteByIdAsync(int noteId)
        {
            try
            {
                _logger.LogInformation($"Fetching note by ID: {noteId}");
                var note = await _noteRL.GetNoteByIdAsync(noteId);
                if (note == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found.");
                }
                return note;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching note by ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Note>> GetNotesFromBin(int userId)
        {
            try
            {
                _logger.LogInformation($"Fetching notes from bin for user with ID: {userId}");
                var notes = await _noteRL.GetNotesFromBin(userId);
                if (notes == null || !notes.Any())
                {
                    _logger.LogWarning($"No notes found in the bin for user with ID: {userId}");
                }
                return notes;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching notes from bin for user with ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UnArchiveNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Unarchiving note with ID: {noteId} for user with ID: {userId}");
                var result = await _noteRL.UnArchiveNoteAsync(noteId, userId);
                if (result)
                {
                    _logger.LogInformation($"Note with ID: {noteId} unarchived successfully.");
                }
                else
                {
                    _logger.LogWarning($"Failed to unarchive note with ID: {noteId} for user with ID: {userId}");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error unarchiving note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RestoreFromBin(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Restoring note with ID: {noteId} from bin for user with ID: {userId}");
                var result = await _noteRL.RestoreFromBin(noteId, userId);
                if (result)
                {
                    _logger.LogInformation($"Note with ID: {noteId} restored successfully from bin.");
                }
                else
                {
                    _logger.LogWarning($"Failed to restore note with ID: {noteId} from bin for user with ID: {userId}");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error restoring note with ID {noteId} from bin: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeletePermNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Permanently deleting note with ID: {noteId} for user with ID: {userId}");
                var result = await _noteRL.DeletePermNoteAsync(noteId, userId);
                if (result)
                {
                    _logger.LogInformation($"Note with ID: {noteId} permanently deleted.");
                }
                else
                {
                    _logger.LogWarning($"Failed to permanently delete note with ID: {noteId} for user with ID: {userId}");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error permanently deleting note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public int GetUserIdFromToken(ClaimsPrincipal user)
        {
            try
            {
                _logger.LogInformation("Extracting user ID from token.");
                var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

                if (userIdClaim?.Value == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogError("User ID not found or invalid in token.");
                    throw new UnauthorizedAccessException("User ID not found or invalid in token.");
                }

                _logger.LogInformation($"User ID extracted: {userId}");
                return userId;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error extracting user ID from token: {ex.Message}");
                throw;
            }
        }
    }
}
