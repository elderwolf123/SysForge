using System;
using System.Threading.Tasks;

namespace RamOptimizerNova.Services;

public interface IMessengerService
{
    Task PublishAsync<TMessage>(TMessage message);
    Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler);
    Task UnsubscribeAsync<TMessage>(Func<TMessage, Task> handler);
    void ClearAllSubscriptions();
}

public record SystemMessage(string Type, string Message, DateTime Timestamp);
public record ErrorMessage(string Error, Exception? Exception = null);
public record StatusMessage(string Status, bool IsSuccess = true);
public record NavigationMessage(string Page, object? Data = null);