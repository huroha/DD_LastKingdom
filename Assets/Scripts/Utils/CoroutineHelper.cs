using System.Collections;
using UnityEngine;

public class CoroutineHelper
{
    public static Coroutine Restart(MonoBehaviour owner, ref Coroutine tracker, IEnumerator routine)
    {
        if (tracker != null)
            owner.StopCoroutine(tracker);
        tracker = owner.StartCoroutine(routine);
        return tracker;
    }

}
