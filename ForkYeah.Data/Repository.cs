using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForkYeah.Data
{
    public class Repository
    {
        [Key]
        [Column(Order = 0)]
        public string Owner { get; set; }
        
        [Required]
        public string OwnerHtmlUrl { get; set; }

        [Key]
        [Column(Order = 1)]
        public string Name { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public string HtmlUrl { get; set; }
        
        public int StargazersCount { get; set; }
        
        public int StargazersIncrease { get; set; }

        public DateTime Added { get; set; }

        public DateTime Updated { get; set; }
    }
}
