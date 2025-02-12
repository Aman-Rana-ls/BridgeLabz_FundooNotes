using BusinessLayer.Interfaces;
using RepoLayer.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services
{
    public class CollaboratorBL : ICollaboratorBL
    {
        private readonly ICollaboratorRL _collaboratorRL;
        private readonly ILogger<CollaboratorBL> _logger;

        public CollaboratorBL(ICollaboratorRL collaboratorRL, ILogger<CollaboratorBL> logger)
        {
            _collaboratorRL = collaboratorRL;
            _logger = logger;
        }

        public async Task<bool> AddCollaborator(int noteId, string collaboratorEmail)
        {
            try
            {
                _logger.LogInformation($"Attempting to add collaborator {collaboratorEmail} to note {noteId}");
                var result = await _collaboratorRL.AddCollaborator(noteId, collaboratorEmail);
                if (result)
                {
                    _logger.LogInformation($"Collaborator {collaboratorEmail} added to note {noteId} successfully");
                }
                else
                {
                    _logger.LogWarning($"Failed to add collaborator {collaboratorEmail} to note {noteId}");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error adding collaborator {collaboratorEmail} to note {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RemoveCollaborator(int noteId, string collaboratorEmail)
        {
            try
            {
                _logger.LogInformation($"Attempting to remove collaborator {collaboratorEmail} from note {noteId}");
                var result = await _collaboratorRL.RemoveCollaborator(noteId, collaboratorEmail);
                if (result)
                {
                    _logger.LogInformation($"Collaborator {collaboratorEmail} removed from note {noteId} successfully");
                }
                else
                {
                    _logger.LogWarning($"Failed to remove collaborator {collaboratorEmail} from note {noteId}");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error removing collaborator {collaboratorEmail} from note {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetCollaboratorsByNoteId(int noteId)
        {
            try
            {
                _logger.LogInformation($"Fetching collaborators for note {noteId}");
                var collaborators = await _collaboratorRL.GetCollaboratorsByNoteId(noteId);
                if (collaborators == null || collaborators.Count == 0)
                {
                    _logger.LogWarning($"No collaborators found for note {noteId}");
                }
                return collaborators;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching collaborators for note {noteId}: {ex.Message}");
                throw;
            }
        }
    }
}
