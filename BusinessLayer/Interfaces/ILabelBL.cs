using System.Collections.Generic;
using System.Threading.Tasks;
using ModelLayer;
using RepoLayer.Entity;

namespace BusinessLayer.Interfaces
{
    public interface ILabelBL
    {
        Task<IEnumerable<Label>> GetAllLabelsAsync();
        Task<Label> GetLabelByIdAsync(int id);
        Task<Label> CreateLabelAsync(LabelInputModel labelInput);
        Task<bool> AssignLabelToNoteAsync(int noteId, int labelId);
        Task<bool> RemoveLabelFromNoteAsync(int noteId, int labelId);
        Task<bool> DeleteLabelAsync(int labelId);

    }
}
