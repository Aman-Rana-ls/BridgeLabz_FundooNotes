using System.Collections.Generic;
using System.Threading.Tasks;
using RepoLayer.Entity;
using ModelLayer;

namespace RepoLayer.Interfaces
{
    public interface INoteRL
    {
        Task<Note> CreateNoteAsync(Note note);
        Task<List<Note>> GetNotesByUserAsync(int userId);
        Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId);
        Task<bool> DeleteNoteAsync(int noteId, int userId);
        Task<Note> GetNoteByIdAsync(int noteId);
    }
}