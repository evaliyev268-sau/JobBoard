using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.Abstractions
{
   public interface IRabbitMqPublisher
    {
        Task PublishAsync(object payload, CancellationToken ct=default);
    }
}
