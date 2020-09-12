using System;

namespace AmortizationCalculator
{
    public class Class1
    {
    }

    public enum AccrualBasis
    {
        Actual360,
        Actual365,
        ActualActual,
        Thirty360,
    }

    public enum PaymentType
    {
        Custom,
        InterestOnly,
        LevelPayment,
        LevelPrincipal,
        PrincipalPercentage,
    }

    public class Loan
    {
        public DateTime StartDate { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public AccrualBasis AccrualBasis { get; set; }
    }

    public class PaymentSchedule
    {
        public PaymentFrequency PaymentFrequency { get; set; }
        public decimal PaymentAmount { get; set; }
        public PaymentType PaymentType { get; set; }
        public DateTime StartDate { get; set; }
    }

    public enum PaymentFrequency
    {
        Annual,
        SemiAnnual,
        Quarterly,
        Monthly,
        Biweekly,
        Weekly,
    }

    public class AmortizationScheduleItem
    {
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public DateTime Date { get; set; }
        public decimal RemainingBalance { get; set; }
    }
}
