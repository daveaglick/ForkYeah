using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ForkYeah.Models.Default
{
    public class Archive
    {
        public IEnumerable<RepositoryListItem> ListItems { get; set; }
        public int? NewerPage { get; set; }
        public int? OlderPage { get; set; }
    }
}