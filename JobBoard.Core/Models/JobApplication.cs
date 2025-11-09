using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Core.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public Job? Job { get; set; }
        public string ApplicantName { get; set; } =String.Empty;
        public string ApplicantEmail { get; set; } = String.Empty;
        public DateTime AppliedAt { get; set; } =DateTime.UtcNow;

    }
}
