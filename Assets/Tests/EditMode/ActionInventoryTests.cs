using NUnit.Framework;
using UnityEngine;

public class ActionInventoryTests
{
    private GameObject gameObject;
    private ActionInventory inventory;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("ActionInventoryTest");
        inventory = gameObject.AddComponent<ActionInventory>();
        inventory.ResetInventory(8, 6, 3, 2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void TotalTurns_IsSumOfAllActions()
    {
        Assert.AreEqual(19, inventory.TotalTurns);

        inventory.TryUseTurn(ActionType.Diagonal);

        Assert.AreEqual(18, inventory.TotalTurns);
        Assert.AreEqual(2, inventory.DiagonalTurns);
    }

    [Test]
    public void TryUseTurn_FailsWhenEmpty()
    {
        inventory.ResetInventory(0, 1, 1, 1);

        Assert.IsFalse(inventory.TryUseTurn(ActionType.Horizontal));
        Assert.AreEqual(3, inventory.TotalTurns);
    }

    [Test]
    public void ResetInventory_ClampsNegativeValues()
    {
        inventory.ResetInventory(-5, 2, 0, 1);

        Assert.AreEqual(0, inventory.HorizontalTurns);
        Assert.AreEqual(3, inventory.TotalTurns);
        Assert.IsFalse(inventory.AreAllTurnsEmpty());
    }

    [Test]
    public void ReportDenied_FiresTurnDenied()
    {
        ActionType? denied = null;
        inventory.TurnDenied += action => denied = action;

        inventory.ReportDenied(ActionType.Rotate);

        Assert.AreEqual(ActionType.Rotate, denied);
    }
}
