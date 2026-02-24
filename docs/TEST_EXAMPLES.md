# Test Examples for Insurance Engine

## Unit Test for Formula Engine

### Test Case 1: Simple Formula Evaluation
- **Input**: `2 + 3`
- **Expected Output**: `5`
- **Execution Result**: Passed

### Test Case 2: Formula with Parentheses
- **Input**: `(2 + 3) * 4`
- **Expected Output**: `20`
- **Execution Result**: Passed

## Unit Test for Condition Evaluator

### Test Case 1: Simple Condition
- **Input**: `age > 18`
- **Expected Output**: `True` if age is `20`, `False` if age is `16`
- **Execution Result**: Passed

### Test Case 2: Complex Condition
- **Input**: `income > 50000 and age < 30`
- **Expected Output**: `True` if income is `60000` and age is `25`, `False` if income is `40000` and age is `35`
- **Execution Result**: Passed

## Unit Test for ULIP Calculator

### Test Case 1: Basic ULIP Calculation
- **Input**: Premium: `1000`, Duration: `10 years`, Rate of return: `8%`
- **Expected Output**: `Total Sum Assured: 12000`
- **Execution Result**: Passed

### Test Case 2: ULIP with Additional Charges
- **Input**: Premium: `1000`, Duration: `10 years`, Rate of return: `8%`, Charges: `200`
- **Expected Output**: `Total Sum Assured: 11800`
- **Execution Result**: Passed