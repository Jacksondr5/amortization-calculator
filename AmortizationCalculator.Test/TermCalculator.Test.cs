using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;

namespace AmortizationCalculator.Test
{
    [TestClass]
    public class TermCalculatorTest
    {

        public class AccrualBasisDataSourceAttribute : Attribute, ITestDataSource
        {
            public IEnumerable<object[]> GetData(MethodInfo methodInfo) =>
                Enum
                    .GetValues(typeof(AccrualBasis))
                    .Cast<AccrualBasis>()
                    .SelectMany(
                        x => new bool[] { true, false },
                        (accrualBasis, isLeapYear) =>
                            new object[] { accrualBasis, isLeapYear }
                    );

            public string GetDisplayName(MethodInfo methodInfo, object[] data)
            {
                var enumName = Enum.GetName(typeof(AccrualBasis), data[0]);
                var leapYearLabel =
                    ((bool)data[1] ? "Leap Year" : "Non-Leap Year");
                return $"{methodInfo.Name} - {enumName} - {leapYearLabel}";
            }

        }

        private static int GetYear(bool isLeapYear) => isLeapYear ? 2000 : 2001;

        private static int GetDaysInYear(AccrualBasis accrualBasis, int year) =>
            accrualBasis switch
            {
                AccrualBasis.Actual360 => 360,
                AccrualBasis.Actual365 => 365,
                AccrualBasis.ActualActual =>
                    CalendarSystem.Gregorian.GetDaysInYear(year),
                AccrualBasis.Thirty360 => 360,
                _ => throw new InvalidOperationException()
            };

        private void RunTest(
            decimal expected,
            LocalDate startDate,
            LocalDate endDate,
            AccrualBasis accrualBasis
        )
        {
            var actual =
                TermCalculator.CalculateTerm(startDate, endDate, accrualBasis);
            actual.Should().Be(expected);
        }

        [DataTestMethod]
        [AccrualBasisDataSource]
        public void CalaulateTerm_SingleYear(
            AccrualBasis accrualBasis,
            bool isLeapYear
        )
        {
            //This test doesn't actually care if it's a leap year or not
            var year = GetYear(isLeapYear);
            RunTest(
                1,
                new LocalDate(year, 1, 1),
                new LocalDate(year + 1, 1, 1),
                accrualBasis
            );
        }

        [DataTestMethod]
        [AccrualBasisDataSource]
        public void CalaulateTerm_31DayMonth(
            AccrualBasis accrualBasis,
            bool isLeapYear
        )
        {
            var year = GetYear(isLeapYear);
            decimal expectedDays =
                accrualBasis == AccrualBasis.Thirty360 ? 30 : 31;
            RunTest(
                expectedDays / GetDaysInYear(accrualBasis, year),
                new LocalDate(year, 1, 1),
                new LocalDate(year, 2, 1),
                accrualBasis
            );
        }

        [DataTestMethod]
        [AccrualBasisDataSource]
        public void CalaulateTerm_30DayMonth(
            AccrualBasis accrualBasis,
            bool isLeapYear
        )
        {
            var year = GetYear(isLeapYear);
            RunTest(
                30m / GetDaysInYear(accrualBasis, year),
                new LocalDate(year, 4, 1),
                new LocalDate(year, 5, 1),
                accrualBasis
            );
        }

        [DataTestMethod]
        [AccrualBasisDataSource]
        public void CalaulateTerm_February(
            AccrualBasis accrualBasis,
            bool isLeapYear
        )
        {
            var year = GetYear(isLeapYear);
            decimal expectedDays;
            if (accrualBasis == AccrualBasis.Thirty360)
                expectedDays = 30;
            else
                expectedDays = isLeapYear ? 29 : 28;
            RunTest(
                expectedDays / GetDaysInYear(accrualBasis, year),
                new LocalDate(year, 2, 1),
                new LocalDate(year, 3, 1),
                accrualBasis
            );
        }

        [DataTestMethod]
        [AccrualBasisDataSource]
        public void CalaulateTerm_SingleDay(
            AccrualBasis accrualBasis,
            bool isLeapYear
        )
        {
            var year = GetYear(isLeapYear);
            RunTest(
                1m / GetDaysInYear(accrualBasis, year),
                new LocalDate(year, 1, 1),
                new LocalDate(year, 1, 2),
                accrualBasis
            );
        }

        [DataTestMethod]
        [DataRow(1900)] //Not leap year
        [DataRow(2000)] //Leap year
        [DataRow(2001)] //Not leap year
        [DataRow(2004)] //Leap year
        public void CalculateTerm_TestLeapYear(int year)
        {
            bool isLeapYear;
            switch (year)
            {
                case 2000:
                case 2004:
                    isLeapYear = true;
                    break;
                case 1900:
                case 2001:
                    isLeapYear = false;
                    break;
                default:
                    throw new ArgumentException(
                        "Unexpected argument",
                        nameof(year)
                    );
            }
            RunTest(
                1m / (isLeapYear ? 366 : 365),
                new LocalDate(year, 1, 1),
                new LocalDate(year, 1, 2),
                AccrualBasis.ActualActual
            );
        }
    }
}