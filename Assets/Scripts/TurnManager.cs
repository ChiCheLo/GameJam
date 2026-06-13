using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Settings")]
    [Tooltip("NPC 每回合要走的步數（可在 Editor 中設定，預設為 2）")]
    [SerializeField] private int npcStepsPerTurn = 2;

    public event Action<int> OnNpcTakeTurn;

    // 用於鎖定玩家輸入，直到 NPC 移動完畢
    public bool IsNpcMoving { get; set; } = false;

    public int NpcStepsPerTurn => npcStepsPerTurn;

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
        // 觸發 NPC 回合 (如果有 NPC 註冊了事件)
        if (OnNpcTakeTurn != null)
        {
            IsNpcMoving = true;
            OnNpcTakeTurn.Invoke(npcStepsPerTurn);
        }
        else
        {
            IsNpcMoving = false; // 無 NPC，直接放行玩家
        }
    }
}
