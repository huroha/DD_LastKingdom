using System;
using System.Collections.Generic;

/// <summary>
/// 타입 안전한 전역 이벤트 버스.
/// 사용법: EventBus.Subscribe<MyEvent>(OnMyEvent);
///         EventBus.Publish(new MyEvent(...));
///         EventBus.Unsubscribe<MyEvent>(OnMyEvent);
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _handlers = new();

    public static void Subscribe<TEvent>(Action<TEvent> handler)
    {
        var type = typeof(TEvent);
        if (_handlers.TryGetValue(type, out var existing))
            _handlers[type] = Delegate.Combine(existing, handler);
        else
            _handlers[type] = handler;
    }

    public static void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        var type = typeof(TEvent);
        if (_handlers.TryGetValue(type, out var existing))
        {
            var updated = Delegate.Remove(existing, handler);
            if (updated == null)
                _handlers.Remove(type);
            else
                _handlers[type] = updated;
        }
    }

    public static void Publish<TEvent>(TEvent evt)
    {
        var type = typeof(TEvent);
        if (_handlers.TryGetValue(type, out var handler))
            ((Action<TEvent>)handler).Invoke(evt);
    }

    public static void Clear()
    {
        _handlers.Clear();
    }
}
