using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace InsuranceEngine.Tests
{
    [TestFixture]
    public class ComprehensiveUnitTests
    {
        [Test]
        public void BasicArithmeticTests()
        {
            Assert.AreEqual(4, Evaluate("2 + 2"));
            Assert.AreEqual(0, Evaluate("2 - 2"));
            Assert.AreEqual(6, Evaluate("2 * 3"));
            Assert.AreEqual(2, Evaluate("6 / 3"));
        }

        [Test]
        public void MaxMinFunctionsTests()
        {
            Assert.AreEqual(5, Evaluate("MAX(3, 5)"));
            Assert.AreEqual(3, Evaluate("MIN(3, 5)"));
        }

        [Test]
        public void IfConditionTests()
        {
            Assert.AreEqual("Yes", Evaluate("IF(1=1, 'Yes', 'No')"));
            Assert.AreEqual("No", Evaluate("IF(1=0, 'Yes', 'No')"));
        }

        [Test]
        public void CustomFunctionTests()
        {
            Assert.AreEqual(2, Evaluate("DATEDIFF('2026-02-24', '2026-02-22', 'DAYS')"));
            Assert.AreEqual(1, Evaluate("MONTHDIFF('2026-01-01', '2026-02-01')"));
        }

        [Test]
        public void LogicEvaluationTests()
        {
            Assert.IsTrue(Evaluate("TRUE AND TRUE"));
            Assert.IsFalse(Evaluate("TRUE AND FALSE"));
            Assert.IsTrue(Evaluate("FALSE OR TRUE"));
        }

        [Test]
        public void BetweenOperatorTests()
        {
            Assert.IsTrue(Evaluate("5 BETWEEN 1 AND 10"));
            Assert.IsFalse(Evaluate("15 BETWEEN 1 AND 10"));
        }

        // Add more tests for nested conditions, fund value projections, mortality charges, discontinuance charges, IRR calculations, and edge cases.

        private object Evaluate(string formula)
        {
            // Dummy implementation of formula evaluation
            return null;
        }
    }
}