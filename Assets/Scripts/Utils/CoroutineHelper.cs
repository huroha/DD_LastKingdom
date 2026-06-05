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
    // 단일 구간 스케일 (SmoothStep 보간)
    public static IEnumerator ScaleTo(Transform target, float from, float to, float duration)
    {
        float elapsed = 0f;
        target.localScale = Vector3.one * from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.localScale = Vector3.one * Mathf.Lerp(from, to, t);
            yield return null;
        }
        target.localScale = Vector3.one * to;
    }
    // 2단계 스케일 (start→mid : 40%, mid→end : 60%)
    public static IEnumerator PopScale(Transform target, float start, float mid, float end, float duration)
    {
        float phase1 = duration * 0.4f;
        float phase2 = duration * 0.6f;

        float elapsed = 0f;
        target.localScale = Vector3.one * start;
        while (elapsed < phase1)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(start, mid, elapsed / phase1);
            yield return null;
        }
        target.localScale = Vector3.one * mid;

        elapsed = 0f;
        while (elapsed < phase2)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / phase2);
            target.localScale = Vector3.one * Mathf.Lerp(mid, end, t);
            yield return null;
        }
        target.localScale = Vector3.one * end;
    }


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

