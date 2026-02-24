# DETAILED API DOCUMENTATION

## Overview
This document provides comprehensive details about the API endpoints available for traditional calculations, ULIP projections, admin endpoints, excel uploads, and audit logs.

## Table of Contents
1. [Traditional Calculations](#traditional-calculations)
2. [ULIP Projections](#ulip-projections)
3. [Admin Endpoints](#admin-endpoints)
4. [Excel Upload](#excel-upload)
5. [Audit Logs](#audit-logs)

---

## Traditional Calculations
### Endpoint: `/api/v1/traditional_calculations`
- **Method**: POST  
- **Request Example**:
    ```json
    {
        "age": 30,
        "sum_assured": 100000,
        "term": 20
    }
    ```
- **Response Example**:
    ```json
    {
        "premium": 5000,
        "death_benefit": 120000
    }
    ```

## ULIP Projections
### Endpoint: `/api/v1/ulip_projections`
- **Method**: POST  
- **Request Example**:
    ```json
    {
        "investment": 100000,
        "years": 10,
        "rate_of_return": 8
    }
    ```
- **Response Example**:
    ```json
    {
        "projected_value": 215000,
        "total_investment": 100000
    }
    ```

## Admin Endpoints
### Endpoint: `/api/v1/admin/users`
- **Method**: GET  
- **Response Example**:
    ```json
    [
        {
            "id": 1,
            "name": "John Doe",
            "role": "Admin"
        },
        {
            "id": 2,
            "name": "Jane Smith",
            "role": "User"
        }
    ]
    ```

## Excel Upload
### Endpoint: `/api/v1/upload/excel`
- **Method**: POST  
- **Request Example**:
    ```json
    { 
        "file": "<binary excel file>"  
    }
    ```
- **Response Example**:
    ```json
    {
        "status": "success",
        "message": "File uploaded successfully"
    }
    ```

## Audit Logs
### Endpoint: `/api/v1/audit_logs`
- **Method**: GET  
- **Response Example**:
    ```json
    [
        {
            "timestamp": "2026-02-24 16:00:00",
            "event": "User logged in"
        },
        {
            "timestamp": "2026-02-24 17:00:00",
            "event": "File uploaded"
        }
    ]
    ```

---

## Conclusion
This documentation is intended to help developers understand the API functionality and how to interact with it effectively for various use cases in traditional insurance calculations and management.