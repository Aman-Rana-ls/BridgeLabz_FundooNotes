using System.Collections.Generic;
using System.Threading.Tasks;
using RepoLayer;
using RepoLayer.EntityOne;

namespace RepoLayer.Interfaces
{
    public interface INoteRL
    {
        Task<Note> CreateNoteAsync(Note note);
        Task<List<Note>> GetNotesByUserAsync(int userId);
        Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId);
        Task<bool> DeleteNoteAsync(int noteId);
        Task<Note> GetNoteByIdAsync(int noteId);

    }
}