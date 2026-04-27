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
        if (tracker != null)
            owner.StopCoroutine(tracker);
        tracker = null;
    }
    public static IEnumerator PulseAlpha(Image[] targets, float speed, float min, float offset)
    {
        int count = targets.Length;
        while (true)
        {
            for (int i = 0; i < count; ++i)
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
    public static float OutQuad(float k) { return 1f - (1f - k) * (1f - k); }

    public static IEnumerator FadeAlpha(SpriteRenderer sr, float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = sr.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            sr.color = c;
            yield return null;
        }
        c.a = to;
        sr.color = c;
    }
    public static IEnumerator FadeAlpha(SpriteRenderer[] srs, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            for (int i = 0; i < srs.Length; ++i)
            {
                Color c = srs[i].color;
                c.a = alpha;
                srs[i].color = c;
            }
            yield return null;
        }
        for (int i = 0; i < srs.Length; ++i)
        {
            Color c = srs[i].color;
            c.a = to;
            srs[i].color = c;
        }
    }
}

