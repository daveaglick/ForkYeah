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
        public string OwnerHtmlUrl { get; set; }
        
        [Required]
        public string HtmlUrl { get; set; }
        
        public string Homepage { get; set; }

        public string Language { get; set; }
        
        public int StargazersCount { get; set; }

        public int StargazersCountChange { get; set; }  // This is duplicative of OriginialStargazersCount and StargazersCount, but since we want to sort by it...

        public int ForksCount { get; set; }

        public int OpenIssuesCount { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }   // This can be checked to see if new details are needed

        public DateTimeOffset? PushedAt { get; set; }        

        public DateTimeOffset DbUpdated { get; set; }

        // These properties are fetched and cached on demand when the details page is viewed

        public string ReadmeHtml { get; set; }

        public int ContributorCount { get; set; }

        public int CommitCount { get; set; }  // Use the contributors endpoint and sum all contributor commits

        public DateTimeOffset DbDetailsUpdated { get; set; }
    }
}
