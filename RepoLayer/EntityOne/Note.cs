using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RepoLayer.EntityOne
{
    public class Note
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NoteId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Color { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsArchived { get; set; } = false;

        [ForeignKey("User")]
        public int CreatedBy { get; set; }

        [JsonIgnore]
        public virtual User? User { get; set; }
    }
}
