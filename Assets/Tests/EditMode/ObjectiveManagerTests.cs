using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ObjectiveManagerTests
{
    private GameObject gameObject;
    private ObjectiveManager objectives;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("ObjectiveManagerTest");
        objectives = gameObject.AddComponent<ObjectiveManager>();
        objectives.Setup(new[]
        {
            new ColorGoal(BlockColor.Blue, 15),
            new ColorGoal(BlockColor.Red, 10)
        });
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    private static int[] Colors(params BlockColor[] colors)
    {
        int[] indices = new int[colors.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            indices[i] = (int)colors[i];
        }

        return indices;
    }

    [Test]
    public void Setup_CreatesGoalsFromStage()
    {
        Assert.AreEqual(2, objectives.Goals.Count);
        Assert.AreEqual(BlockColor.Blue, objectives.Goals[0].Color);
        Assert.AreEqual(15, objectives.Goals[0].Remaining);
        Assert.AreEqual(10, objectives.Goals[1].Remaining);
        Assert.IsFalse(objectives.IsComplete);
    }

    [Test]
    public void RegisterCleared_ReducesMatchingColorsOnly()
    {
        List<GoalProgress> changed = new List<GoalProgress>();
        objectives.GoalProgressChanged += changed.Add;

        objectives.RegisterCleared(Colors(
            BlockColor.Blue, BlockColor.Blue, BlockColor.Blue,
            BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red,
            BlockColor.Green, BlockColor.Green, BlockColor.Green));

        Assert.AreEqual(12, objectives.Goals[0].Remaining);
        Assert.AreEqual(6, objectives.Goals[1].Remaining);
        Assert.AreEqual(2, changed.Count, "Hijau bukan target, jadi tidak memicu event.");
    }

    [Test]
    public void RegisterCleared_ClampsAtZero_AndCompletesWhenAllGoalsDone()
    {
        List<int> blueAndRed = new List<int>();

        for (int i = 0; i < 20; i++)
        {
            blueAndRed.Add((int)BlockColor.Blue);
        }

        objectives.RegisterCleared(blueAndRed);

        Assert.AreEqual(0, objectives.Goals[0].Remaining);
        Assert.IsFalse(objectives.IsComplete, "Merah belum selesai.");

        objectives.RegisterCleared(Colors(
            BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red,
            BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red));

        Assert.AreEqual(0, objectives.Goals[1].Remaining);
        Assert.IsTrue(objectives.IsComplete);
    }

    [Test]
    public void CompletedGoal_DoesNotFireEventAgain()
    {
        objectives.Setup(new[] { new ColorGoal(BlockColor.Yellow, 3) });
        objectives.RegisterCleared(Colors(BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow));

        int events = 0;
        objectives.GoalProgressChanged += _ => events++;
        objectives.RegisterCleared(Colors(BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow));

        Assert.AreEqual(0, events);
        Assert.IsTrue(objectives.IsComplete);
    }

    [Test]
    public void Setup_ResetsProgress_AndFiresGoalsReset()
    {
        objectives.RegisterCleared(Colors(BlockColor.Blue, BlockColor.Blue, BlockColor.Blue));

        bool reset = false;
        objectives.GoalsReset += () => reset = true;
        objectives.Setup(new[] { new ColorGoal(BlockColor.Blue, 15) });

        Assert.IsTrue(reset);
        Assert.AreEqual(1, objectives.Goals.Count);
        Assert.AreEqual(15, objectives.Goals[0].Remaining);
    }

    [Test]
    public void EmptyGoals_IsNeverComplete()
    {
        LogAssert.Expect(LogType.Error, "ObjectiveManager: Stage tidak punya target.");
        objectives.Setup(new ColorGoal[0]);

        Assert.IsFalse(objectives.IsComplete);
    }
}
