using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.DTOs
{
    public class ApplyRequest
    {
        public string ApplicantName { get; set; } = String.Empty;
        public string ApplicantEmail { get; set; } = String.Empty;

    }
}
