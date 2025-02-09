using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RepoLayer.Entity;

namespace RepoLayer.Entity
{
    public class NoteCollaborator
    {
        public int NoteId { get; set; }
        public int UserId { get; set; }

        [JsonIgnore] 
        [ForeignKey("NoteId")]
        public virtual Note Note { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
