using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using RepoLayer.Entity;

namespace RepoLayer.Entity
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        // Add the Notes navigation property to represent the one-to-many relationship with Note
        public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

        // Add the CollaboratedNotes navigation property to represent the many-to-many relationship with Note
        [JsonIgnore] // Prevent circular reference during serialization
        public virtual ICollection<NoteCollaborator> CollaboratedNotes { get; set; } = new List<NoteCollaborator>();

        // New RefreshToken property to store refresh tokens for the user
        public string RefreshToken { get; set; }
    }
}
