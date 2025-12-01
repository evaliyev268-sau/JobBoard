namespace JobBoard.Application.Abstractions
{
  
    public interface IRabbitMqConsumer
    {
        
        bool IsConnected { get; }

        string QueueName { get; }
    }
}