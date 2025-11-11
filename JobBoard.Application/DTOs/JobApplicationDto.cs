using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.DTOs
{
    public record JobApplicationDto(int Id, int Jobİd,string ApplicantName,string ApplicantEmail,DateTime AppliedAt);
   
    
}
