using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForkYeah.Data
{
    public class UpdateHistory
    {
        [Key]
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public int TotalCount { get; set; }
        public int? UpdatedCount { get; set; }
        public string Exception { get; set; }
    }
}
