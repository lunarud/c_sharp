https://dev.to/absjabed/publisher-subscriber-vs-observer-pattern-with-c-3gpc
https://github.com/absjabed/PubSub-and-Observable-Design-pattern/blob/master/pubSubEventDelegate/Subscriber.cs
https://dev-journal.netlify.app/blog/observer-pubsub-indepth-csharp
https://github.com/absjabed/PubSub-and-Observable-Design-pattern/blob/master/pubSubEventDelegate/Publisher.cs
https://hackernoon.com/observer-vs-pub-sub-pattern-50d3b27f838c
https://c-sharptutorial.com/event/event-in-csharp
https://medium.com/@joaopedrosmelo/design-patterns-in-c-part-4-enhancing-event-driven-architectures-with-the-observer-pattern-33632811964e


// Define the method that will handle the event
private void HandleDataUpdated(string action, string key)
{
    Console.WriteLine($"ConsumerBackgroundService received notification: {action} operation on key '{key}'");
}

// Assign the method to the event
_dataStore.OnDataUpdated += HandleDataUpdated;
