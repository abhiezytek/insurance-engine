# API Documentation for Insurance Calculation Endpoints

## 1. Introduction
- Overview of the insurance calculation system.

## 2. Traditional Benefit Calculation
- **Endpoint:** `/api/traditional/calculate`
- **Method:** POST
- **Parameters:**
  - `premium`: Number (Monthly premium)
  - `term`: Number (Policy term in years)
  - **Example Request:**
    ```json
    {
      "premium": 1000,
      "term": 10
    }
    ```
  - **Example Response:**
    ```json
    {
      "totalBenefit": 120000
    }
    ```

## 3. ULIP Benefit Calculation
- **Endpoint:** `/api/ulip/calculate`
- **Method:** POST
- **Parameters:**
  - `investmentAmount`: Number (Amount invested per month)
  - `duration`: Number (Investment duration in years)
  - **Example Request:**
    ```json
    {
      "investmentAmount": 5000,
      "duration": 10
    }
    ```
  - **Example Response:**
    ```json
    {
      "totalInvestment": 600000,
      "estimatedReturns": 800000
    }
    ```

## 4. Formula Testing
- Descriptive guide on how to test various formulas used for calculations.

## 5. Parameter Management
- Explanation of parameter handling and validation rules.

## 6. Audit Logs
- Overview of how audit logs are maintained for transactions.
  
## 7. Conclusion
- Summary and references to further documentation.