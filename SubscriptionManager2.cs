using System;
using AMPS.Client;

public class SubscriptionManager : IDisposable
{
    private HAClient _client;
    private string _serverAddress;
    private string _topic;
    private Action<Message> _messageHandler;

    public SubscriptionManager(string serverAddress, string topic, Action<Message> messageHandler)
    {
        _serverAddress = serverAddress;
        _topic = topic;
        _messageHandler = messageHandler;
        _client = new HAClient("SubscriptionManagerClient");

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            _client.connect(_serverAddress);
            _client.logon();

            Console.WriteLine("Connected to AMPS server at " + _serverAddress);
            StartSubscription();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error connecting to AMPS: " + ex.Message);
        }
    }

    private void StartSubscription()
    {
        try
        {
            _client.subscribe(_messageHandler, _topic);
            Console.WriteLine("Subscribed to topic: " + _topic);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Subscription error: " + ex.Message);
        }
    }

    public void Dispose()
    {
        if (_client != null)
        {
            _client.disconnect();
            _client.Dispose();
            Console.WriteLine("Disconnected from AMPS.");
        }
    }
}

// Example Usage
class Program
{
    static void Main()
    {
        string server = "tcp://localhost:9007/amps/json";
        string topic = "orders";

        using (var subscription = new SubscriptionManager(server, topic, HandleMessage))
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    static void HandleMessage(Message message)
    {
        Console.WriteLine("Received message: " + message.Data);
    }
}
