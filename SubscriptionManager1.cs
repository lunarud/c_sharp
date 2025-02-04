using System;
using System.Collections.Generic;
using AmpsClientLibrary; // Assuming you have an AMPS client library

public class SubscriptionManager
{
    private readonly IAmpsClient _ampsClient;
    private readonly Dictionary<string, Subscription> _subscriptions;

    public SubscriptionManager(IAmpsClient ampsClient)
    {
        _ampsClient = ampsClient ?? throw new ArgumentNullException(nameof(ampsClient));
        _subscriptions = new Dictionary<string, Subscription>();
    }

    public void Subscribe(string topic, Action<Message> messageHandler)
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic cannot be null or empty.", nameof(topic));

        if (messageHandler == null)
            throw new ArgumentNullException(nameof(messageHandler));

        if (_subscriptions.ContainsKey(topic))
        {
            Console.WriteLine($"Already subscribed to topic: {topic}");
            return;
        }

        var subscription = _ampsClient.Subscribe(topic, messageHandler);
        _subscriptions[topic] = subscription;

        Console.WriteLine($"Subscribed to topic: {topic}");
    }

    public void Unsubscribe(string topic)
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic cannot be null or empty.", nameof(topic));

        if (_subscriptions.TryGetValue(topic, out var subscription))
        {
            _ampsClient.Unsubscribe(subscription);
            _subscriptions.Remove(topic);
            Console.WriteLine($"Unsubscribed from topic: {topic}");
        }
        else
        {
            Console.WriteLine($"No subscription found for topic: {topic}");
        }
    }

    public void UnsubscribeAll()
    {
        foreach (var topic in _subscriptions.Keys)
        {
            Unsubscribe(topic);
        }
    }

    public void Dispose()
    {
        UnsubscribeAll();
        _ampsClient.Dispose();
    }
}

// Example usage
public class Program
{
    public static void Main(string[] args)
    {
        // Assuming you have an AMPS client instance
        IAmpsClient ampsClient = new AmpsClient("localhost:9007");

        var subscriptionManager = new SubscriptionManager(ampsClient);

        // Subscribe to a topic
        subscriptionManager.Subscribe("orders", message =>
        {
            Console.WriteLine($"Received message: {message.Content}");
        });

        // Unsubscribe from a topic
        subscriptionManager.Unsubscribe("orders");

        // Dispose the subscription manager
        subscriptionManager.Dispose();
    }
}

// Mock AMPS client interface and classes for illustration purposes
public interface IAmpsClient : IDisposable
{
    Subscription Subscribe(string topic, Action<Message> messageHandler);
    void Unsubscribe(Subscription subscription);
}

public class AmpsClient : IAmpsClient
{
    private readonly string _connectionString;

    public AmpsClient(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Subscription Subscribe(string topic, Action<Message> messageHandler)
    {
        // Simulate subscription logic
        Console.WriteLine($"Connecting to AMPS server at {_connectionString} and subscribing to {topic}");
        return new Subscription(topic, messageHandler);
    }

    public void Unsubscribe(Subscription subscription)
    {
        // Simulate unsubscription logic
        Console.WriteLine($"Unsubscribing from {subscription.Topic}");
    }

    public void Dispose()
    {
        // Simulate cleanup
        Console.WriteLine("Disposing AMPS client");
    }
}

public class Subscription
{
    public string Topic { get; }
    public Action<Message> MessageHandler { get; }

    public Subscription(string topic, Action<Message> messageHandler)
    {
        Topic = topic;
        MessageHandler = messageHandler;
    }
}

public class Message
{
    public string Content { get; set; }
}
