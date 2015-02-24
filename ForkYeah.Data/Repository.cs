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

        [Key]
        [Column(Order = 1)]
        public string Name { get; set; }

        public DateTimeOffset DbAdded { get; set; }

        public int OriginialStargazersCount { get; set; }
        
        // These properties are fetched and cached as part of a periodic search by a web job

        [Required]
        public string Description { get; set; }
        
        [Required]
        public string HtmlUrl { get; set; }
        
        public string Homepage { get; set; }

        public string Language { get; set; }
        
        public int StargazersCount { get; set; }        

        [NotMapped]
        public int StargazersCountChange
        {
            get { return StargazersCount - OriginialStargazersCount; }
        }

        public int OpenIssuesCount { get; set; }
        
        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? PushedAt { get; set; }
        
        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset DbUpdated { get; set; }

        // These properties are fetched and cached on demand when the details page is viewed

        public string ReadmeHtml { get; set; }

        public int Contributors { get; set; }

        public int Commits { get; set; }  // Use the contributors endpoint and sum all contributor commits

        public DateTimeOffset DbDetailsUpdated { get; set; }
    }
}
