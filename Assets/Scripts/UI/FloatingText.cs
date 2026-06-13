using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string text = "鈴鈴鈴";
    [SerializeField] private float duration = 1.2f;

    private Coroutine _loop;

    void Awake()
    {
        if (label != null) label.text = text;
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
                var c = label.color;
                c.a = 1f - t;
                label.color = c;
                yield return null;
            }

            var reset = label.color;
            reset.a = 1f;
            label.color = reset;
        }
    }
}
