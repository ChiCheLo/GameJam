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
        foreach (var mb in resettables)
        {
            if (mb is IResettable r)
                r.OnReset();
        }
    }
}
