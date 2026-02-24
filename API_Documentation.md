# API Documentation for Insurance Benefit Calculation Endpoints

## Introduction
This API provides access to insurance benefit calculation services for both traditional and ULIP (Unit Linked Insurance Plan) policies.

## Endpoints

### Traditional Calculation Endpoint
- **URL**: `/api/v1/benefits/traditional`
- **Method**: `POST`

**Request Example**:
```json
{
  "policy_id": "123456",
  "age": 30,
  "premium": 5000
}
```

**Response Example**:
```json
{
  "benefit_amount": 100000,
  "status": "success"
}
```

---

### ULIP Calculation Endpoint
- **URL**: `/api/v1/benefits/ulip`
- **Method**: `POST`

**Request Example**:
```json
{
  "policy_id": "654321",
  "age": 35,
  "investment": 10000,
  "term": 15
}
```

**Response Example**:
```json
{
  "net_value": 150000,
  "status": "success"
}
```

---

## Formula Testing
- **Traditional Calculation Formula**: Formula description here.
- **ULIP Calculation Formula**: Formula description here.

### Example test cases and expected results

---

## Parameter Validation
- **Required Parameters**:
  - `policy_id`
  - `age`
- **Optional Parameters** for ULIP:
  - `investment`
  - `term`

### Error Handling Responses for Invalid Parameters
- Example for missing parameter:
```json
{
  "error": "Missing required parameter: age"
}
```

---

## Audit Logs
The API logs all calls made for monitoring and auditing purposes.

### Example Log Entries
```
2026-02-24 16:38:09 API Call: /api/v1/benefits/traditional, User: abhiezytek, Parameters: {policy_id: "123456", age: 30, premium: 5000}, Response: {benefit_amount: 100000, status: "success"}
```

---

## Conclusion
This API allows for comprehensive insurance benefit calculations, with detailed logging for audit purposes. For more information, please contact support.