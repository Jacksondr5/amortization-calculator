using System;
using System.Collections.Generic;
using System.Linq;
using AmortizationCalculator;
using Microsoft.AspNetCore.Mvc;

namespace AmCalcWeb.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AmortizationController : ControllerBase
    {
        [HttpPost]
        public List<AmortizationScheduleItem> Post(
            [FromBody] AmCalcInput input
        )
        {
            input.Loan.InterestRate = input.Loan.InterestRate / 100;
            return AmortizationCalculator.AmortizationCalculator.GenerateAmortizationSchedule(
                input.Loan,
                input.PaymentSchedules
            )
                .Select(x => new AmortizationScheduleItem
                {
                    Date = x.Date,
                    Interest = Math.Round(x.Interest, 2),
                    Principal = Math.Round(x.Principal, 2),
                    RemainingBalance = Math.Round(x.RemainingBalance, 2)
                })
                .ToList();
        }
    }

    public class AmCalcInput
    {
        public Loan Loan { get; set; }
        public List<PaymentSchedule> PaymentSchedules { get; set; }
    }
}