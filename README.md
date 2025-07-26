# Beverage Distributor System

A .NET 8.0-based system for managing beverage distribution orders, built with Clean Architecture principles.

## Project Structure

- **BeverageDistributor.API**: ASP.NET Core Web API project (Presentation Layer)
- **BeverageDistributor.Application**: Application layer with use cases and business logic
- **BeverageDistributor.Domain**: Domain models, entities, and interfaces
- **BeverageDistributor.Infrastructure**: Infrastructure concerns (data access, external services)
- **BeverageDistributor.Tests**: Unit and integration tests

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL 13 or later
- Docker (for RabbitMQ and optional PostgreSQL)

## Getting Started

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd beverage-distributor
   ```

2. **Set up the database**
   - Ensure PostgreSQL is running
   - Update the connection string in `BeverageDistributor.API/appsettings.json`

3. **Run database migrations**
   ```bash
   cd BeverageDistributor.API
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run --project BeverageDistributor.API
   ```

5. **Access the API documentation**
   - Swagger UI: https://localhost:5001/swagger
   - Swagger JSON: https://localhost:5001/swagger/v1/swagger.json

## Running with Docker

1. Start the infrastructure services:
   ```bash
   docker-compose -f docker-compose.infrastructure.yml up -d
   ```

2. Run the application:
   ```bash
   dotnet run --project BeverageDistributor.API
   ```

## Testing

Run the unit tests:
```bash
dotnet test
```

## Architecture

The application follows Clean Architecture principles with the following layers:

- **Domain Layer**: Contains enterprise-wide business rules and entities
- **Application Layer**: Contains application-specific business rules and use cases
- **Infrastructure Layer**: Contains implementation details (database, external services)
- **API Layer**: Contains the Web API and presentation logic

## Technologies Used

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core 8.0
- PostgreSQL
- FluentValidation
- Swagger/OpenAPI
- xUnit (testing)
- Moq (mocking)
- RabbitMQ (messaging)
- Polly (resilience)
