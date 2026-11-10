using System.Collections.Generic;
using UnityEngine;

// Daftar stage berurutan (Stage 1, 2, 3, ...), dikelompokkan per bab.
// Bab 1 = stage 1-12, Bab 2 = stage 13-24, dan seterusnya.
[CreateAssetMenu(fileName = "StageList", menuName = "Blocky/Stage List")]
public class StageList : ScriptableObject
{
    // Satu layar level selector = satu bab = 12 stage.
    public const int StagesPerChapter = 12;

    [SerializeField] private List<StageData> stages = new List<StageData>();
    [SerializeField] private List<string> chapterNames = new List<string>();

    public int Count => stages.Count;

    public int ChapterCount =>
        Mathf.CeilToInt(stages.Count / (float)StagesPerChapter);

    public StageData GetStage(int index)
    {
        if (index < 0 || index >= stages.Count)
        {
            return null;
        }

        return stages[index];
    }

    public string GetChapterName(int chapter)
    {
        if (chapter < 0 || chapter >= chapterNames.Count)
        {
            return string.Empty;
        }

        return chapterNames[chapter];
    }

    // Index stage (0-based) → bab (0-based).
    public static int GetChapterOf(int stageIndex)
    {
        return Mathf.Max(0, stageIndex) / StagesPerChapter;
    }
}
