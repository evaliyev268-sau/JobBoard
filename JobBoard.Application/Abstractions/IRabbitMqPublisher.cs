namespace JobBoard.Application.Abstractions
{
   public interface IRabbitMqPublisher
    {
        Task PublishAsync(object payload, CancellationToken ct=default);
    }
}
