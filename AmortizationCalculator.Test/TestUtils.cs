using System;

namespace AmortizationCalculator.Test
{
    public static class TestUtils
    {
        private const int ROUNDING_DECIMAL_PLACES = 5;
        public static decimal RoundDecimal(decimal value) =>
            Math.Round(value, ROUNDING_DECIMAL_PLACES);
    }
}