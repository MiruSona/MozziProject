using System;
using System.Collections.Generic;
using Mozzi.Data;
using Mozzi.Game.View;
using NUnit.Framework;
using UnityEngine;

namespace Mozzi.Tests
{
    // FarmDecor 표 검사. 구운 표가 울타리 안 칸에 겹침 없이 들어가는지와, 칸 밖 · 겹침을 막는지 본다.
    public sealed class FarmDecorLayoutTests
    {
        [Test]
        public void BakedTable_FitsInsideFenceWithoutOverlap()
        {
            GameDataTables tables = StaticDataManager.LoadBaked();

            Assert.That(tables.FarmDecorRows.Count, Is.GreaterThan(0));
            Assert.DoesNotThrow(() => FarmDecorLayout.Validate(tables.FarmDecorRows, LayoutConst.DecorCols, LayoutConst.DecorRows));
        }

        [Test]
        public void BakedTable_FillsAboutOneFifthOfCells()
        {
            GameDataTables tables = StaticDataManager.LoadBaked();
            float fill = tables.FarmDecorRows.Count / (float)(LayoutConst.DecorCols * LayoutConst.DecorRows);

            Assert.That(fill, Is.InRange(0.15f, 0.25f));
        }

        [Test]
        public void ColumnOutside_Throws()
        {
            List<FarmDecorRow> rows = new List<FarmDecorRow> { Row("a", LayoutConst.DecorCols, 0) };

            Assert.Throws<InvalidOperationException>(() => FarmDecorLayout.Validate(rows, LayoutConst.DecorCols, LayoutConst.DecorRows));
        }

        [Test]
        public void NegativeRow_Throws()
        {
            List<FarmDecorRow> rows = new List<FarmDecorRow> { Row("a", 0, -1) };

            Assert.Throws<InvalidOperationException>(() => FarmDecorLayout.Validate(rows, LayoutConst.DecorCols, LayoutConst.DecorRows));
        }

        [Test]
        public void SameCellTwice_Throws()
        {
            List<FarmDecorRow> rows = new List<FarmDecorRow> { Row("a", 3, 4), Row("b", 3, 4) };

            Assert.Throws<InvalidOperationException>(() => FarmDecorLayout.Validate(rows, LayoutConst.DecorCols, LayoutConst.DecorRows));
        }

        [Test]
        public void FirstCell_SitsOnFarmBottomLeft()
        {
            Rect area = LayoutConst.FarmRect;
            Vector2 pos = FarmDecorLayout.CellBottomCenter(area, 0, 0);

            Assert.That(pos.x, Is.EqualTo(area.xMin + 0.5f).Within(0.0001f));
            Assert.That(pos.y, Is.EqualTo(area.yMin).Within(0.0001f));
        }

        private static FarmDecorRow Row(string id, int col, int row)
        {
            return new FarmDecorRow { Id = id, SpriteKey = "Farm/decor_00", Col = col, Row = row };
        }
    }
}
