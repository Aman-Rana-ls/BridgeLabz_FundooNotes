using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ModelLayer;
using RepoLayer.Entity;

namespace BusinessLayer.Interfaces
{
    public interface INoteBL
    {
        Task<Note> CreateNoteAsync(NoteInputModel model, int userId);
        Task<List<Note>> GetNotesByUserAsync(int userId);
        Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId);
        Task<bool> DeleteNoteAsync(int noteId, int userId);
        Task<Note> GetNoteByIdAsync(int noteId);
        int GetUserIdFromToken(ClaimsPrincipal user);
    }
}