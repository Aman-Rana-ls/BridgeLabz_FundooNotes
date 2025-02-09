using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepoLayer.Context;
using RepoLayer.Entity;
using RepoLayer.Interfaces;

namespace RepoLayer.Services
{
    public class LabelRL : ILabelRL
    {
        private readonly ApplicationDbContext _context;

        public LabelRL(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Label> CreateLabelAsyncWithoutId(Label label)
        {
            _context.Labels.Add(label);
            await _context.SaveChangesAsync();  
            return label;
        }

        public async Task<IEnumerable<Label>> GetAllLabelsAsync()
        {
            return await _context.Labels
                .Include(l => l.NoteLabels) 
                .ThenInclude(nl => nl.Note) 
                .ToListAsync();
        }

        public async Task<Label> GetLabelByIdAsync(int id)
        {
            return await _context.Labels
                .Include(l => l.NoteLabels) 
                .ThenInclude(nl => nl.Note) 
                .FirstOrDefaultAsync(l => l.LabelId == id);
        }

        public async Task<Label> CreateLabelAsync(Label label)
        {
            _context.Labels.Add(label);
            await _context.SaveChangesAsync();
            return label;
        }

        public async Task<Label> CreateLabelAsync(Label label, List<int> noteIds)
        {
            _context.Labels.Add(label);
            await _context.SaveChangesAsync(); 

            foreach (var noteId in noteIds)
            {
                var noteLabel = new NoteLabel
                {
                    NoteId = noteId,
                    LabelId = label.LabelId  
                };

                _context.NoteLabels.Add(noteLabel); 
            }

            await _context.SaveChangesAsync(); 
            return label;
        }

        public async Task<bool> AssignLabelToNoteAsync(int noteId, int labelId)
        {
            var note = await _context.Notes.FindAsync(noteId);
            var label = await _context.Labels.FindAsync(labelId);

            if (note == null || label == null)
                return false;

            var existingNoteLabel = await _context.NoteLabels
                .FirstOrDefaultAsync(nl => nl.NoteId == noteId && nl.LabelId == labelId);

            if (existingNoteLabel != null)
                return false;

            var newNoteLabel = new NoteLabel
            {
                NoteId = noteId,
                LabelId = labelId
            };

            _context.NoteLabels.Add(newNoteLabel);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveLabelFromNoteAsync(int noteId, int labelId)
        {
            var noteLabel = await _context.NoteLabels
                .FirstOrDefaultAsync(nl => nl.NoteId == noteId && nl.LabelId == labelId);

            if (noteLabel == null)
                return false;

            _context.NoteLabels.Remove(noteLabel);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignLabelToNotesAsync(List<int> noteIds, int labelId)
        {
            var noteLabels = new List<NoteLabel>();

            foreach (var noteId in noteIds)
            {
                var note = await _context.Notes.FindAsync(noteId);

                if (note != null)
                {
                    noteLabels.Add(new NoteLabel
                    {
                        NoteId = noteId,
                        LabelId = labelId
                    });
                }
            }

            // If no valid notes are found, return false
            if (!noteLabels.Any())
                return false;

            _context.NoteLabels.AddRange(noteLabels);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> DeleteLabelAsync(int labelId)
        {
            var label = await _context.Labels.Include(l => l.NoteLabels)
                                               .FirstOrDefaultAsync(l => l.LabelId == labelId);
            if (label == null)
                return false;

            _context.NoteLabels.RemoveRange(label.NoteLabels); // Remove associated NoteLabel records
            _context.Labels.Remove(label); // Remove the label
            await _context.SaveChangesAsync();

            return true;
        }

    }
}
