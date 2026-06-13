using System.Collections.Generic;
using UnityEngine;

public class LevelArea : MonoBehaviour
{
    public Vector3 playerSpawnPosition;
    public Vector3 playerSpawnRotation;
    public Vector3 cameraPosition;
    public int maxAP = 5;

    [SerializeField] private List<MonoBehaviour> resettables;

    public void ResetArea()
    {
        // 1. 重置手動指派的 resettables
        foreach (var mb in resettables)
        {
            if (mb is IResettable r)
                r.OnReset();
        }

        // 2. 自動尋找目前關卡區域下的所有 IResettable 元件（例如 NPC、鬧鐘），防止漏拉 Inspector
        IResettable[] childResettables = GetComponentsInChildren<IResettable>(true);
        foreach (var r in childResettables)
        {
            r.OnReset();
        }
    }
}
