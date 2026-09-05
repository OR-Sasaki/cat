#nullable enable

using NUnit.Framework;

namespace Home.GridPreviewLogic.Tests
{
    public class FootprintEvaluatorTests
    {
        [Test]
        [Description("全セル範囲内・未占有なら設置可能で、全セルが可視となる")]
        public void Evaluate_AllInRangeAndUnoccupied_CanPlaceAndAllCellsVisible()
        {
            var result = FootprintEvaluator.Evaluate(
                new GridCell(0, 0),
                new GridCell(2, 2),
                selfUserFurnitureId: 1,
                isInRange: _ => true,
                occupantIdOf: _ => 0);

            Assert.IsTrue(result.CanPlace);
            CollectionAssert.AreEqual(
                new[] { new GridCell(0, 0), new GridCell(0, 1), new GridCell(1, 0), new GridCell(1, 1) },
                result.VisibleCells);
        }

        [Test]
        [Description("1セルでも他家具に占有されていたら設置不可だが、全セルは可視のまま")]
        public void Evaluate_OneCellOccupiedByOther_CannotPlaceButAllCellsVisible()
        {
            var result = FootprintEvaluator.Evaluate(
                new GridCell(0, 0),
                new GridCell(2, 1),
                selfUserFurnitureId: 1,
                isInRange: _ => true,
                occupantIdOf: cell => cell == new GridCell(1, 0) ? 99 : 0);

            Assert.IsFalse(result.CanPlace);
            Assert.AreEqual(2, result.VisibleCells.Count);
        }

        [Test]
        [Description("一部が範囲外なら設置不可で、範囲内セルのみ可視となる")]
        public void Evaluate_PartiallyOutOfRange_CannotPlaceAndOnlyInRangeCellsVisible()
        {
            var result = FootprintEvaluator.Evaluate(
                new GridCell(0, 0),
                new GridCell(2, 1),
                selfUserFurnitureId: 1,
                isInRange: cell => cell != new GridCell(1, 0),
                occupantIdOf: _ => 0);

            Assert.IsFalse(result.CanPlace);
            CollectionAssert.AreEqual(new[] { new GridCell(0, 0) }, result.VisibleCells);
        }

        [Test]
        [Description("全域が範囲外なら設置不可で、可視セルは空となる")]
        public void Evaluate_AllOutOfRange_CannotPlaceAndNoVisibleCells()
        {
            var result = FootprintEvaluator.Evaluate(
                new GridCell(0, 0),
                new GridCell(2, 2),
                selfUserFurnitureId: 1,
                isInRange: _ => false,
                occupantIdOf: _ => 0);

            Assert.IsFalse(result.CanPlace);
            Assert.IsEmpty(result.VisibleCells);
        }

        [Test]
        [Description("占有idが自分自身のidなら設置可能")]
        public void Evaluate_OccupiedBySelf_CanPlace()
        {
            var result = FootprintEvaluator.Evaluate(
                new GridCell(0, 0),
                new GridCell(1, 1),
                selfUserFurnitureId: 5,
                isInRange: _ => true,
                occupantIdOf: _ => 5);

            Assert.IsTrue(result.CanPlace);
        }
    }
}
