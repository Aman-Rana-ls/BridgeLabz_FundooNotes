using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepoLayer.Context;
using RepoLayer.Entity;
using RepoLayer.Interfaces;
using ModelLayer;
using Microsoft.Extensions.Logging;

namespace RepoLayer.Services
{
    public class NoteRL : INoteRL
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NoteRL> _logger;

        public NoteRL(ApplicationDbContext context, ILogger<NoteRL> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            try
            {
                _logger.LogInformation($"Creating a new note with title: {note.Title}");
                _context.Notes.Add(note);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note with title '{note.Title}' created successfully.");
                return note;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error creating note with title '{note.Title}': {ex.Message}");
                throw;
            }
        }

        public async Task<List<Note>> GetNotesByUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Fetching notes for user with ID: {userId}");
                var notes = await _context.Notes
                    .Where(n => (n.CreatedBy == userId || n.Collaborators.Any(c => c.UserId == userId)) && !n.IsDeleted)
                    .Include(n => n.NoteLabels)
                        .ThenInclude(nl => nl.Label)
                    .Include(n => n.Collaborators)
                        .ThenInclude(c => c.User)
                    .ToListAsync();
                _logger.LogInformation($"Found {notes.Count} notes for user with ID: {userId}");
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
                var notes = await _context.Notes
                    .Where(n => (n.CreatedBy == userId || n.Collaborators.Any(c => c.UserId == userId)) && n.IsArchived)
                    .Include(n => n.NoteLabels)
                        .ThenInclude(nl => nl.Label)
                    .Include(n => n.Collaborators)
                        .ThenInclude(c => c.User)
                    .ToListAsync();
                _logger.LogInformation($"Found {notes.Count} archived notes for user with ID: {userId}");
                return notes;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching archived notes for user with ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Note>> GetNotesFromBin(int userId)
        {
            try
            {
                _logger.LogInformation($"Fetching deleted notes for user with ID: {userId}");
                var notes = await _context.Notes
                    .Where(n => (n.CreatedBy == userId || n.Collaborators.Any(c => c.UserId == userId)) && n.IsDeleted)
                    .Include(n => n.NoteLabels)
                        .ThenInclude(nl => nl.Label)
                    .Include(n => n.Collaborators)
                        .ThenInclude(c => c.User)
                    .ToListAsync();
                _logger.LogInformation($"Found {notes.Count} deleted notes for user with ID: {userId}");
                return notes;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching deleted notes for user with ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Updating note with ID: {noteId} for user with ID: {userId}");
                var existingNote = await _context.Notes
                    .Include(n => n.NoteLabels)
                        .ThenInclude(nl => nl.Label)
                    .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);

                if (existingNote == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found or unauthorized access.");
                    return null;
                }

                existingNote.Title = note.Title;
                existingNote.Description = note.Description;
                existingNote.Color = note.Color;
                existingNote.IsDeleted = note.IsDeleted;
                existingNote.IsArchived = note.IsArchived;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note with ID {noteId} updated successfully.");
                return existingNote;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error updating note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Note> ArchiveNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Archiving note with ID: {noteId} for user with ID: {userId}");
                var existingNote = await _context.Notes
                    .Include(n => n.NoteLabels)
                        .ThenInclude(nl => nl.Label)
                    .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);

                if (existingNote == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found or unauthorized.");
                    throw new KeyNotFoundException("Note not found or unauthorized");
                }

                if (existingNote.IsDeleted)
                {
                    _logger.LogWarning($"Cannot archive a deleted note with ID {noteId}");
                    throw new InvalidOperationException("Cannot archive a deleted note");
                }

                existingNote.IsArchived = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note with ID {noteId} archived successfully.");
                return existingNote;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error archiving note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UnArchiveNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Unarchiving note with ID: {noteId} for user with ID: {userId}");
                var existingNote = await _context.Notes
                    .Include(n => n.NoteLabels)
                        .ThenInclude(nl => nl.Label)
                    .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);
                if (existingNote == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found or unauthorized.");
                    return false;
                }
                existingNote.IsArchived = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note with ID {noteId} unarchived successfully.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error unarchiving note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Deleting note with ID: {noteId} for user with ID: {userId}");
                var note = await _context.Notes
                    .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);

                if (note == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found or unauthorized.");
                    return false;
                }

                note.IsDeleted = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note with ID {noteId} deleted successfully.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error deleting note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RestoreFromBin(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Restoring note with ID: {noteId} for user with ID: {userId}");
                var note = await _context.Notes
                  .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);
                if (note == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found or unauthorized.");
                    return false;
                }
                note.IsDeleted = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note with ID {noteId} restored successfully.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error restoring note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeletePermNoteAsync(int noteId, int userId)
        {
            try
            {
                _logger.LogInformation($"Permanently deleting note with ID: {noteId} for user with ID: {userId}");
                var note = await _context.Notes
                    .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);
                if (note == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found or unauthorized.");
                    return false;
                }
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note with ID {noteId} permanently deleted.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error permanently deleting note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Note> GetNoteByIdAsync(int noteId)
        {
            try
            {
                _logger.LogInformation($"Fetching note with ID: {noteId}");
                var note = await _context.Notes
                    .Include(n => n.NoteLabels)
                        .ThenInclude(nl => nl.Label)
                    .Include(n => n.Collaborators)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(n => n.NoteId == noteId);

                if (note == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found.");
                }

                return note;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching note with ID {noteId}: {ex.Message}");
                throw;
            }
        }
    }
}
