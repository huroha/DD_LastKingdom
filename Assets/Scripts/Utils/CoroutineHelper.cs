using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoroutineHelper
{
    public static Coroutine Restart(MonoBehaviour owner, ref Coroutine tracker, IEnumerator routine)
    {
        if (tracker != null)
            owner.StopCoroutine(tracker);
        tracker = owner.StartCoroutine(routine);
        return tracker;
    }
    public static void Stop(MonoBehaviour owner, ref Coroutine tracker)
    {
        if(tracker != null)
            owner.StopCoroutine(tracker);
        tracker = null;
    }
    public static IEnumerator PulseAlpha(Image[]  targets, float speed, float min, float offset)
    {
        int count = targets.Length;
        while(true)
        {
            for (int i=0; i< count; ++i)
            {
                float pingPong = Mathf.PingPong((Time.time + i * offset) * speed, 1f);
                float alpha = Mathf.Lerp(min, 1f, pingPong);
                Color c = targets[i].color;
                c.a = alpha;
                targets[i].color = c;
            }
            yield return null;
        }
    }
}
