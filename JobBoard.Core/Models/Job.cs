using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Core.Models
{
    public class Job
    {
        public int Id { get; set; }
        public string Title { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

    }
}
