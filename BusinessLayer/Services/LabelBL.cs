using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLayer.Interfaces;
using RepoLayer.Interfaces;
using RepoLayer.Entity;
using ModelLayer;

namespace BusinessLayer.Services
{
    public class LabelBL : ILabelBL
    {
        private readonly ILabelRL _labelRL;

        public LabelBL(ILabelRL labelRL)
        {
            _labelRL = labelRL;
        }

        public async Task<IEnumerable<Label>> GetAllLabelsAsync()
        {
            return await _labelRL.GetAllLabelsAsync();
        }

        public async Task<Label> GetLabelByIdAsync(int id)
        {
            return await _labelRL.GetLabelByIdAsync(id);
        }

        public async Task<Label> CreateLabelAsync(LabelInputModel labelInput)
        {
            var label = new Label
            {
                Name = labelInput.Name
            };

            // If NoteIds are provided, create relationships in the NoteLabel table
            if (labelInput.NoteIds != null && labelInput.NoteIds.Count > 0)
            {
                return await _labelRL.CreateLabelAsync(label, labelInput.NoteIds);
            }

            // Create the label without associating notes
            return await _labelRL.CreateLabelAsyncWithoutId(label);
        }

        public async Task<bool> AssignLabelToNoteAsync(int noteId, int labelId)
        {
            return await _labelRL.AssignLabelToNoteAsync(noteId, labelId);
        }

        public async Task<bool> RemoveLabelFromNoteAsync(int noteId, int labelId)
        {
            return await _labelRL.RemoveLabelFromNoteAsync(noteId, labelId);
        }
        public async Task<bool> DeleteLabelAsync(int labelId)
        {
            return await _labelRL.DeleteLabelAsync(labelId);
        }
    }
}
