using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.DTOs
{
   public class CreateJobRequest
    {
        public string Title { get; set; } = String.Empty;

        public string Description { get; set; } = String.Empty;
    }
}
