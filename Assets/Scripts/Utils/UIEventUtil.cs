using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class UIEventUtil
{
    public static void AddHover(Button btn, System.Action onEnter, System.Action onExit)
    {
        EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener(_ => onEnter?.Invoke());
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener(_ => onExit?.Invoke());
        trigger.triggers.Add(exit);
    }
}
