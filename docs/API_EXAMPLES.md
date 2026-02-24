# API Examples

## Traditional Calculations

### Request
```
POST /api/traditional-calculations
Content-Type: application/json

{  
  "premium": 1000,  
  "term": 20,  
  "sum_assured": 50000  
}
```

### Response
```
{
  "total_premium": 20000,
  "maturity_amount": 75000
}
```


## ULIP Projections

### Request
```
POST /api/ulip-projections
Content-Type: application/json

{
  "investment_period": 15,
  "premium": 2000,
  "fund_option": "equity"
}
```

### Response
```
{
  "projected_value": 100000,
  "risk": "medium"
}
```


## Admin Endpoints

### Request
```
GET /api/admin/users
```

### Response
```
[
  {
    "id": 1,
    "username": "user1",
    "role": "admin"
  },
  {
    "id": 2,
    "username": "user2",
    "role": "user"
  }
]
```


## Excel Upload

### Request
```
POST /api/upload-excel
Content-Type: multipart/form-data

{ file: <ExcelFile> }
```

### Response
```
{
  "status": "success",
  "message": "File uploaded successfully!"
}
```


## Error Responses

### Request
```
GET /api/non-existent-endpoint
```

### Response
```
{
  "error": "Not Found",
  "message": "The requested endpoint does not exist."
}
```

### Request
```
POST /api/traditional-calculations
Content-Type: application/json

{
  "premium": -1000,
  "term": 0,
  "sum_assured": 0
}
```

### Response
```
{
  "error": "Validation Error",
  "message": "Premium, term, and sum assured must be positive values."
}
```
