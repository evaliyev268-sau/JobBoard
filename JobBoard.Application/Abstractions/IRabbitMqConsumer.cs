using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobBoard.Application.Abstractions
{
    public interface IRabbitMqConsumer
    {
        Task ConsumeAsync(CancellationToken ct=default);
    }
}
