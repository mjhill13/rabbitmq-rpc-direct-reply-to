using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace rabbitmq_rpc_direct_reply_to;

static class Program
{
    private const string QueueName = "my-queue";
    private const string ReplyTo = "amq.rabbitmq.reply-to";
    
    public static void Main(string[] args)
    {
        var server = new Server();
        var client = new Client();
        
        string? message;
        do
        {
            Console.WriteLine("Enter a message to reverse:");
            message = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(message));

        var response = client.Call(message);
        Console.WriteLine($"Response: {response}");
    }

    class Client
    {
        private readonly BlockingCollection<string> _responseQueue = new();
        
        public string Call(string message)
        {
            // create a connection
            var factory = new ConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // create a consumer to wait for a message received
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, args) =>
            {
                // receive and to responses
                var response = Encoding.UTF8.GetString(args.Body.ToArray());
                _responseQueue.Add(response);
            };

            // publish a message, ensure to set reply-to property to 'amq.rabbitmq.reply-to' in no-ack mode
            var properties = channel.CreateBasicProperties();
            properties.ReplyTo = ReplyTo;
            channel.BasicConsume(consumer, ReplyTo, true);
            channel.BasicPublish("", QueueName, properties, Encoding.UTF8.GetBytes(message));
            
            return _responseQueue.Take();
        }
    }
    
    class Server
    {
        public Server()
        {
            // create a connection
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            // declare the queue with exclusive false to stop queue locking errors
            channel.QueueDeclare(QueueName, exclusive: false);

            // create a consumer to wait for a message received
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, args) =>
            {
                // receive the message, reverse it and send a response back
                var message = Encoding.UTF8.GetString(args.Body.ToArray());
                var response = Reverse(message);
                var properties = channel.CreateBasicProperties();
                channel.BasicPublish("", args.BasicProperties.ReplyTo, properties, Encoding.UTF8.GetBytes(response));
            };
            
            // start the consumer
            channel.BasicConsume(consumer, QueueName);
        }

        private string Reverse(string message)
        {
            char[] arr = message.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }
    }
}