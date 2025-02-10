using BusinessLayer.Interfaces;
using RepoLayer.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class CollaboratorBL : ICollaboratorBL
    {
        private readonly ICollaboratorRL _collaboratorRL;

        public CollaboratorBL(ICollaboratorRL collaboratorRL)
        {
            _collaboratorRL = collaboratorRL;
        }

        public async Task<bool> AddCollaborator(int noteId, string collaboratorEmail)
        {
            return await _collaboratorRL.AddCollaborator(noteId, collaboratorEmail);
        }

        public async Task<bool> RemoveCollaborator(int noteId, string collaboratorEmail)
        {
            return await _collaboratorRL.RemoveCollaborator(noteId, collaboratorEmail);
        }

        public async Task<List<string>> GetCollaboratorsByNoteId(int noteId)
        {
            return await _collaboratorRL.GetCollaboratorsByNoteId(noteId);
        }
    }
}