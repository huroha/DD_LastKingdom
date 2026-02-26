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
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

    // TEvent는 하나의 큰 이벤트의 종류 ex) PlayerDiedEvent
    // 추가적으로 딕셔너리로 delegate로 관련된 하위 함수 이벤트들을 가지고있는다. ex ) OnPlayerDied_UI, OnPlayerDied_Sound 등등
    // Dictionary로 만든 이유 -> 검색에서 O(1)의 속도를 보여준다. list는 전체 리스트를 확인하기 때문에 O(n)
    // Delegate는 함수를 변수로 선언한것.

    public static void Subscribe<TEvent>(Action<TEvent> handler)
    {
        Type type = typeof(TEvent);                                        
        if (!_handlers.ContainsKey(type))                                   
            _handlers[type] = new List<Delegate>();                        
        _handlers[type].Add(handler);                                      
    }                                                                      
                                                                           
    public static void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        Type type = typeof(TEvent);
        if (!_handlers.ContainsKey(type))
            return;
        
        _handlers[type].Remove(handler);
        if (_handlers[type].Count == 0)
            _handlers.Remove(type);
       
       
    }

    public static void Publish<TEvent>(TEvent evt)
    {
        Type type = typeof(TEvent);
        if (!_handlers.ContainsKey(type))
            return;
        List<Delegate> handlers = new List<Delegate>(_handlers[type]);      // 복사본을 순회
        for(int i = 0; i < handlers.Count; ++i)
        {
            ((Action<TEvent>)handlers[i]).Invoke(evt);  
        }
    }

    public static void Clear()
    {
        _handlers.Clear();
    }
}
