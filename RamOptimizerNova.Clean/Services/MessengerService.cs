using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RamOptimizerNova.Services;

public class MessengerService : IMessengerService
{
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task PublishAsync<TMessage>(TMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = typeof(TMessage);
        
        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            var tasks = handlers.Select(handler => handler(message)).ToArray();
            await Task.WhenAll(tasks);
        }
    }

    public async Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        await _semaphore.WaitAsync();
        
        try
        {
            var messageType = typeof(TMessage);
            if (!_handlers.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<Func<object, Task>>();
                _handlers[messageType] = handlers;
            }
            
            handlers.Add(async msg => await handler((TMessage)msg));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UnsubscribeAsync<TMessage>(Func<TMessage, Task> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        await _semaphore.WaitAsync();
        
        try
        {
            var messageType = typeof(TMessage);
            if (_handlers.TryGetValue(messageType, out var handlers))
            {
                var handlerToRemove = handlers.FirstOrDefault(h => 
                    h.Method == handler.Method && 
                    h.Target == handler.Target);
                
                if (handlerToRemove != null)
                {
                    handlers.Remove(handlerToRemove);
                    
                    if (handlers.Count == 0)
                    {
                        _handlers.TryRemove(messageType, out _);
                    }
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ClearAllSubscriptions()
    {
        _handlers.Clear();
    }
}