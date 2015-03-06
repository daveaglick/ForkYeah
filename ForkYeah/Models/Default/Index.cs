using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ForkYeah.Models.Default
{
    public class Index
    {
        public bool Authorized { get; set; }
        public string UserLogin { get; set; }
        public string UserAvatarUrl { get; set; }
        public string UserHtmlUrl { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Languages { get; set; }
    }
}