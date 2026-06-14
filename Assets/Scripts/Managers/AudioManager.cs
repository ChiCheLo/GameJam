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
        Debug.Log($"[AudioManager] BGM 開始: {clip?.name}");
    }

    // 單次 SE
    public void PlayReset()
    {
        Debug.Log("[AudioManager] SE: Reset");
        sfxSource.PlayOneShot(resetSFX);
    }

    public void PlayCannonFire()
    {
        Debug.Log("[AudioManager] SE: CannonFire");
        sfxSource.PlayOneShot(cannonFireSFX);
    }

    public void PlayEnding()
    {
        Debug.Log("[AudioManager] SE: Ending");
        sfxSource.PlayOneShot(endingSFX);
    }

    public void PlayCarHorn()
    {
        Debug.Log("[AudioManager] SE: CarHorn");
        sfxSource.PlayOneShot(carHornSFX);
    }

    // 循環 SE — 鬧鐘
    public void PlayClockRing()
    {
        if (clockSource == null || clockSource.isPlaying) return;
        clockSource.clip = clockRingSFX;
        clockSource.loop = true;
        clockSource.Play();
        Debug.Log("[AudioManager] Loop 開始: ClockRing");
    }

    public void StopClockRing()
    {
        if (clockSource != null) clockSource.Stop();
        Debug.Log($"[AudioManager] Loop 結束: ClockRing\n{System.Environment.StackTrace}");
    }

    // 循環 SE — 點燃
    public void PlayBurn()
    {
        Debug.Log($"[AudioManager] PlayBurn 被呼叫 | burnSource={burnSource} | burnSFX={burnSFX} | isPlaying={burnSource?.isPlaying}");
        if (burnSource == null || burnSource.isPlaying) return;
        burnSource.clip = burnSFX;
        burnSource.loop = true;
        burnSource.Play();
        Debug.Log("[AudioManager] Loop 開始: Burn");
    }

    public void StopBurn()
    {
        Debug.Log($"[AudioManager] StopBurn 被呼叫 | burnSource={burnSource}");
        if (burnSource != null) burnSource.Stop();
        Debug.Log("[AudioManager] Loop 結束: Burn");
    }
}
