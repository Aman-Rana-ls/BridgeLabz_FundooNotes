using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepoLayer;
using RepoLayer.EntityOne;
using RepoLayer.ContextOne;
using RepoLayer.Interfaces;

namespace RepoLayer.Services
{
    public class NoteRL : INoteRL
    {
        private readonly ApplicationDbContext _context;

        public NoteRL(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<List<Note>> GetNotesByUserAsync(int userId)
        {
            return await _context.Notes
                .Where(n => n.CreatedBy == userId && !n.IsDeleted)
                .ToListAsync();
        }

        public async Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId)
        {
            var existingNote = await _context.Notes.FindAsync(noteId);
            if (existingNote == null || existingNote.CreatedBy != userId)
            {
                return null; // Prevent unauthorized modifications
            }

            existingNote.Title = note.Title;
            existingNote.Description = note.Description;
            existingNote.Color = note.Color;
            existingNote.IsDeleted = note.IsDeleted;
            existingNote.IsArchived = note.IsArchived;

            await _context.SaveChangesAsync();
            return existingNote;
        }

        public async Task<bool> DeleteNoteAsync(int noteId)
        {
            var note = await _context.Notes.FindAsync(noteId);
            if (note == null)
            {
                return false;
            }

            note.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<Note> GetNoteByIdAsync(int noteId)
        {
            return await _context.Notes.FindAsync(noteId);
        }

    }
}