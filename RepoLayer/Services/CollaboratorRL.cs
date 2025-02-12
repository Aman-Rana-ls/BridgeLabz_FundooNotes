using Microsoft.EntityFrameworkCore;
using RepoLayer.Context;
using RepoLayer.Entity;
using RepoLayer.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RepositoryLayer.Services
{
    public class CollaboratorRL : ICollaboratorRL
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CollaboratorRL> _logger;

        public CollaboratorRL(ApplicationDbContext context, ILogger<CollaboratorRL> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AddCollaborator(int noteId, string collaboratorEmail)
        {
            try
            {
                _logger.LogInformation($"Attempting to add collaborator with email: {collaboratorEmail} to note with ID: {noteId}");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == collaboratorEmail);
                if (user == null)
                {
                    _logger.LogWarning($"User with email {collaboratorEmail} not found.");
                    return false;
                }

                var note = await _context.Notes.FindAsync(noteId);
                if (note == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} not found.");
                    return false;
                }

                var collaborator = new NoteCollaborator
                {
                    NoteId = noteId,
                    UserId = user.Id
                };

                _context.NoteCollaborators.Add(collaborator);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Collaborator with email {collaboratorEmail} added to note with ID {noteId}.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error adding collaborator to note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RemoveCollaborator(int noteId, string collaboratorEmail)
        {
            try
            {
                _logger.LogInformation($"Attempting to remove collaborator with email: {collaboratorEmail} from note with ID: {noteId}");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == collaboratorEmail);
                if (user == null)
                {
                    _logger.LogWarning($"User with email {collaboratorEmail} not found.");
                    return false;
                }

                var collaborator = await _context.NoteCollaborators
                    .FirstOrDefaultAsync(nc => nc.NoteId == noteId && nc.UserId == user.Id);

                if (collaborator == null)
                {
                    _logger.LogWarning($"Collaborator with email {collaboratorEmail} not found for note with ID {noteId}.");
                    return false;
                }

                _context.NoteCollaborators.Remove(collaborator);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Collaborator with email {collaboratorEmail} removed from note with ID {noteId}.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error removing collaborator from note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetCollaboratorsByNoteId(int noteId)
        {
            try
            {
                _logger.LogInformation($"Fetching collaborators for note with ID: {noteId}");
                var collaborators = await _context.NoteCollaborators
                    .Where(nc => nc.NoteId == noteId)
                    .Include(nc => nc.User)
                    .Select(nc => nc.User.Email)
                    .ToListAsync();

                _logger.LogInformation($"Found {collaborators.Count} collaborators for note with ID {noteId}.");
                return collaborators;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching collaborators for note with ID {noteId}: {ex.Message}");
                throw;
            }
        }
    }
}
