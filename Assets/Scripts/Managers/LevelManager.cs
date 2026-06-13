using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private List<MonoBehaviour> resettables;
    [SerializeField] private int maxAP = 5;
    [SerializeField] private TMP_Text apText;

    [Header("Levels")]
    [SerializeField] private List<LevelArea> levels;
    [SerializeField] private PlayerGridMovement player;
    [SerializeField] private float cameraMoveSpeed = 5f;

    private int _currentAP;
    private int _currentLevelIndex;
    public bool canAction = true;

    void Awake()
    {
        Instance = this;
        _currentAP = maxAP;
        canAction = true;
        PlayerGridMovement.OnActionTaken += OnActionTaken;
        UpdateText();
        SwitchLevel(0);
    }

    void OnDestroy()
    {
        PlayerGridMovement.OnActionTaken -= OnActionTaken;
    }

    void OnActionTaken()
    {
        _currentAP--;
        UpdateText();

        if (_currentAP <= 0)
            canAction = false;
    }

    void UpdateText()
    {
        if (apText != null)
            apText.text = $"AP: {_currentAP} / {maxAP}";
    }

    public void NextLevel()
    {
        SwitchLevel(_currentLevelIndex + 1);
    }

    public void SwitchLevel(int index)
    {
        if (levels == null || index >= levels.Count)
        {
            Debug.Log("[LevelManager] 沒有更多關卡");
            return;
        }

        _currentLevelIndex = index;
        var area = levels[index];

        Debug.Log($"[LevelManager] 切換到關卡 {index} : {area.gameObject.name}");

        maxAP = area.maxAP;

        if (player != null)
            player.SetSpawn(area.playerSpawnPosition, area.playerSpawnRotation);

        // ResetAll();
Instance._currentAP = Instance.maxAP;
        StartCoroutine(MoveCameraTo(area.cameraPosition));
    }

    IEnumerator MoveCameraTo(Vector3 target)
    {
        var cam = Camera.main.transform;
        while (Vector3.Distance(cam.position, target) > 0.01f)
        {
            cam.position = Vector3.MoveTowards(cam.position, target, cameraMoveSpeed * Time.deltaTime);
            yield return null;
        }
        cam.position = target;
    }

    public static void ResetAll()
    {
        if (Instance == null) return;

        Instance.canAction = true;
        Instance._currentAP = Instance.maxAP;
        Instance.UpdateText();

        foreach (var mb in Instance.resettables)
        {
            if (mb is IResettable r)
                r.OnReset();
        }

        if (Instance.levels != null && Instance._currentLevelIndex < Instance.levels.Count)
            Instance.levels[Instance._currentLevelIndex]?.ResetArea();
    }
}
