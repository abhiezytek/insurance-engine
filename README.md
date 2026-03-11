# Enterprise Insurance Benefit Engine

A runnable prototype of the Insurance Engine built with .NET 8 (ASP.NET Core Web API) and React (Vite).

## Architecture

```
src/
  InsuranceEngine.Api/       # .NET 8 ASP.NET Core Web API
  InsuranceEngine.Tests/     # NUnit test project
  insurance-engine-ui/       # React + Vite frontend
InsuranceEngine.slnx         # .NET solution file
docker-compose.yml           # Docker Compose for local dev
```

## Quick Start with Docker Compose

```bash
docker compose up --build
```

This starts:
- **SQL Server** on port 1433
- **API** on http://localhost:5000 (Swagger at http://localhost:5000/swagger)
- **UI** on http://localhost:3000

On first startup, the API automatically applies EF Core migrations and seeds sample data.

## Local Development (without Docker)

### Prerequisites
- .NET 8 SDK
- SQL Server (local or Azure SQL)
- Node.js 18+

### Backend API

```bash
# Set connection string (or update appsettings.Development.json)
export ConnectionStrings__DefaultConnection="Server=localhost;Database=InsuranceEngineDb;TrustServerCertificate=True;Integrated Security=True"

# Apply migrations (first time)
cd src/InsuranceEngine.Api
dotnet ef database update

# Run the API
dotnet run
# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

### React Frontend

```bash
cd src/insurance-engine-ui
cp .env.example .env
# Edit .env to set VITE_API_URL=http://localhost:5000
npm install
npm run dev
# UI available at http://localhost:5173
```

## Running Tests

```bash
dotnet test InsuranceEngine.slnx
```

Tests cover:
- Formula evaluation (including dependency ordering, built-in functions)
- Condition evaluation (AND/OR groups, all operators)
- Calculation endpoint integration tests (in-memory DB)

## DB Migration Instructions

```bash
# Apply migrations
cd src/InsuranceEngine.Api
dotnet ef database update

# Create a new migration (after model changes)
dotnet ef migrations add <MigrationName>
```

## Sample Calculation

Using the seeded **CENTURY_INCOME** product:

```bash
curl -X POST http://localhost:5000/api/calculation/traditional \
  -H "Content-Type: application/json" \
  -d '{
    "productCode": "CENTURY_INCOME",
    "parameters": {
      "AP": 10000,
      "SA": 100000,
      "PPT": 10,
      "PT": 20,
      "Age": 35,
      "TotalPremiumPaid": 50000,
      "SurrenderValue": 40000
    }
  }'
```

Expected response:
```json
{
  "productCode": "CENTURY_INCOME",
  "version": "1.0",
  "results": {
    "GMB": 115000.0,
    "GSV": 34500.0,
    "SSV": 120000.0,
    "MATURITY_BENEFIT": 115000.0,
    "DEATH_BENEFIT": 100000.0
  }
}
```

## Sample Data

The seed data creates:
- **Insurer**: Sample Life Insurance Co. (SLIC)
- **Product**: Century Income Plan (CENTURY_INCOME)
- **Version**: 1.0
- **Parameters**: AP, SA, PPT, PT, Age, TotalPremiumPaid, SurrenderValue
- **Formulas** (in execution order):
  1. `GMB = AP * 11.5`
  2. `GSV = GMB * 0.30`
  3. `SSV = AP * 12`
  4. `MATURITY_BENEFIT = GMB`
  5. `DEATH_BENEFIT = MAX(10*AP, 1.05*TotalPremiumPaid, SurrenderValue)`

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/calculation/traditional` | Run traditional product calculation |
| GET | `/api/admin/products` | List all products |
| POST | `/api/admin/products` | Create a product |
| GET | `/api/admin/versions` | List product versions |
| POST | `/api/admin/versions` | Create a version |
| GET | `/api/admin/parameters` | List parameters |
| POST | `/api/admin/parameters` | Create a parameter |
| GET | `/api/admin/formulas` | List formulas |
| POST | `/api/admin/formulas` | Create a formula |
| PUT | `/api/admin/formulas/{id}` | Update a formula |
| DELETE | `/api/admin/formulas/{id}` | Delete a formula |
| POST | `/api/admin/formulas/{id}/test` | Test formula expression |
| GET | `/api/admin/insurers` | List insurers |
| POST | `/api/upload` | Upload Excel/CSV for bulk operations |
| GET | `/api/upload/batches` | List upload batches |
| GET | `/health` | Health check |
| GET | `/health/ready` | Readiness check |

## Excel/CSV Upload Format

### Formulas CSV
```csv
Name,Expression,ExecutionOrder,Description
GMB,AP * 11.5,1,Guaranteed Maturity Benefit
GSV,GMB * 0.30,2,Guaranteed Surrender Value
```

### Parameters CSV
```csv
Name,DataType,Description
AP,decimal,Annual Premium
SA,decimal,Sum Assured
```