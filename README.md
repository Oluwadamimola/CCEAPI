# Country Currency & Exchange API

A RESTful API built with .NET 8 and C# that fetches country data from external APIs, stores it in a MySQL database, and provides CRUD operations with exchange rate calculations.

## Features

- Fetch and cache country data from REST Countries API
- Real-time exchange rate integration
- Automatic GDP estimation calculations
- MySQL database persistence
- Advanced filtering and sorting capabilities
- Dynamic summary image generation
- Comprehensive error handling

## Tech Stack

- **Framework:** .NET 8.0
- **Language:** C# 
- **Database:** MySQL 8.0
- **ORM:** Entity Framework Core 8.0
- **Image Processing:** SkiaSharp
- **API Documentation:** Swagger/Swashbuckle
- **Containerization:** Docker & Docker Compose

## 📦 Prerequisites

### For Local Development:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [MySQL 8.0](https://dev.mysql.com/downloads/installer/)
- IDE (Visual Studio 2022, VS Code, or Rider)


## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/CCEAPI.git
cd CCEAPI
```

### 2. Install Dependencies

```bash
dotnet restore
```

### 3. Install Required NuGet Packages

```bash
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.0
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
dotnet add package SkiaSharp --version 2.88.7
```

## Create Database

```bash
# Create migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

## Running the Application

### Local Development

```bash
dotnet run
```

## 📡 API Endpoints

### Country Operations

#### Refresh Countries Data

POST /countries/refresh
Fetches all countries and exchange rates from external APIs and caches them in the database.

**Response:**
```json
{
  "message": "Countries refreshed successfully"
}
```

#### Get All Countries

GET /countries

**Query Parameters:**
- `region` (optional): Filter by region (e.g., `Africa`, `Europe`)
- `currency` (optional): Filter by currency code (e.g., `NGN`, `USD`)
- `sort` (optional): Sort results
  - `gdp_desc` - Sort by GDP descending
  - `gdp_asc` - Sort by GDP ascending
  - `name_asc` - Sort by name A-Z
  - `name_desc` - Sort by name Z-A
  - `population_desc` - Sort by population descending
  - `population_asc` - Sort by population ascending

**Example:**
GET /countries?region=Africa&sort=gdp_desc



#### Get Single Country
```http
GET /countries/{name}
```

**Example:**
```http
GET /countries/Nigeria
```

#### Delete Country
```http
DELETE /countries/{name}
```

**Example:**
```http
DELETE /countries/Nigeria
```

**Response:**
```json
{
  "message": "Country 'Nigeria' deleted successfully"
}
```

### Status

#### Get System Status

GET /status

**Response:**
{
  "totalCountries": 250,
  "lastRefreshedAt": "2025-10-29T18:00:00Z"
}


### Image

#### Get Summary Image

GET /countries/image

Returns a PNG image showing:
- Total number of countries
- Top 5 countries by estimated GDP
- Last refresh timestamp



## 🌐 External APIs

### REST Countries API
- **URL:** https://restcountries.com/v2/all
- **Purpose:** Fetch country data (name, capital, region, population, currencies, flags)

### Open Exchange Rates API
- **URL:** https://open.er-api.com/v6/latest/USD
- **Purpose:** Fetch current exchange rates


## 🐛 Error Handling

The API returns consistent JSON error responses:

### 400 Bad Request
{
  "error": "Validation failed"
}

### 404 Not Found
{
  "error": "Country not found"
}

### 500 Internal Server Error
{
  "error": "Internal server error"
}

### 503 Service Unavailable
{
  "error": "External data source unavailable",
}

## 🧪 Testing

### Using Swagger UI

1. Run the application
2. Navigate to `https://localhost:5001/swagger`
3. Try out the endpoints interactively


- [REST Countries API](https://restcountries.com) for country data
- [Open Exchange Rates](https://www.exchangerate-api.com) for currency rates
- [SkiaSharp](https://github.com/mono/SkiaSharp) for image generation
- [Entity Framework Core](https://github.com/dotnet/efcore) for database operations

