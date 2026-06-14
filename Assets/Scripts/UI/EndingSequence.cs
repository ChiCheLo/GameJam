using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingSequence : MonoBehaviour
{
    [SerializeField] private Image displayImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameDuration = 0.5f;
    [SerializeField] private string returnScene = "NewGame";

    void Awake()
    {
        if (displayImage != null)
            displayImage.gameObject.SetActive(false);
    }

    public void Play()
    {
        if (displayImage != null)
            displayImage.gameObject.SetActive(true);
        AudioManager.Instance?.PlayEnding();
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            displayImage.sprite = frames[i];
            if (i == frames.Length - 1)
                AudioManager.Instance?.PlayCarHorn();
            yield return new WaitForSeconds(frameDuration);
        }

        SceneManager.LoadScene(returnScene);
    }
}
