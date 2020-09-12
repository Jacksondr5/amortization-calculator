using System.Collections.Generic;

namespace AmortizationCalculator
{
    public static class AmortizationCalculator
    {
        public static List<AmortizationScheduleItem> GenerateAmortizationSchedule(
            Loan loan,
            List<PaymentSchedule> paymentSchedules
        )
        { throw new System.NotImplementedException(); }

        public static List<AmortizationScheduleItem> GenerateAmortizationSchedule(
            Loan loan,
            PaymentFrequency paymentFrequency,
            PaymentType paymentType,
            int term
        )
        { throw new System.NotImplementedException(); }

        private static List<AmortizationScheduleItem> Calculate(
            Loan loan,
            List<PaymentSchedule> paymentSchedules
        )
        { throw new System.NotImplementedException(); }
    }
}