using System;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives.Elements;
using NUnit.Framework;

namespace MCV_Module.Tests
{
    /// <summary>
    /// ElementNameMap 完整性测试 —— 保证每个枚举值都有映射、反查不抛异常。
    /// 新增 ElementType / ElementPointNameType 枚举值时若忘记补映射，本测试会失败。
    /// </summary>
    public class ElementNameMapTests
    {
        [Test]
        public void AllElementTypes_HaveMapping()
        {
            foreach (ElementType type in Enum.GetValues(typeof(ElementType)))
            {
                Assert.DoesNotThrow(
                    () => ElementNameMap.GetName(type),
                    $"ElementType.{type} 缺少名称映射，请补入 ElementNameMap.ElementRemap");
            }
        }

        [Test]
        public void AllPointNameTypes_HaveMapping()
        {
            foreach (ElementPointNameType type in Enum.GetValues(typeof(ElementPointNameType)))
            {
                Assert.DoesNotThrow(
                    () => ElementNameMap.GetName(type),
                    $"ElementPointNameType.{type} 缺少名称映射，请补入 ElementNameMap.PointRemap");
            }
        }

        [Test]
        public void ReverseLookup_RoundTrip()
        {
            Assert.AreEqual(ElementType.Resistor, ElementNameMap.GetElementType("R"));
            Assert.AreEqual(ElementType.Power, ElementNameMap.GetElementType("P"));
            Assert.AreEqual(ElementType.Point, ElementNameMap.GetElementType("点"));
            Assert.AreEqual(ElementType.Line, ElementNameMap.GetElementType("线"));

            Assert.AreEqual(ElementPointNameType.Input1, ElementNameMap.GetPointNameType("1L1"));
            Assert.AreEqual(ElementPointNameType.NO_Out_2, ElementNameMap.GetPointNameType("16NO"));
            Assert.AreEqual(ElementPointNameType.NinetySix, ElementNameMap.GetPointNameType("96"));
        }

        [Test]
        public void ReverseLookup_Unknown_ReturnsNone()
        {
            Assert.AreEqual(ElementType.None, ElementNameMap.GetElementType("__不存在__"));
            Assert.AreEqual(ElementPointNameType.None, ElementNameMap.GetPointNameType("__不存在__"));
            Assert.AreEqual(ElementType.None, ElementNameMap.GetElementType(null));
        }
    }
}
