using System;

namespace AmortizationCalculator
{
    public enum AccrualBasis
    {
        Actual360,
        Actual365,
        ActualActual,
        Thirty360,
    }

    public enum PaymentType
    {
        Bullet,
        Custom,
        InterestOnly,
        LevelPayment,
        LevelPrincipal,
        PrincipalPercentage,
        PrincipalOnly,
    }

    public class Loan
    {
        public AccrualBasis AccrualBasis { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime InterestAccrualStartDate { get; set; }
    }

    public class PaymentSchedule
    {
        public DateTime EndDate { get; set; }
        public PaymentFrequency PaymentFrequency { get; set; }
        public decimal PaymentAmount { get; set; }
        public PaymentType PaymentType { get; set; }
        /// <summary>
        /// The date of the first payment
        /// </summary>
        public DateTime StartDate { get; set; }
    }

    public enum PaymentFrequency
    {
        Annual,
        // Biweekly,
        Bullet,
        Monthly,
        Quarterly,
        SemiAnnual,
        Weekly,
    }

    public class AmortizationScheduleItem
    {
        internal PaymentSchedule Schedule { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public DateTime Date { get; set; }
        public decimal RemainingBalance { get; set; }
    }
}
