using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ForkYeah.Models.Default
{
    public class RepositoryListItem
    {
        public DateTimeOffset DbAdded { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string HtmlUrl { get; set; }
        public int StargazersCount { get; set; }        
        public int StargazersCountChange { get; set; }
        public bool Starred { get; set; }
    }
}