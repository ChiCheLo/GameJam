using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip homepageBGM;
    [SerializeField] private AudioClip inGameBGM;

    [Header("SE")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip resetSFX;
    [SerializeField] private AudioClip cannonFireSFX;
    [SerializeField] private AudioClip endingSFX;
    [SerializeField] private AudioClip carHornSFX;

    [Header("Loop SE")]
    [SerializeField] private AudioSource clockSource;
    [SerializeField] private AudioClip clockRingSFX;
    [SerializeField] private AudioSource burnSource;
    [SerializeField] private AudioClip burnSFX;

    void Awake()
    {
        Instance = this;

        string scene = SceneManager.GetActiveScene().name;
        if (scene == "NewGame")
            PlayHomepageBGM();
        else if (scene == "Level")
            PlayInGameBGM();
    }

    // BGM
    public void PlayHomepageBGM() => PlayBGM(homepageBGM);
    public void PlayInGameBGM()   => PlayBGM(inGameBGM);

    void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // 單次 SE
    public void PlayReset()      => sfxSource.PlayOneShot(resetSFX);
    public void PlayCannonFire() => sfxSource.PlayOneShot(cannonFireSFX);
    public void PlayEnding()     => sfxSource.PlayOneShot(endingSFX);
    public void PlayCarHorn()    => sfxSource.PlayOneShot(carHornSFX);

    // 循環 SE — 鬧鐘
    public void PlayClockRing()
    {
        if (clockSource == null || clockSource.isPlaying) return;
        clockSource.clip = clockRingSFX;
        clockSource.loop = true;
        clockSource.Play();
    }

    public void StopClockRing() { if (clockSource != null) clockSource.Stop(); }

    // 循環 SE — 點燃
    public void PlayBurn()
    {
        if (burnSource == null || burnSource.isPlaying) return;
        burnSource.clip = burnSFX;
        burnSource.loop = true;
        burnSource.Play();
    }

    public void StopBurn() { if (burnSource != null) burnSource.Stop(); }
}
