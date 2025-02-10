using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLayer.Interfaces
{
    public interface ICollaboratorBL
    {
        Task<bool> AddCollaborator(int noteId, string collaboratorEmail);
        Task<bool> RemoveCollaborator(int noteId, string collaboratorEmail);
        Task<List<string>> GetCollaboratorsByNoteId(int noteId);
    }
}