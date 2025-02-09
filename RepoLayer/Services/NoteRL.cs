using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepoLayer.Context;
using RepoLayer.Entity;
using RepoLayer.Interfaces;
using ModelLayer;

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
                .Include(n => n.NoteLabels) // Include NoteLabels
                .ThenInclude(nl => nl.Label) // Include Label for each NoteLabel
                .ToListAsync();
        }

        public async Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId)
        {
            var existingNote = await _context.Notes
                .Include(n => n.NoteLabels) // Include NoteLabels
                .ThenInclude(nl => nl.Label) // Include Label for each NoteLabel
                .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);

            if (existingNote == null)
            {
                return null; // Note not found or unauthorized
            }

            // Update note properties
            existingNote.Title = note.Title;
            existingNote.Description = note.Description;
            existingNote.Color = note.Color;
            existingNote.IsDeleted = note.IsDeleted;
            existingNote.IsArchived = note.IsArchived;

            await _context.SaveChangesAsync();
            return existingNote;
        }

        public async Task<bool> DeleteNoteAsync(int noteId, int userId)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);

            if (note == null)
            {
                return false; // Note not found or unauthorized
            }

            note.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Note> GetNoteByIdAsync(int noteId)
        {
            return await _context.Notes
                .Include(n => n.NoteLabels) // Include NoteLabels
                .ThenInclude(nl => nl.Label) // Include Label for each NoteLabel
                .FirstOrDefaultAsync(n => n.NoteId == noteId);
        }
    }
}