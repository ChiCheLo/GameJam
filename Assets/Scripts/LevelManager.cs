using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private List<MonoBehaviour> resettables;
    [SerializeField] private int maxAP = 5;
    [SerializeField] private TMP_Text apText;

    private int _currentAP;
    public bool canAction = true;

    void Awake()
    {
        Instance = this;
        _currentAP = maxAP;
        canAction = true;
        PlayerGridMovement.OnActionTaken += OnActionTaken;
        UpdateText();
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
        {
            canAction = false;
        }
    }

    void UpdateText()
    {
        if (apText != null)
            apText.text = $"AP: {_currentAP} / {maxAP}";
    }

    [Button]
    public static void ResetAll()
    {
        if(Instance == null) return;
        
        Instance.canAction = true;
        Instance._currentAP = Instance.maxAP;
        Instance.UpdateText();

        foreach (var mb in Instance.resettables)
        {
            if (mb is IResettable r)
                r.OnReset();
        }
    }
}