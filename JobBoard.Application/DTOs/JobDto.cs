using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.DTOs
{
    public record JobDto(
        int Id,
        string Title,
        string Description
        );
}
