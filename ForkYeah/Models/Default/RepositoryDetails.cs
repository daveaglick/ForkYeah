using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ForkYeah.Models.Default
{
    public class RepositoryDetails
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DbAdded { get; set; }
        public string Description { get; set; }
        public string OwnerHtmlUrl { get; set; }
        public string HtmlUrl { get; set; }
        public string Homepage { get; set; }
        public string Language { get; set; }
        public int StargazersCount { get; set; }
        public int StargazersCountChange { get; set; }
        public int ForksCount { get; set; }
        public int OpenIssuesCount { get; set; }
        public string ReadmeHtml { get; set; }
        public int ContributorCount { get; set; }
        public int CommitCount { get; set; }
    }
}