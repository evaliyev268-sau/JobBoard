using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.Events
{
    public class JobApplicationCreatedEvent
    {
        public string Type { get; set; } = "JobApplicationCreated";

        public int JobId { get; set; }

        public int ApplicationId { get; set; }

        public string ApplicantName { get; set; } = String.Empty;

        public string ApplicantEmail { get; set; } = String.Empty;

        public DateTime AppliedAt { get; set; }

        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    }
}
