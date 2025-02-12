using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLayer.Interfaces;
using RepoLayer.Interfaces;
using RepoLayer.Entity;
using ModelLayer;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services
{
    public class LabelBL : ILabelBL
    {
        private readonly ILabelRL _labelRL;
        private readonly ILogger<LabelBL> _logger;

        public LabelBL(ILabelRL labelRL, ILogger<LabelBL> logger)
        {
            _labelRL = labelRL;
            _logger = logger;
        }

        public async Task<IEnumerable<Label>> GetAllLabelsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all labels.");
                var labels = await _labelRL.GetAllLabelsAsync();
                if (labels == null)
                {
                    _logger.LogWarning("No labels found.");
                }
                return labels;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching labels: {ex.Message}");
                throw;
            }
        }

        public async Task<Label> GetLabelByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching label by ID: {id}");
                var label = await _labelRL.GetLabelByIdAsync(id);
                if (label == null)
                {
                    _logger.LogWarning($"Label with ID {id} not found.");
                }
                return label;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error fetching label by ID {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<Label> CreateLabelAsync(LabelInputModel labelInput)
        {
            try
            {
                _logger.LogInformation($"Creating label: {labelInput.Name}");
                var label = new Label
                {
                    Name = labelInput.Name
                };

                if (labelInput.NoteIds != null && labelInput.NoteIds.Count > 0)
                {
                    var createdLabel = await _labelRL.CreateLabelAsync(label, labelInput.NoteIds);
                    _logger.LogInformation($"Label {labelInput.Name} created and associated with notes.");
                    return createdLabel;
                }
                var labelWithoutNotes = await _labelRL.CreateLabelAsyncWithoutId(label);
                _logger.LogInformation($"Label {labelInput.Name} created without associated notes.");
                return labelWithoutNotes;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error creating label {labelInput.Name}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AssignLabelToNoteAsync(int noteId, int labelId)
        {
            try
            {
                _logger.LogInformation($"Assigning label {labelId} to note {noteId}");
                var result = await _labelRL.AssignLabelToNoteAsync(noteId, labelId);
                if (result)
                {
                    _logger.LogInformation($"Label {labelId} successfully assigned to note {noteId}.");
                }
                else
                {
                    _logger.LogWarning($"Failed to assign label {labelId} to note {noteId}.");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error assigning label {labelId} to note {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RemoveLabelFromNoteAsync(int noteId, int labelId)
        {
            try
            {
                _logger.LogInformation($"Removing label {labelId} from note {noteId}");
                var result = await _labelRL.RemoveLabelFromNoteAsync(noteId, labelId);
                if (result)
                {
                    _logger.LogInformation($"Label {labelId} successfully removed from note {noteId}.");
                }
                else
                {
                    _logger.LogWarning($"Failed to remove label {labelId} from note {noteId}.");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error removing label {labelId} from note {noteId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteLabelAsync(int labelId)
        {
            try
            {
                _logger.LogInformation($"Deleting label {labelId}");
                var result = await _labelRL.DeleteLabelAsync(labelId);
                if (result)
                {
                    _logger.LogInformation($"Label {labelId} deleted successfully.");
                }
                else
                {
                    _logger.LogWarning($"Failed to delete label {labelId}.");
                }
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error deleting label {labelId}: {ex.Message}");
                throw;
            }
        }
    }
}
