using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

// Memastikan stage asset yang dibuat setup script valid.
public class StageAssetTests
{
    private const string StageListPath = "Assets/Data/StageList.asset";
    private const string InfiniteStagePath = "Assets/Data/InfiniteMode.asset";

    private static StageList LoadList()
    {
        StageList list = AssetDatabase.LoadAssetAtPath<StageList>(StageListPath);
        Assert.IsNotNull(list, $"{StageListPath} tidak ditemukan. Jalankan Blocky > Setup All Scenes.");
        return list;
    }

    [Test]
    public void StageList_Has5ChaptersOf12Stages()
    {
        StageList list = LoadList();

        Assert.AreEqual(60, list.Count);
        Assert.AreEqual(5, list.ChapterCount);

        for (int chapter = 0; chapter < list.ChapterCount; chapter++)
        {
            Assert.IsNotEmpty(list.GetChapterName(chapter), $"Bab {chapter + 1} belum punya nama.");
        }
    }

    [Test]
    public void StageAssets_MatchGenerator()
    {
        StageList list = LoadList();
        List<StageSpec> specs = StageGenerator.Generate(5, StageList.StagesPerChapter, StageGenerator.DefaultSeed);

        for (int i = 0; i < list.Count; i++)
        {
            StageData stage = list.GetStage(i);

            Assert.AreEqual(specs[i].TotalTurns, stage.TotalTurns, $"Stage {stage.StageId}");
            Assert.AreEqual(specs[i].Goals.Count, stage.Goals.Count, $"Stage {stage.StageId}");
            Assert.AreEqual(specs[i].Star3MinTurnsLeft, stage.Star3MinTurnsLeft, $"Stage {stage.StageId}");
        }
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

    [Test]
    public void InfiniteStage_HasBoardAndTurns_NoGoals()
    {
        StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(InfiniteStagePath);

        Assert.IsNotNull(stage, $"{InfiniteStagePath} tidak ditemukan.");
        Assert.AreEqual(0, stage.Goals.Count);
        Assert.Greater(stage.TotalTurns, 0);
    }
}
