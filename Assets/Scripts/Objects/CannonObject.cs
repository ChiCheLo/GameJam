using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CannonObject : KeepableBase, IInteractable
{
    [SerializeField] private GameObject fireTarget;
    [SerializeField] private GameObject countdownDisplay;
    [SerializeField] private Image countdownImage;
    [SerializeField] private Sprite[] countdownSprites;  // [0]=剩兩步, [1]=剩一步
    [SerializeField] private Sprite interactSprite;

    public Sprite InteractSprite => interactSprite;

    private int  _countdown;
    private bool _isFired;
    private bool _justIgnited;
    private bool _isSubscribed;

    private int  _snapshotCountdown;
    private bool _snapshotIsFired;

    public void Interact()
    {
        if (_countdown > 0 || _isFired) return;
        _countdown    = 2;
        _justIgnited  = true;
        Subscribe();
        UpdateCountdownUI();
        UpdateBurnAudio();
    }

    void OnActionTaken()
    {
        if (_justIgnited)
        {
            _justIgnited = false;
            return;
        }

        if (_isFired)
        {
            fireTarget?.SetActive(false);
            _isFired = false;
            Unsubscribe();
            UpdateCountdownUI();
            return;
        }

        if (_countdown <= 0) return;

        _countdown--;
        UpdateCountdownUI();

        if (_countdown == 0)
        {
            _isFired = true;
            StartCoroutine(FireAfterDelay());
        }
    }

    IEnumerator FireAfterDelay()
    {
        UpdateBurnAudio();
        AudioManager.Instance?.PlayCannonFire();
        yield return new WaitForSeconds(0.2f);
        fireTarget?.SetActive(true);
    }

    void UpdateBurnAudio()
    {
        Debug.Log($"[CannonObject] UpdateBurnAudio | _countdown={_countdown} | Instance={AudioManager.Instance}");
        if (_countdown > 0)
            AudioManager.Instance?.PlayBurn();
        else
            AudioManager.Instance?.StopBurn();
    }

    void UpdateCountdownUI()
    {
        if (countdownDisplay == null) return;

        bool active = _countdown > 0;
        countdownDisplay.SetActive(active);

        if (active && countdownImage != null && countdownSprites != null)
        {
            int index = _countdown == 1 ? 1 : 0;
            if (index < countdownSprites.Length)
                countdownImage.sprite = countdownSprites[index];
        }
    }

    void Subscribe()
    {
        if (_isSubscribed) return;
        PlayerGridMovement.OnActionTaken += OnActionTaken;
        _isSubscribed = true;
    }

    void Unsubscribe()
    {
        if (!_isSubscribed) return;
        PlayerGridMovement.OnActionTaken -= OnActionTaken;
        _isSubscribed = false;
    }

    void OnDestroy() => Unsubscribe();

    protected override void SaveSnapshot()
    {
        _snapshotCountdown = _countdown;
        _snapshotIsFired   = _isFired;
    }

    protected override void RestoreSnapshot()
    {
        Unsubscribe();
        _justIgnited = false;
        _countdown   = _snapshotCountdown;
        _isFired     = _snapshotIsFired;

        fireTarget?.SetActive(_isFired);

        if (_countdown > 0 || _isFired)
            Subscribe();

        UpdateBurnAudio();
        UpdateCountdownUI();
    }

    protected override void OnResetInternal()
    {
        Unsubscribe();
        _justIgnited = false;
        _countdown   = 0;
        _isFired     = false;
        fireTarget?.SetActive(false);
        UpdateBurnAudio();
        UpdateCountdownUI();
    }
}
