using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RepoLayer.Entity
{
    public class NoteLabel
    {
        public int NoteId { get; set; }
        public int LabelId { get; set; }

        [JsonIgnore] // Prevent circular reference during serialization
        [ForeignKey("NoteId")]
        public virtual Note Note { get; set; }

        [ForeignKey("LabelId")]
        public virtual Label Label { get; set; }
    }
}