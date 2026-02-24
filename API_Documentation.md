# API Documentation

## Overview
This is the API documentation for the Insurance Engine project. The purpose of this document is to provide comprehensive details on the API endpoints available in the Insurance Engine, including examples of requests and responses.

## Base URL
`https://api.insurance-engine.com/v1`

## Endpoints

### 1. Create Policy
- **Endpoint**: `/policies`
- **Method**: `POST`
- **Request**:
  - **Headers**:
    - `Content-Type: application/json`
  - **Body**:
    ```json
    {
      "customerId": "string",
      "coverageType": "string",
      "premium": number,
      "startDate": "YYYY-MM-DD",
      "endDate": "YYYY-MM-DD"
    }
    ```
- **Response**:
  - **Status**: `201 Created`
  - **Body**:
    ```json
    {
      "policyId": "string",
      "status": "active"
    }
    ```

### 2. Get Policy
- **Endpoint**: `/policies/{policyId}`
- **Method**: `GET`
- **Request**:
  - **Headers**:
    - `Authorization: Bearer {token}`
- **Response**:
  - **Status**: `200 OK`
  - **Body**:
    ```json
    {
      "policyId": "string",
      "customerId": "string",
      "coverage": {
        "type": "string",
        "amount": number
      },
      "status": "string"
    }
    ```

### 3. Update Policy
- **Endpoint**: `/policies/{policyId}`
- **Method**: `PUT`
- **Request**:
  - **Headers**:
    - `Content-Type: application/json`
  - **Body**:
    ```json
    {
      "coverageType": "string",
      "premium": number
    }
    ```
- **Response**:
  - **Status**: `200 OK`
  - **Body**:
    ```json
    {
      "message": "Policy updated successfully"
    }
    ```

### 4. Delete Policy
- **Endpoint**: `/policies/{policyId}`
- **Method**: `DELETE`
- **Response**:
  - **Status**: `204 No Content`

## Authentication
- Use Bearer Token for authentication in the headers of each request.

## Error Handling
- Common error responses are returned with appropriate HTTP status codes and a JSON body containing an error message.

### Example Error:
- **Status**: `404 Not Found`
- **Body**:
    ```json
    {
      "error": "Policy not found"
    }
    ```

## Conclusion
This API provides essential functionalities to manage insurance policies seamlessly. For further queries, please refer to the official support documentation or contact support.