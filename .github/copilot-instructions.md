# Funda Makelaars Analysis Project

## Project Overview
This is a .NET 8.0 console application that analyzes Funda real estate API data to determine top makelaars in Amsterdam.

## Architecture & Guidelines
- Uses .NET 8.0 with dependency injection
- Implements SOLID principles and clean code practices
- Uses Polly for retry policies and rate limiting
- Follows separation of concerns with service-based architecture
- Comprehensive error handling and logging

## Key Components
- **FundaApiService**: API communication with resilience patterns
- **MakelaarAnalysisService**: Data analysis and ranking logic
- **ResultDisplayService**: Console output formatting
- **Configuration**: Externalized settings via appsettings.json

## Development Commands
- Build: `dotnet build`
- Run: `dotnet run --project Funda.MakelaarsReport`
- Use VS Code tasks for build and run operations

## Code Style
- Follow C# conventions and best practices
- Use dependency injection for all services
- Implement proper error handling and logging
- Keep services focused and testable