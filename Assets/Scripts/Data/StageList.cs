using System.Collections.Generic;
using UnityEngine;

// Daftar stage berurutan (Stage 1, 2, 3, ...).
[CreateAssetMenu(fileName = "StageList", menuName = "Blocky/Stage List")]
public class StageList : ScriptableObject
{
    [SerializeField] private List<StageData> stages = new List<StageData>();

    public int Count => stages.Count;

    public StageData GetStage(int index)
    {
        if (index < 0 || index >= stages.Count)
        {
            return null;
        }

        return stages[index];
    }
}
