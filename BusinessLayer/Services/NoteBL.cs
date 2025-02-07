using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessLayer.Interfaces;
using RepoLayer;
using RepoLayer.EntityOne;
using RepoLayer.Interfaces;

namespace BusinessLayer.Services
{
    public class NoteBL : INoteBL
    {
        private readonly INoteRL _noteRL;

        public NoteBL(INoteRL noteRL)
        {
            _noteRL = noteRL;
        }

        public async Task<Note> CreateNoteAsync(NoteInputModel model, int userId)
        {
            var note = new Note
            {
                Title = model.Title,
                Description = model.Description,
                Color = model.Color,
                CreatedBy = userId,
                IsDeleted = false,
                IsArchived = false
            };

            return await _noteRL.CreateNoteAsync(note);
        }

        public async Task<List<Note>> GetNotesByUserAsync(int userId)
        {
            return await _noteRL.GetNotesByUserAsync(userId);
        }

        public async Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId)
        {
            return await _noteRL.UpdateNoteAsync(note, noteId, userId);
        }

        public async Task<bool> DeleteNoteAsync(int noteId, int userId)
        {
            var note = await _noteRL.GetNoteByIdAsync(noteId);
            if (note == null || note.CreatedBy != userId)
            {
                return false;
            }

            return await _noteRL.DeleteNoteAsync(noteId);
        }

        public async Task<Note> GetNoteByIdAsync(int noteId)
        {
            return await _noteRL.GetNoteByIdAsync(noteId);
        }

        public int GetUserIdFromToken(ClaimsPrincipal user)
        {
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim?.Value == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found or invalid in token.");
            }

            return userId;
        }
    }
}
