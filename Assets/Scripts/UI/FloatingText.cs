using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private float duration = 1.2f;

    private Coroutine _loop;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Play()
    {
        gameObject.SetActive(true);
        if (_loop == null)
            _loop = StartCoroutine(FadeLoop());
    }

    public void Stop()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        gameObject.SetActive(false);
    }

    IEnumerator FadeLoop()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                var c = icon.color;
                c.a = 1f - t;
                icon.color = c;
                yield return null;
            }

            var reset = icon.color;
            reset.a = 1f;
            icon.color = reset;
        }
    }
}
