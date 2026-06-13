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
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        foreach (var frame in frames)
        {
            displayImage.sprite = frame;
            yield return new WaitForSeconds(frameDuration);
        }

        SceneManager.LoadScene(returnScene);
    }
}
