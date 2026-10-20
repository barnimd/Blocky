using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using StepResult = BoardInput.StepResult;

// Aturan path gesture di BoardInput.EvaluateStep,
// termasuk perbaikan bug "rotate terkunci".
public class GesturePathTests
{
    private GameObject gameObject;
    private ActionInventory inventory;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("GesturePathTest");
        inventory = gameObject.AddComponent<ActionInventory>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    private StepResult Evaluate(Vector2Int cell, out ActionType denied, params Vector2Int[] path)
    {
        return BoardInput.EvaluateStep(new List<Vector2Int>(path), cell, inventory, out denied);
    }

    private StepResult Evaluate(Vector2Int cell, params Vector2Int[] path)
    {
        return Evaluate(cell, out _, path);
    }

    private static Vector2Int C(int x, int y) => new Vector2Int(x, y);

    [Test]
    public void SecondCell_AllowedWhenSwapHasTurns()
    {
        inventory.ResetInventory(1, 0, 0, 0);

        Assert.AreEqual(StepResult.Allowed, Evaluate(C(1, 0), C(0, 0)));
    }

    [Test]
    public void RotateLockoutFix_StraightStepAllowedWithOnlyRotateTurns()
    {
        // Horizontal & Vertical habis, Rotate masih ada:
        // awal gerakan L harus tetap bisa ditarik.
        inventory.ResetInventory(0, 0, 0, 1);

        Assert.AreEqual(StepResult.Allowed, Evaluate(C(1, 0), C(0, 0)));
        Assert.AreEqual(StepResult.Allowed, Evaluate(C(0, 1), C(0, 0)));
        Assert.AreEqual(StepResult.Allowed, Evaluate(C(1, 1), C(0, 0), C(1, 0)));
    }

    [Test]
    public void SecondCell_NoTurns_ReportsSwapAction()
    {
        inventory.ResetInventory(0, 0, 0, 0);

        Assert.AreEqual(StepResult.NoTurns, Evaluate(C(1, 0), out ActionType denied, C(0, 0)));
        Assert.AreEqual(ActionType.Horizontal, denied);
    }

    [Test]
    public void DiagonalStep_NotUnlockedByRotate()
    {
        inventory.ResetInventory(0, 0, 0, 1);

        Assert.AreEqual(StepResult.NoTurns, Evaluate(C(1, 1), out ActionType denied, C(0, 0)));
        Assert.AreEqual(ActionType.Diagonal, denied);
    }

    [Test]
    public void ThirdCell_LShapeWithoutRotateTurns_IsDenied()
    {
        inventory.ResetInventory(1, 1, 0, 0);

        Assert.AreEqual(StepResult.NoTurns, Evaluate(C(1, 1), out ActionType denied, C(0, 0), C(1, 0)));
        Assert.AreEqual(ActionType.Rotate, denied);
    }

    [Test]
    public void ThirdCell_StraightLine_IsInvalid()
    {
        inventory.ResetInventory(8, 6, 3, 2);

        Assert.AreEqual(StepResult.Invalid, Evaluate(C(2, 0), C(0, 0), C(1, 0)));
    }

    [Test]
    public void FourthCell_ClosesLoop_OnlyInSameDirection()
    {
        inventory.ResetInventory(8, 6, 3, 2);

        // (0,0) → (1,0) → (1,1) belok kiri (CCW), penutupnya (0,1).
        Assert.AreEqual(StepResult.Allowed, Evaluate(C(0, 1), C(0, 0), C(1, 0), C(1, 1)));
        Assert.AreEqual(StepResult.Invalid, Evaluate(C(2, 1), C(0, 0), C(1, 0), C(1, 1)));
    }

    [Test]
    public void PathLongerThanFour_IsInvalid()
    {
        inventory.ResetInventory(8, 6, 3, 2);

        Assert.AreEqual(StepResult.Invalid, Evaluate(C(0, 2), C(0, 0), C(1, 0), C(1, 1), C(0, 1)));
    }

    [Test]
    public void NonNeighbor_AndRevisit_AreInvalid()
    {
        inventory.ResetInventory(8, 6, 3, 2);

        Assert.AreEqual(StepResult.Invalid, Evaluate(C(2, 0), C(0, 0)));
        Assert.AreEqual(StepResult.Invalid, Evaluate(C(0, 0), C(0, 0), C(1, 0)));
    }
}
