using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

namespace AmortizationCalculator
{
    public static class AmortizationCalculator
    {
        public static List<AmortizationScheduleItem> GenerateAmortizationSchedule(
            Loan loan,
            List<PaymentSchedule> paymentSchedules
        )
        {
            //1. create list of payment dates and associate them with a payment 
            //schedule
            //2. 
            var amSchedule = new List<AmortizationScheduleItem>();
            foreach (var schedule in paymentSchedules)
            {
                var paymentNumber = 0;
                var paymentDate = LocalDate.FromDateTime(schedule.StartDate);
                while (paymentDate.CompareTo(
                    LocalDate.FromDateTime(schedule.EndDate)
                ) < 1)
                {
                    amSchedule.Add(
                        new AmortizationScheduleItem
                        {
                            Date = paymentDate.ToDateTimeUnspecified(),
                            Schedule = schedule
                        }
                    );
                    paymentDate = schedule.PaymentFrequency switch
                    {
                        PaymentFrequency.Annual => paymentDate.PlusYears(1),
                        PaymentFrequency.Monthly => paymentDate.PlusMonths(1),
                        PaymentFrequency.Quarterly => paymentDate.PlusMonths(3),
                        PaymentFrequency.SemiAnnual =>
                            paymentDate.PlusMonths(6),
                        PaymentFrequency.Weekly => paymentDate.PlusWeeks(1),
                        _ => throw new InvalidOperationException()
                    };
                    paymentNumber++;
                }
            }
            amSchedule = amSchedule.OrderBy(x => x.Date).ToList();
            var lastPaymentDate =
                LocalDate.FromDateTime(loan.InterestAccrualStartDate);
            var accruedInterest = 0m;
            var remainingBalance = loan.Amount;
            foreach (var payment in amSchedule)
            {
                //Calculate accrued interest
                var term = TermCalculator.CalculateTerm(
                    lastPaymentDate,
                    LocalDate.FromDateTime(payment.Date),
                    loan.AccrualBasis
                );
                accruedInterest += remainingBalance * loan.InterestRate * term;

                //Calculate payment
                switch (payment.Schedule.PaymentType)
                {
                    case PaymentType.InterestOnly:
                        payment.Interest = accruedInterest;
                        payment.Principal = 0;
                        accruedInterest = 0;
                        break;
                    case PaymentType.LevelPayment:
                        payment.Interest = accruedInterest;
                        payment.Principal = GetPrincipal(
                            payment.Schedule.PaymentAmount - accruedInterest,
                            remainingBalance
                        );
                        accruedInterest = 0;
                        break;
                    case PaymentType.LevelPrincipal:
                        payment.Interest = accruedInterest;
                        payment.Principal = GetPrincipal(
                            payment.Schedule.PaymentAmount,
                            remainingBalance
                        );
                        accruedInterest = 0;
                        break;
                    case PaymentType.PrincipalPercentage:
                        payment.Interest = accruedInterest;
                        payment.Principal = GetPrincipal(
                            remainingBalance * payment.Schedule.PaymentAmount,
                            remainingBalance
                        );
                        accruedInterest = 0;
                        break;
                    case PaymentType.PrincipalOnly:
                        payment.Interest = 0;
                        payment.Principal = GetPrincipal(
                            payment.Schedule.PaymentAmount,
                            remainingBalance
                        );
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                lastPaymentDate = LocalDate.FromDateTime(payment.Date);
                payment.RemainingBalance = remainingBalance -= payment.Principal;
            }

            return amSchedule;
        }

        private static decimal GetPrincipal(
            decimal calculatedPrincipal,
            decimal remainingBalance
        ) => calculatedPrincipal > remainingBalance ?
            remainingBalance :
            calculatedPrincipal;

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