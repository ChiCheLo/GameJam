using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Round & Turn Settings")]
    [SerializeField] private int maxStepsPerRound = 10;
    [SerializeField] private int chaseThresholdStep = 5;

    public enum NpcMode { Normal, Chase }
    
    // UI 或其他腳本可以監聽這些事件
    public event Action<NpcMode> OnModeChanged;
    public event Action<int> OnNpcTakeTurn;

    private NpcMode _currentMode = NpcMode.Normal;
    private int _playerStepsInRound = 0;
    
    // 用於鎖定玩家輸入，直到 NPC 移動完畢
    public bool IsNpcMoving { get; set; } = false;

    public NpcMode CurrentMode => _currentMode;
    public int PlayerStepsInRound => _playerStepsInRound;
    public int MaxStepsPerRound => maxStepsPerRound;
    public int ChaseThresholdStep => chaseThresholdStep;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        PlayerGridMovement.OnActionTaken += HandlePlayerAction;
    }

    private void OnDisable()
    {
        PlayerGridMovement.OnActionTaken -= HandlePlayerAction;
    }

    private void HandlePlayerAction()
    {
        _playerStepsInRound++;
        Debug.Log($"[TurnManager] 玩家第 {_playerStepsInRound} 步。");

        // 1. 檢查是否達到追逐模式門檻
        if (_playerStepsInRound >= chaseThresholdStep && _currentMode == NpcMode.Normal)
        {
            _currentMode = NpcMode.Chase;
            OnModeChanged?.Invoke(_currentMode);
            Debug.Log("[TurnManager] NPC 進入 Chase Mode (玩家移動 1 步，NPC 移動 2 步)");
        }

        // 2. 檢查是否超過回合最大步數
        if (_playerStepsInRound > maxStepsPerRound)
        {
            _playerStepsInRound = 1; // 重置回合，玩家這次移動算作新回合的第一步
            _currentMode = NpcMode.Normal;
            OnModeChanged?.Invoke(_currentMode);
            Debug.Log("[TurnManager] 回合重置，NPC 回到 Normal Mode (玩家移動 1 步，NPC 移動 1 步)");
        }

        // 3. 觸發 NPC 回合
        int npcSteps = (_currentMode == NpcMode.Chase) ? 2 : 1;
        IsNpcMoving = true;
        OnNpcTakeTurn?.Invoke(npcSteps);
    }
}
