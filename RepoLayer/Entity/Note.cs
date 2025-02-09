using RepoLayer.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RepoLayer.Entity;
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

    [JsonIgnore] // Prevent circular reference during serialization
    public virtual User? User { get; set; }

    [JsonIgnore] // Prevent serializing the NoteLabels collection
    public virtual ICollection<NoteLabel> NoteLabels { get; set; } = new List<NoteLabel>();

    [JsonIgnore] // Prevent serializing the Collaborators collection
    public virtual ICollection<NoteCollaborator> Collaborators { get; set; } = new List<NoteCollaborator>();

    [NotMapped]
    public List<string> Labels => NoteLabels?.Select(nl => nl.Label.Name).ToList();

    [NotMapped]
    public List<string> CollaboratorEmails => Collaborators?.Select(c => c.User.Email).ToList();
}