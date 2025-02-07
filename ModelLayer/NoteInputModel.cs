using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RepoLayer
{
    public class NoteInputModel
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Color { get; set; }
    }
}