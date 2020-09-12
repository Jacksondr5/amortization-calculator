using System;
using System.Runtime.CompilerServices;
using NodaTime;
[assembly: InternalsVisibleTo("AmortizationCalculator.Test")]

namespace AmortizationCalculator
{
    internal static class TermCalculator
    {
        internal static decimal CalculateTerm(
            LocalDate startDate,
            LocalDate endDate,
            AccrualBasis accrualBasis
        )
        {
            var years =
                Period.Between(startDate, endDate, PeriodUnits.Years).Years;
            var startPlusYears = startDate.PlusYears(years);
            decimal days =
                Period.Between(startPlusYears, endDate, PeriodUnits.Days).Days;
            var remainingYearPart = accrualBasis switch
            {
                AccrualBasis.Actual360 => days / 360,
                AccrualBasis.Actual365 => days / 365,
                AccrualBasis.ActualActual =>
                    days / CalendarSystem.Gregorian.GetDaysInYear(endDate.Year),
                AccrualBasis.Thirty360 =>
                    GetDaysForThirty360(startPlusYears, endDate) / 360,
                _ => throw new InvalidOperationException("shouldn't happen")
            };
            return years + remainingYearPart;
        }

        private static decimal GetDaysForThirty360(
            LocalDate startDate,
            LocalDate endDate
        )
        {
            var months =
                Period.Between(startDate, endDate, PeriodUnits.Months).Months;
            var startPlusMonths = startDate.PlusMonths(months);
            var days =
                Period.Between(startPlusMonths, endDate, PeriodUnits.Days).Days;
            var retVal = months * 30 + (days > 30 ? 30 : days);
            return retVal;
        }
    }
}