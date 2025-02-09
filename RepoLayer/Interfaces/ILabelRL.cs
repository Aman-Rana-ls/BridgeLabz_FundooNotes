using RepoLayer.Entity;

namespace RepoLayer.Interfaces
{
    public interface ILabelRL
    {
        Task<IEnumerable<Label>> GetAllLabelsAsync();
        Task<Label> GetLabelByIdAsync(int id);
        Task<Label> CreateLabelAsync(Label label, List<int> noteIds); // Existing method
        Task<Label> CreateLabelAsyncWithoutId(Label label); // New method
        Task<bool> AssignLabelToNoteAsync(int noteId, int labelId);
        Task<bool> RemoveLabelFromNoteAsync(int noteId, int labelId);
        Task<bool> DeleteLabelAsync(int labelId);
    }
}
