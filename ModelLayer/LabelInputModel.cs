using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ModelLayer
{
    public class LabelInputModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public List<int> NoteIds { get; set; }  
    }
}
