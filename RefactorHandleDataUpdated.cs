https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/asynchronous-programming-model-apm
https://dev.to/absjabed/publisher-subscriber-vs-observer-pattern-with-c-3gpc
https://github.com/absjabed/PubSub-and-Observable-Design-pattern/blob/master/pubSubEventDelegate/Subscriber.cs
https://dev-journal.netlify.app/blog/observer-pubsub-indepth-csharp
https://github.com/absjabed/PubSub-and-Observable-Design-pattern/blob/master/pubSubEventDelegate/Publisher.cs
https://hackernoon.com/observer-vs-pub-sub-pattern-50d3b27f838c
https://c-sharptutorial.com/event/event-in-csharp
https://medium.com/@joaopedrosmelo/design-patterns-in-c-part-4-enhancing-event-driven-architectures-with-the-observer-pattern-33632811964e
https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/event-based-asynchronous-pattern-eap
https://olegkarasik.wordpress.com/2019/04/16/code-tip-how-to-work-with-asynchronous-event-handlers-in-c/
// Define the method that will handle the event
private void HandleDataUpdated(string action, string key)
{
    Console.WriteLine($"ConsumerBackgroundService received notification: {action} operation on key '{key}'");
}

// Assign the method to the event
_dataStore.OnDataUpdated += HandleDataUpdated;
https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/event-based-asynchronous-pattern-overview
https://medium.com/@joaopedrosmelo/design-patterns-in-c-part-4-enhancing-event-driven-architectures-with-the-observer-pattern-33632811964e
https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.asyncoperationmanager?view=net-9.0
https://medium.com/@joaopedrosmelo/design-patterns-in-c-part-4-enhancing-event-driven-architectures-with-the-observer-pattern-33632811964e


