using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RepoLayer.Entity
{
    public class Label
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LabelId { get; set; }

        [Required]
        public string Name { get; set; }

        [JsonIgnore] // Prevent circular reference during serialization
        public virtual ICollection<NoteLabel> NoteLabels { get; set; } = new List<NoteLabel>();
    }
}