using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextContainer.Models
{
    public class Filter
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(500)]
        [Required]
        public string Name { get; set; }
    }
}
