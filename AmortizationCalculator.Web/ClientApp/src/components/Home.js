import React, { useReducer, useState } from "react";
import moment from "moment";
import {
  Select,
  TextField,
  InputLabel,
  MenuItem,
  TableContainer,
  Paper,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Button,
  FormControl,
} from "@material-ui/core";
import { DatePicker, MuiPickersUtilsProvider } from "@material-ui/pickers";
import MomentUtils from "@date-io/moment";

const accrualBasisList = [
  { name: "Actual360", value: 0 },
  { name: "Actual365", value: 1 },
  { name: "ActualActual", value: 2 },
  { name: "Thirty360", value: 3 },
];

const paymentFrequencies = [
  { name: "Annual", value: 0 },
  // { name: "Biweekly", value: 1 },
  { name: "Bullet", value: 1 },
  { name: "Monthly", value: 2 },
  { name: "Quarterly", value: 3 },
  { name: "SemiAnnual", value: 4 },
  { name: "Weekly", value: 5 },
];

const paymentTypes = [
  { name: "Bullet", value: 0 },
  { name: "Custom", value: 1 },
  { name: "InterestOnly", value: 2 },
  { name: "LevelPayment", value: 3 },
  { name: "LevelPrincipal", value: 4 },
  { name: "PrincipalPercentage", value: 5 },
  { name: "PrincipalOnly", value: 6 },
];

function GetNewPaymentSchedule(id = 0) {
  return {
    id: id,
    startDate: moment(),
    endDate: moment().add(1, "y"),
    paymentType: { name: "LevelPayment", value: 3 },
    paymentFrequency: { name: "Annual", value: 0 },
    amount: 500,
  };
}

function PaymentScheduleReducer(state, action) {
  var retVal = [...state];
  switch (action.target) {
    case "new":
      retVal = [...state, GetNewPaymentSchedule(retVal.length)];
      break;
    case "startDate":
      retVal[action.rowId].startDate = action.value;
      break;
    case "endDate":
      retVal[action.rowId].endDate = action.value;
      break;
    case "paymentType":
      retVal[action.rowId].paymentType = paymentTypes[action.value];
      break;
    case "paymentFrequency":
      retVal[action.rowId].paymentFrequency = paymentFrequencies[action.value];
      break;
    case "amount":
      retVal[action.rowId].amount = action.value;
      break;
    default:
      throw new Error();
  }
  return retVal;
}

export function Home() {
  var [loanAmount, setLoanAmount] = useState(0);
  var [accrualBasis, setAccrualBasis] = useState(3);
  var [interestRate, setInterestRate] = useState(0);
  var [interestStartDate, setInterestStartDate] = useState(moment());
  var [maturityDate, setMaturityDate] = useState(moment());
  var [
    paymentSchedules,
    setPaymentSchedules,
  ] = useReducer(PaymentScheduleReducer, [GetNewPaymentSchedule()]);
  var [amSchedule, setAmSchedule] = useState([]);
  return (
    <MuiPickersUtilsProvider utils={MomentUtils}>
      <h3>Loan Information</h3>
      <TextField
        label="Loan Amount"
        value={loanAmount}
        onChange={(e) => setLoanAmount(e.target.value)}
      />
      <FormControl>
        <InputLabel id="accrual-basis-select-label">Accrual Basis</InputLabel>
        <Select
          id="accrural-basis-select"
          labelId="accrual-basis-select-label"
          value={accrualBasis}
          onChange={(e) => setAccrualBasis(e.target.value)}
        >
          {accrualBasisList.map((x) => (
            <MenuItem key={x.value} value={x.value}>
              {x.name}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      <TextField
        label="Interest Rate (%)"
        value={interestRate}
        onChange={(e) => setInterestRate(e.target.value)}
      />
      <DatePicker
        label="Interest Start Date"
        format="MM/DD/yyyy"
        views={["year", "month", "date"]}
        openTo="year"
        value={interestStartDate}
        onChange={setInterestStartDate}
      />
      <DatePicker
        label="Maturity Date"
        format="MM/DD/yyyy"
        views={["year", "month", "date"]}
        openTo="year"
        value={maturityDate}
        onChange={setMaturityDate}
      />
      <br />
      <h3>Payment Schedules</h3>
      <PaymentScheduleList
        paymentSchedules={paymentSchedules}
        paymentTypes={paymentTypes}
        paymentFrequencies={paymentFrequencies}
        onPaymentSchedulesChange={setPaymentSchedules}
      ></PaymentScheduleList>
      <Button
        variant="contained"
        color="primary"
        onClick={() => setPaymentSchedules({ target: "new" })}
      >
        Add New Payment Schedule
      </Button>
      <br />
      <Button
        variant="contained"
        color="primary"
        onClick={() => {
          var loan = {
            accrualBasis: accrualBasis,
            amount: new Number(loanAmount),
            interestRate: new Number(interestRate),
            interestAccrualStartDate: interestStartDate.format(),
            maturityDate: maturityDate.format(),
          };
          var mappedPaymentSchedules = paymentSchedules.map((x) => ({
            startDate: x.startDate.format(),
            endDate: x.endDate.format(),
            paymentType: x.paymentType.value,
            paymentFrequency: x.paymentFrequency.value,
            paymentAmount: new Number(x.amount),
          }));
          var postBody = { loan, paymentSchedules: mappedPaymentSchedules };
          console.log(postBody);
          console.log(JSON.stringify(postBody));
          fetch("amortization", {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify(postBody),
          })
            .then((x) => x.json())
            .then((x) => setAmSchedule(x));
        }}
      >
        Generate Amortization Schedule
      </Button>
      <AmortizationSchedule amSchedule={amSchedule} />
    </MuiPickersUtilsProvider>
  );
}

function AmortizationSchedule(props) {
  console.log(props);
  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Date</TableCell>
            <TableCell>Principal</TableCell>
            <TableCell>Interest</TableCell>
            <TableCell>Total Payment</TableCell>
            <TableCell>Remaining Balance</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {props.amSchedule.map((row) => (
            <TableRow key={row.id}>
              <TableCell>{row.date}</TableCell>
              <TableCell>{row.principal}</TableCell>
              <TableCell>{row.interest}</TableCell>
              <TableCell>{row.principal + row.interest}</TableCell>
              <TableCell>{row.remainingBalance}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

function PaymentScheduleList(props) {
  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Start Date</TableCell>
            <TableCell>End Date</TableCell>
            <TableCell>Payment Type</TableCell>
            <TableCell>Payment Frequency</TableCell>
            <TableCell>Amount</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {props.paymentSchedules.map((row) => (
            <TableRow key={row.id}>
              <TableCell>
                <DatePicker
                  label="Start Date"
                  format="MM/DD/yyyy"
                  views={["year", "month", "date"]}
                  openTo="year"
                  value={row.startDate}
                  onChange={(e) =>
                    props.onPaymentSchedulesChange({
                      rowId: row.id,
                      target: "startDate",
                      value: e,
                    })
                  }
                />
              </TableCell>
              <TableCell>
                <DatePicker
                  label="End Date"
                  format="MM/DD/yyyy"
                  views={["year", "month", "date"]}
                  openTo="year"
                  value={row.endDate}
                  onChange={(e) =>
                    props.onPaymentSchedulesChange({
                      rowId: row.id,
                      target: "endDate",
                      value: e,
                    })
                  }
                />
              </TableCell>
              <TableCell>
                <FormControl>
                  <InputLabel id="payment-type-select-label">Type</InputLabel>
                  <Select
                    labelId="payment-type-select-label"
                    value={row.paymentType.value}
                    onChange={(e) =>
                      props.onPaymentSchedulesChange({
                        rowId: row.id,
                        target: "paymentType",
                        value: e.target.value,
                      })
                    }
                  >
                    {props.paymentTypes.map((x) => (
                      <MenuItem key={x.value} value={x.value}>
                        {x.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
              <TableCell>
                <FormControl>
                  <InputLabel id="payment-frequency-select-label">
                    Frequency
                  </InputLabel>
                  <Select
                    labelId="payment-frequency-select-label"
                    value={row.paymentFrequency.value}
                    onChange={(e) =>
                      props.onPaymentSchedulesChange({
                        rowId: row.id,
                        target: "paymentFrequency",
                        value: e.target.value,
                      })
                    }
                  >
                    {props.paymentFrequencies.map((x) => (
                      <MenuItem key={x.value} value={x.value}>
                        {x.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </TableCell>
              <TableCell>
                <TextField
                  label="Amount"
                  value={row.amount}
                  onChange={(e) =>
                    props.onPaymentSchedulesChange({
                      rowId: row.id,
                      target: "amount",
                      value: e.target.value,
                    })
                  }
                />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
