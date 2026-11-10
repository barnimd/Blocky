using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class StageProgressTests
{
    private const string TestPrefix = "blocky.test.";
    private StageList list;

    [SetUp]
    public void SetUp()
    {
        StageProgress.KeyPrefix = TestPrefix;
        StageProgress.ClearAll(10);

        // StageList sementara berisi 3 stage (id 1-3).
        list = ScriptableObject.CreateInstance<StageList>();
        SerializedObject so = new SerializedObject(list);
        SerializedProperty stages = so.FindProperty("stages");
        stages.arraySize = 3;

        for (int i = 0; i < 3; i++)
        {
            StageData stage = ScriptableObject.CreateInstance<StageData>();
            SerializedObject stageSo = new SerializedObject(stage);
            stageSo.FindProperty("stageId").intValue = i + 1;
            stageSo.ApplyModifiedPropertiesWithoutUndo();

            stages.GetArrayElementAtIndex(i).objectReferenceValue = stage;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [TearDown]
    public void TearDown()
    {
        StageProgress.ClearAll(10);
        StageProgress.KeyPrefix = "blocky.";
    }

    [Test]
    public void OnlyFirstStage_IsUnlocked_AtStart()
    {
        Assert.IsTrue(StageProgress.IsUnlocked(list, 0));
        Assert.IsFalse(StageProgress.IsUnlocked(list, 1));
        Assert.IsFalse(StageProgress.IsUnlocked(list, 2));
    }

    [Test]
    public void WinningStage_UnlocksNextStage()
    {
        StageProgress.SaveStars(1, 1);

        Assert.IsTrue(StageProgress.IsUnlocked(list, 1));
        Assert.IsFalse(StageProgress.IsUnlocked(list, 2));
    }

    [Test]
    public void SaveStars_KeepsBestResult()
    {
        Assert.IsTrue(StageProgress.SaveStars(1, 2));
        Assert.IsFalse(StageProgress.SaveStars(1, 1), "Hasil lebih buruk tidak menimpa.");
        Assert.AreEqual(2, StageProgress.GetStars(1));

        Assert.IsTrue(StageProgress.SaveStars(1, 3));
        Assert.AreEqual(3, StageProgress.GetStars(1));
    }

    [Test]
    public void TotalStars_SumsRange()
    {
        StageProgress.SaveStars(1, 3);
        StageProgress.SaveStars(2, 2);

        Assert.AreEqual(5, StageProgress.GetTotalStars(list, 0, 3));
        Assert.AreEqual(2, StageProgress.GetTotalStars(list, 1, 2));
    }

    [Test]
    public void InfiniteBest_OnlyGoesUp()
    {
        Assert.IsTrue(StageProgress.SaveInfiniteBest(500));
        Assert.IsFalse(StageProgress.SaveInfiniteBest(300));
        Assert.AreEqual(500, StageProgress.InfiniteBest);
    }
}
