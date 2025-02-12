using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepoLayer.Context;
using RepoLayer.Entity;
using RepoLayer.Interfaces;
using Microsoft.Extensions.Logging;

namespace RepoLayer.Services
{
    public class LabelRL : ILabelRL
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LabelRL> _logger;

        public LabelRL(ApplicationDbContext context, ILogger<LabelRL> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Label> CreateLabelAsyncWithoutId(Label label)
        {
            try
            {
                _logger.LogInformation("Creating label without ID.");
                _context.Labels.Add(label);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Label '{label.Name}' created successfully.");
                return label;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error creating label without ID: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Label>> GetAllLabelsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all labels.");
                var labels = await _context.Labels
                    .Include(l => l.NoteLabels)
                    .ThenInclude(nl => nl.Note)
                    .ToListAsync();
                _logger.LogInformation($"Found {labels.Count} labels.");
                return labels;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching all labels: {ex.Message}");
                throw;
            }
        }

        public async Task<Label> GetLabelByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching label with ID: {id}");
                var label = await _context.Labels
                    .Include(l => l.NoteLabels)
                    .ThenInclude(nl => nl.Note)
                    .FirstOrDefaultAsync(l => l.LabelId == id);

                if (label == null)
                {
                    _logger.LogWarning($"Label with ID {id} not found.");
                }
                return label;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching label with ID {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<Label> CreateLabelAsync(Label label)
        {
            try
            {
                _logger.LogInformation($"Creating label with name: {label.Name}");
                _context.Labels.Add(label);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Label '{label.Name}' created successfully.");
                return label;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error creating label '{label.Name}': {ex.Message}");
                throw;
            }
        }

        public async Task<Label> CreateLabelAsync(Label label, List<int> noteIds)
        {
            try
            {
                _logger.LogInformation($"Creating label '{label.Name}' and associating with {noteIds.Count} notes.");
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
                _logger.LogInformation($"Label '{label.Name}' created and associated with {noteIds.Count} notes.");
                return label;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error creating label '{label.Name}' and associating with notes: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AssignLabelToNoteAsync(int noteId, int labelId)
        {
            try
            {
                _logger.LogInformation($"Assigning label with ID {labelId} to note with ID {noteId}");
                var note = await _context.Notes.FindAsync(noteId);
                var label = await _context.Labels.FindAsync(labelId);

                if (note == null || label == null)
                {
                    _logger.LogWarning($"Note with ID {noteId} or Label with ID {labelId} not found.");
                    return false;
                }

                var existingNoteLabel = await _context.NoteLabels
                    .FirstOrDefaultAsync(nl => nl.NoteId == noteId && nl.LabelId == labelId);

                if (existingNoteLabel != null)
                {
                    _logger.LogWarning($"Label with ID {labelId} is already assigned to note with ID {noteId}.");
                    return false;
                }

                var newNoteLabel = new NoteLabel
                {
                    NoteId = noteId,
                    LabelId = labelId
                };

                _context.NoteLabels.Add(newNoteLabel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Label with ID {labelId} successfully assigned to note with ID {noteId}.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error assigning label with ID {labelId} to note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RemoveLabelFromNoteAsync(int noteId, int labelId)
        {
            try
            {
                _logger.LogInformation($"Removing label with ID {labelId} from note with ID {noteId}");
                var noteLabel = await _context.NoteLabels
                    .FirstOrDefaultAsync(nl => nl.NoteId == noteId && nl.LabelId == labelId);

                if (noteLabel == null)
                {
                    _logger.LogWarning($"Label with ID {labelId} not assigned to note with ID {noteId}.");
                    return false;
                }

                _context.NoteLabels.Remove(noteLabel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Label with ID {labelId} removed from note with ID {noteId}.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error removing label with ID {labelId} from note with ID {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AssignLabelToNotesAsync(List<int> noteIds, int labelId)
        {
            try
            {
                _logger.LogInformation($"Assigning label with ID {labelId} to {noteIds.Count} notes.");
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

                if (!noteLabels.Any())
                {
                    _logger.LogWarning($"No valid notes found for label with ID {labelId}.");
                    return false;
                }

                _context.NoteLabels.AddRange(noteLabels);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Label with ID {labelId} successfully assigned to {noteLabels.Count} notes.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error assigning label with ID {labelId} to notes: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteLabelAsync(int labelId)
        {
            try
            {
                _logger.LogInformation($"Deleting label with ID {labelId}");
                var label = await _context.Labels.Include(l => l.NoteLabels)
                                                   .FirstOrDefaultAsync(l => l.LabelId == labelId);

                if (label == null)
                {
                    _logger.LogWarning($"Label with ID {labelId} not found.");
                    return false;
                }

                _context.NoteLabels.RemoveRange(label.NoteLabels);
                _context.Labels.Remove(label);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Label with ID {labelId} deleted successfully.");
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error deleting label with ID {labelId}: {ex.Message}");
                throw;
            }
        }
    }
}
