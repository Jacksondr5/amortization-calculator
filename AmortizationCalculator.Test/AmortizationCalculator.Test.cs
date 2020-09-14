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
    public class AmortizationCalculatorTest
    {
        private static Loan GetTestLoan(
            AccrualBasis accrualBasis = AccrualBasis.ActualActual,
            decimal amount = 0,
            decimal interestRate = 0,
            DateTime? interestAccrualStartDate = null
        ) => new Loan
        {
            AccrualBasis = accrualBasis,
            Amount = amount,
            InterestRate = interestRate,
            InterestAccrualStartDate =
                interestAccrualStartDate ?? new DateTime(2001, 1, 1)
        };

        private static PaymentSchedule GetTestPaymentSchedule(
            DateTime? endDate = null,
            PaymentFrequency paymentFrequency = PaymentFrequency.Annual,
            decimal paymentAmount = 0,
            PaymentType paymentType = PaymentType.InterestOnly,
            DateTime? startDate = null
        ) => new PaymentSchedule
        {
            EndDate = endDate ?? new DateTime(2001, 1, 1),
            PaymentAmount = paymentAmount,
            PaymentFrequency = paymentFrequency,
            PaymentType = paymentType,
            StartDate = startDate ?? new DateTime(2001, 1, 1)
        };

        [TestMethod]
        public void GenerateAmSchedule_FrequencyIsAnnual_ShouldGenerateCorrectNumberOfItems()
        {
            //Assemble
            var startDate = new LocalDate(2001, 1, 1);
            var endDate = new LocalDate(2003, 1, 1);
            var expected = new List<DateTime>
            {
                startDate.ToDateTimeUnspecified(),
                startDate.PlusYears(1).ToDateTimeUnspecified(),
                startDate.PlusYears(2).ToDateTimeUnspecified(),
            };

            //Act
            RunFrequencyTest(expected, endDate, PaymentFrequency.Annual);
        }

        [TestMethod]
        public void GenerateAmSchedule_FrequencyIsSemiAnnual_ShouldGenerateCorrectNumberOfItems()
        {
            //Assemble
            var startDate = new LocalDate(2001, 1, 1);
            var endDate = new LocalDate(2002, 1, 1);
            var expected = new List<DateTime>
            {
                startDate.ToDateTimeUnspecified(),
                startDate.PlusMonths(6).ToDateTimeUnspecified(),
                startDate.PlusYears(1).ToDateTimeUnspecified(),
            };

            //Act
            RunFrequencyTest(expected, endDate, PaymentFrequency.SemiAnnual);
        }

        [TestMethod]
        public void GenerateAmSchedule_FrequencyIsQuarterly_ShouldGenerateCorrectNumberOfItems()
        {
            //Assemble
            var startDate = new LocalDate(2001, 1, 1);
            var endDate = new LocalDate(2001, 7, 1);
            var expected = new List<DateTime>
            {
                startDate.ToDateTimeUnspecified(),
                startDate.PlusMonths(3).ToDateTimeUnspecified(),
                startDate.PlusMonths(6).ToDateTimeUnspecified(),
            };

            //Act
            RunFrequencyTest(expected, endDate, PaymentFrequency.Quarterly);
        }

        [TestMethod]
        public void GenerateAmSchedule_FrequencyIsMonthly_ShouldGenerateCorrectNumberOfItems()
        {
            //Assemble
            var startDate = new LocalDate(2001, 1, 1);
            var endDate = new LocalDate(2001, 3, 1);
            var expected = new List<DateTime>
            {
                startDate.ToDateTimeUnspecified(),
                startDate.PlusMonths(1).ToDateTimeUnspecified(),
                startDate.PlusMonths(2).ToDateTimeUnspecified(),
            };

            //Act
            RunFrequencyTest(expected, endDate, PaymentFrequency.Monthly);
        }

        [TestMethod]
        public void GenerateAmSchedule_FrequencyIsWeekly_ShouldGenerateCorrectNumberOfItems()
        {
            //Assemble
            var startDate = new LocalDate(2001, 1, 1);
            var endDate = new LocalDate(2001, 1, 15);
            var expected = new List<DateTime>
            {
                startDate.ToDateTimeUnspecified(),
                startDate.PlusWeeks(1).ToDateTimeUnspecified(),
                startDate.PlusWeeks(2).ToDateTimeUnspecified(),
            };

            //Act
            RunFrequencyTest(expected, endDate, PaymentFrequency.Weekly);
        }

        private static void RunFrequencyTest(
            List<DateTime> expected,
            LocalDate endDate,
            PaymentFrequency frequency
        )
        {
            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                GetTestLoan(),
                new List<PaymentSchedule>
                {
                    GetTestPaymentSchedule(
                        endDate: endDate.ToDateTimeUnspecified(),
                        paymentFrequency: frequency
                    )
                }
            );

            //Assert
            actual.Select(x => x.Date).Should().Equal(expected);
        }

        [TestMethod]
        public void GenerateAmSchedule_MultiplePaymentSchedules_ShouldOrderByDate()
        {
            //Assemble
            var annualStartDate = new LocalDate(2001, 1, 1);
            var semiAnnualStartDate = new LocalDate(2001, 3, 1);
            var expected = new List<DateTime>
            {
                annualStartDate.ToDateTimeUnspecified(),
                semiAnnualStartDate.ToDateTimeUnspecified(),
                semiAnnualStartDate.PlusMonths(6).ToDateTimeUnspecified(),
                annualStartDate.PlusYears(1).ToDateTimeUnspecified(),
                semiAnnualStartDate.PlusYears(1).ToDateTimeUnspecified(),
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                GetTestLoan(),
                new List<PaymentSchedule>
                {
                    GetTestPaymentSchedule(
                        endDate:
                            annualStartDate.PlusYears(1).ToDateTimeUnspecified(),
                        paymentFrequency: PaymentFrequency.Annual,
                        startDate: annualStartDate.ToDateTimeUnspecified()
                    ),
                    GetTestPaymentSchedule(
                        endDate: semiAnnualStartDate
                            .PlusYears(1)
                            .ToDateTimeUnspecified(),
                        paymentFrequency: PaymentFrequency.SemiAnnual,
                        startDate: semiAnnualStartDate.ToDateTimeUnspecified()
                    )
                }
            );

            //Assert
            actual.Select(x => x.Date).Should().Equal(expected);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void GenerateAmSchedule_ShouldAccrueInterestFromStartDate(
            int loanLength
        )
        {
            //Assemble
            var interestStartDate = new LocalDate(2001, 1, 1);
            var paymentStartDate = new DateTime(2001 + loanLength, 1, 1);
            var loan = GetTestLoan(
                amount: 1000,
                interestRate: 0.5m,
                interestAccrualStartDate:
                    interestStartDate.ToDateTimeUnspecified()
            );
            var yearlyInterest = loan.Amount * loan.InterestRate;
            var paymentSchedule = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate: paymentStartDate,
                    paymentFrequency: PaymentFrequency.Annual,
                    startDate: paymentStartDate
                )
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedule
            );

            //Assert
            actual[0].Interest.Should().Be(yearlyInterest * loanLength);
        }

        [TestMethod]
        public void GenerateAmSchedule_PaymentTypeIsInterestOnly_ShouldGenerateAmSchedule()
        {
            //Assemble
            var startDate = new LocalDate(2001, 1, 1);
            var loan = GetTestLoan(
                accrualBasis: AccrualBasis.Thirty360,
                amount: 1000,
                interestRate: 0.5m,
                interestAccrualStartDate: startDate.ToDateTimeUnspecified()
            );
            var monthlyDate = startDate.PlusMonths(1).ToDateTimeUnspecified();
            var quarterlyDate = startDate.PlusMonths(3).ToDateTimeUnspecified();
            var semiDate = startDate.PlusMonths(6).ToDateTimeUnspecified();
            var annualDate = startDate.PlusYears(1).ToDateTimeUnspecified();
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = monthlyDate,
                    //1 month of interest
                    Interest = loan.Amount * loan.InterestRate / 12
                },
                new AmortizationScheduleItem
                {
                    Date = quarterlyDate,
                    //2 months of interest
                    Interest = loan.Amount * loan.InterestRate / 6
                },
                new AmortizationScheduleItem
                {
                    Date = semiDate,
                    //3 months of interest
                    Interest = loan.Amount * loan.InterestRate / 4
                },
                new AmortizationScheduleItem
                {
                    Date = annualDate,
                    //6 months of interest
                    Interest = loan.Amount * loan.InterestRate / 2
                }
            };
            var paymentSchedules = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate: monthlyDate,
                    paymentFrequency: PaymentFrequency.Monthly,
                    startDate: monthlyDate
                ),
                GetTestPaymentSchedule(
                    endDate: quarterlyDate,
                    paymentFrequency: PaymentFrequency.Quarterly,
                    startDate: quarterlyDate
                ),
                GetTestPaymentSchedule(
                    endDate: semiDate,
                    paymentFrequency: PaymentFrequency.SemiAnnual,
                    startDate: semiDate
                ),
                GetTestPaymentSchedule(
                    endDate: annualDate,
                    paymentFrequency: PaymentFrequency.Annual,
                    startDate: annualDate
                )
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedules
            );

            //Assert
            actual
                .Select(x => x.Date)
                .Should()
                .Equal(expected.Select(x => x.Date));
            actual
                .Select(x => TestUtils.RoundDecimal(x.Interest))
                .Should()
                .Equal(expected.Select(x => TestUtils.RoundDecimal(x.Interest)));
        }

        [TestMethod]
        public void GenerateAmSchedule_PaymentTypeIsLevelPayment_ShouldGenerateAmSchedule()
        {
            //Assemble
            var loan = GetTestLoan(
                amount: 1000,
                interestRate: 0.1m
            );
            var paymentSchedule = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate: new DateTime(2001, 4, 1),
                    paymentAmount: 338.9m,
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentType: PaymentType.LevelPayment,
                    startDate: new DateTime(2001, 2, 1)
                )
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 2, 1),
                    Interest = 8.49315m,
                    Principal = 330.40685m,
                    RemainingBalance = 669.59315m
                },
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 3, 1),
                    Interest = 5.13660m,
                    Principal = 333.76340m,
                    RemainingBalance = 335.82976m
                },
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 4, 1),
                    Interest = 2.85225m,
                    Principal = 335.82976m,
                    RemainingBalance = 0m
                }
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedule
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }

        [TestMethod]
        public void GenerateAmSchedule_PaymentTypeIsLevelPrincipal_ShouldGenerateAmSchedule()
        {
            //Assemble
            var levelPrincipalAmount = 300;
            var numberOfPayments = 3;
            var loan = GetTestLoan(
                amount: levelPrincipalAmount * numberOfPayments,
                interestRate: 0.1m
            );
            var paymentSchedule = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate: new DateTime(2001, 4, 1),
                    paymentAmount: levelPrincipalAmount,
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentType: PaymentType.LevelPrincipal,
                    startDate: new DateTime(2001, 2, 1)
                )
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 2, 1),
                    Interest = 7.64384m,
                    Principal = levelPrincipalAmount,
                    RemainingBalance = 600
                },
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 3, 1),
                    Interest = 4.60274m,
                    Principal = levelPrincipalAmount,
                    RemainingBalance = 300
                },
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 4, 1),
                    Interest = 2.54795m,
                    Principal = levelPrincipalAmount,
                    RemainingBalance = 0
                }
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedule
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }

        [TestMethod]
        public void GenerateAmSchedule_PaymentTypeIsPrincipalPercentage_ShouldGenerateAmSchedule()
        {
            //Assemble
            var loan = GetTestLoan(
                amount: 1000,
                interestRate: 0.1m
            );
            var paymentSchedule = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate: new DateTime(2001, 4, 1),
                    paymentAmount: 0.2m,
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentType: PaymentType.PrincipalPercentage,
                    startDate: new DateTime(2001, 2, 1)
                )
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 2, 1),
                    Interest = 8.49315m,
                    Principal = 200,
                    RemainingBalance = 800
                },
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 3, 1),
                    Interest = 6.13699m,
                    Principal = 160,
                    RemainingBalance = 640
                },
                //Automatic bullet payment
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 4, 1),
                    Interest = 5.43562m,
                    Principal = 640,
                    RemainingBalance = 0
                }
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedule
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }

        [TestMethod]
        public void GenerateAmSchedule_PaymentTypeIsPrincipalOnly_ShouldGenerateAmSchedule()
        {
            //Assemble
            var levelPrincipalAmount = 300;
            var numberOfPayments = 3;
            var loan = GetTestLoan(
                amount: levelPrincipalAmount * numberOfPayments,
                interestRate: 0.1m
            );
            var paymentSchedule = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate: new DateTime(2001, 4, 1),
                    paymentAmount: levelPrincipalAmount,
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentType: PaymentType.PrincipalOnly,
                    startDate: new DateTime(2001, 2, 1)
                )
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 2, 1),
                    Interest = 0,
                    Principal = levelPrincipalAmount,
                    RemainingBalance = 600
                },
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 3, 1),
                    Interest = 0,
                    Principal = levelPrincipalAmount,
                    RemainingBalance = 300
                },
                //Automatic bullet payment
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 4, 1),
                    Interest = 14.79452m,
                    Principal = levelPrincipalAmount,
                    RemainingBalance = 0
                }
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedule
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }

        [TestMethod]
        public void GenerateAmSchedule_PaymentTypeIsBullet_ShouldPayOffEntireLoan()
        {
            var loanAmount = 10000;
            var loan = GetTestLoan(
                amount: loanAmount,
                interestRate: 0.1m,
                interestAccrualStartDate: new DateTime(2001, 1, 1)
            );
            var paymentSchedule = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    paymentFrequency: PaymentFrequency.Bullet,
                    paymentType: PaymentType.Bullet,
                    startDate: new DateTime(2001, 2, 1)
                )
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = new DateTime(2001, 2, 1),
                    Interest = 84.93151m,
                    Principal = loanAmount,
                    RemainingBalance = 0
                },
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedule
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }

        [TestMethod]
        public void GenerateAmSchedule_MultiplePaymentSchedulesProvided_ShouldCalculatePaymentsCorrectly()
        {
            //Assemble
            var loanStartDate = new LocalDate(2001, 1, 1);
            var percentagePaymentsStartDate =
                loanStartDate.PlusMonths(1).PlusDays(15);
            var loan = GetTestLoan(
                amount: 3000,
                interestAccrualStartDate: loanStartDate.ToDateTimeUnspecified(),
                interestRate: 0.1m
            );
            var paymentSchedules = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate:
                        loanStartDate.PlusMonths(2).ToDateTimeUnspecified(),
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentAmount: 100,
                    paymentType: PaymentType.LevelPayment,
                    startDate:
                        loanStartDate.PlusMonths(1).ToDateTimeUnspecified()
                ),
                GetTestPaymentSchedule(
                    endDate: percentagePaymentsStartDate
                        .PlusMonths(1)
                        .ToDateTimeUnspecified(),
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentAmount: 0.1m,
                    paymentType: PaymentType.PrincipalPercentage,
                    startDate:
                        percentagePaymentsStartDate.ToDateTimeUnspecified()
                ),
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = loanStartDate.PlusMonths(1).ToDateTimeUnspecified(),
                    Interest = 25.47945m,
                    Principal = 74.52055m,
                    RemainingBalance = 2925.47945m,
                },
                new AmortizationScheduleItem
                {
                    Date = percentagePaymentsStartDate.ToDateTimeUnspecified(),
                    Interest = 12.02252m,
                    Principal = 292.54795m,
                    RemainingBalance = 2632.93151m,
                },
                new AmortizationScheduleItem
                {
                    Date = loanStartDate.PlusMonths(2).ToDateTimeUnspecified(),
                    Interest = 9.37756m,
                    Principal = 90.62244m,
                    RemainingBalance = 2542.30907m,
                },
                //Automatic bullet payment
                new AmortizationScheduleItem
                {
                    Date = percentagePaymentsStartDate
                        .PlusMonths(1)
                        .ToDateTimeUnspecified(),
                    Interest = 10.44785m,
                    Principal = 2542.30907m,
                    RemainingBalance = 0m,
                },
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedules
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }

        [TestMethod]
        public void GenerateAmSchedule_ScheduleListDoesNotContainBulletSchedule_ShouldAddBulletSchedule()
        {
            var loanStartDate = new LocalDate(2001, 1, 1);
            var loan = GetTestLoan(
                amount: 3000,
                interestAccrualStartDate: loanStartDate.ToDateTimeUnspecified(),
                interestRate: 0.1m
            );
            var paymentSchedules = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate:
                        loanStartDate.PlusMonths(2).ToDateTimeUnspecified(),
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentAmount: 100,
                    paymentType: PaymentType.LevelPayment,
                    startDate:
                        loanStartDate.PlusMonths(1).ToDateTimeUnspecified()
                ),
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = loanStartDate.PlusMonths(1).ToDateTimeUnspecified(),
                    Interest = 25.47945m,
                    Principal = 74.52055m,
                    RemainingBalance = 2925.47945m,
                },
                new AmortizationScheduleItem
                {
                    Date = loanStartDate.PlusMonths(2).ToDateTimeUnspecified(),
                    Interest = 22.44203m,
                    Principal = 2925.47945m,
                    RemainingBalance = 0m,
                },
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedules
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }

        [TestMethod]
        public void GenerateAmSchedule_ScheduleListContainsBulletPayment_AllPaymentsAfterBulletShouldBeRemoved()
        {
            var loanStartDate = new LocalDate(2001, 1, 1);
            var loan = GetTestLoan(
                amount: 3000,
                interestAccrualStartDate: loanStartDate.ToDateTimeUnspecified(),
                interestRate: 0.1m
            );
            var paymentSchedules = new List<PaymentSchedule>
            {
                GetTestPaymentSchedule(
                    endDate:
                        loanStartDate.PlusMonths(5).ToDateTimeUnspecified(),
                    paymentFrequency: PaymentFrequency.Monthly,
                    paymentAmount: 100,
                    paymentType: PaymentType.LevelPayment,
                    startDate:
                        loanStartDate.PlusMonths(1).ToDateTimeUnspecified()
                ),
                GetTestPaymentSchedule(
                    paymentAmount: 0.1m,
                    paymentType: PaymentType.Bullet,
                    startDate:
                        loanStartDate.PlusMonths(2).ToDateTimeUnspecified()
                ),
            };
            var expected = new List<AmortizationScheduleItem>
            {
                new AmortizationScheduleItem
                {
                    Date = loanStartDate.PlusMonths(1).ToDateTimeUnspecified(),
                    Interest = 25.47945m,
                    Principal = 74.52055m,
                    RemainingBalance = 2925.47945m,
                },
                new AmortizationScheduleItem
                {
                    Date = loanStartDate.PlusMonths(2).ToDateTimeUnspecified(),
                    Interest = 22.44203m,
                    Principal = 2925.47945m,
                    RemainingBalance = 0m,
                },
            };

            //Act
            var actual = AmortizationCalculator.GenerateAmortizationSchedule(
                loan,
                paymentSchedules
            );

            //Assert
            actual
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = TestUtils.RoundDecimal(x.Interest),
                    Principal = TestUtils.RoundDecimal(x.Principal),
                    RemainingBalance =
                        TestUtils.RoundDecimal(x.RemainingBalance),
                })
                .Should()
                .BeEquivalentTo(expected, x => x.Excluding(x => x.Schedule));
        }
    }
}
