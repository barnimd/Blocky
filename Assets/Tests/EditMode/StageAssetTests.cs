using NUnit.Framework;
using UnityEditor;

// Memastikan stage asset yang dibuat setup script valid.
public class StageAssetTests
{
    private const string StageListPath = "Assets/Data/StageList.asset";

    private static StageList LoadList()
    {
        StageList list = AssetDatabase.LoadAssetAtPath<StageList>(StageListPath);
        Assert.IsNotNull(list, $"{StageListPath} tidak ditemukan. Jalankan Blocky > Setup Game Scene.");
        return list;
    }

    [Test]
    public void StageList_HasStages()
    {
        Assert.GreaterOrEqual(LoadList().Count, 1);
    }

    [Test]
    public void Stage1_MatchesGddExample()
    {
        StageData stage = LoadList().GetStage(0);

        Assert.AreEqual(1, stage.StageId);
        Assert.AreEqual(8, stage.Width);
        Assert.AreEqual(8, stage.Height);
        Assert.AreEqual(2, stage.Goals.Count);
        Assert.AreEqual(BlockColor.Blue, stage.Goals[0].Color);
        Assert.AreEqual(15, stage.Goals[0].Amount);
        Assert.AreEqual(BlockColor.Red, stage.Goals[1].Color);
        Assert.AreEqual(10, stage.Goals[1].Amount);
        Assert.AreEqual(8, stage.HorizontalTurns);
        Assert.AreEqual(6, stage.VerticalTurns);
        Assert.AreEqual(3, stage.DiagonalTurns);
        Assert.AreEqual(2, stage.RotateTurns);
        Assert.AreEqual(19, stage.TotalTurns);
        Assert.AreEqual(5, stage.Star3MinTurnsLeft);
        Assert.AreEqual(2, stage.Star2MinTurnsLeft);
    }

    [Test]
    public void AllStages_AreValid()
    {
        StageList list = LoadList();

        for (int i = 0; i < list.Count; i++)
        {
            StageData stage = list.GetStage(i);

            Assert.IsNotNull(stage, $"Stage index {i} kosong.");
            Assert.AreEqual(i + 1, stage.StageId, "StageId harus berurutan.");
            Assert.That(stage.Width, Is.InRange(3, 9));
            Assert.That(stage.Height, Is.InRange(3, 9));
            Assert.Greater(stage.Goals.Count, 0, $"Stage {stage.StageId} tidak punya target.");
            Assert.Greater(stage.TotalTurns, 0);
            Assert.LessOrEqual(stage.Star2MinTurnsLeft, stage.Star3MinTurnsLeft);
            Assert.Less(stage.Star3MinTurnsLeft, stage.TotalTurns);
        }
    }
}
