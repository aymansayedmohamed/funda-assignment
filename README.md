# Funda Real Estate Agents Report Tool

A .NET 8.0 console application that analyzes real estate data from the Funda API to determine which real estate agents in Amsterdam have the most properties listed for sale.

## Overview

This application fetches property data from the Funda API and generates two reports:
1. **Top 10 real estate agents** with the most properties in Amsterdam
2. **Top 10 real estate agents** with the most properties with gardens in Amsterdam

## Features

- **Rate Limiting**: Respects the API's 100 requests per minute limit
- **Retry Policies**: Uses Polly for resilient HTTP requests with exponential backoff
- **Dependency Injection**: Clean architecture with proper separation of concerns
- **Error Handling**: Robust error handling for network issues and data anomalies
- **Comprehensive Logging**: Detailed logging throughout the application
- **SOLID Principles**: Follows clean code practices and SOLID principles

## Architecture

### Core Services

- **IFundaApiService**: Handles API communication with retry policies and rate limiting
- **IRealEstateAgentReportService**: Processes data and generates top 10 rankings
- **IResultDisplayService**: Formats and displays results to console

### Models

- **FundaApiResponse**: Maps the Funda API JSON response
- **FundaObject**: Represents individual property listings
- **RealEstateAgentRanking**: Contains ranking data for real estate agents
- **ReportResult**: Comprehensive report results

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "FundaApi": {
    "ApiKey": "76666a29898f491480386d966b75f949",
    "BaseUrl": "http://partnerapi.funda.nl/feeds/Aanbod.svc/json",
    "PageSize": 25,
    "MaxRequestsPerMinute": 100,
    "RetryAttempts": 3,
    "RetryDelay": "00:00:02",
    "HttpClientTimeout": "00:00:30"
  }
}
```

## Building and Running

### Prerequisites

- .NET 8.0 SDK
- Visual Studio Code (recommended) with C# Dev Kit extension

### Build the Application

```bash
dotnet build
```

### Run the Application

```bash
dotnet run --project Funda.RealEstateAgentsReport
```

Or use the VS Code tasks:
- **Build Funda Report**: `Ctrl+Shift+P` → `Tasks: Run Task` → `Build Funda Report`
- **Run Funda Report**: `Ctrl+Shift+P` → `Tasks: Run Task` → `Run Funda Report`

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run only unit tests
dotnet test --filter "Category!=Integration"

# Run only integration tests
dotnet test --filter "Category=Integration"
```

## Technical Implementation

### Rate Limiting Strategy

The application implements a configurable rate limiting mechanism that calculates delays based on the `MaxRequestsPerMinute` setting to respect the Funda API's 100 requests per minute limit.

### Sequential vs Parallel Execution

The application **intentionally executes reports sequentially** rather than in parallel for the following architectural reasons:

- **API Rate Limiting**: Running reports in parallel would effectively double the request rate (~200 req/min), violating Funda's 100 req/min limit
- **Resource Management**: Sequential execution prevents memory pressure from loading multiple large datasets simultaneously  
- **Semaphore Design**: The `FundaApiService` uses `SemaphoreSlim(1,1)` ensuring only one concurrent API call, making parallel execution ineffective
- **User Experience**: Progressive feedback allows users to see results as they become available
- **API Courtesy**: Respectful usage patterns prevent potential API key blocking

**Result**: Sequential execution provides optimal performance while maintaining reliability and API compliance.

### Error Handling

- **Polly Retry Policies**: Automatic retries for transient HTTP errors
- **Circuit Breaker**: Prevents cascading failures
- **Nullable Properties**: Handles data inconsistencies (e.g., properties without prices)
- **Comprehensive Logging**: Detailed error logging and progress tracking

### Data Processing

1. Fetches all pages of data for each search query
2. Aggregates properties by real estate agent
3. Sorts by property count (descending)
4. Returns top 10 real estate agents with rankings

## Sample Output

```
================================================================================
FUNDA REAL ESTATE AGENTS REPORT
================================================================================
Report Generated: 2025-10-23 14:30:45 UTC

================================================================================
TOP 10 REAL ESTATE AGENTS - ALL PROPERTIES IN AMSTERDAM
================================================================================
Search Query: /amsterdam/
Total Objects Found: 5,200
Report Date: 2025-10-23 14:30:45 UTC
Processing Time: 127.35 seconds

Rank   Real Estate Agent Name                            Properties   
----------------------------------------------------------------------
1      ERA Makelaardij Amsterdam                          156       
2      Hoekstra en van Eck Makelaars                     142       
3      Broersma Makelaardij                              138       
...
```

## Dependencies

- **Microsoft.Extensions.Hosting** (9.0.10): For dependency injection and hosting
- **Microsoft.Extensions.Http.Polly** (9.0.10): HTTP client with Polly integration
- **Polly** (8.6.4): Resilience and transient-fault-handling library

### Test Dependencies

- **xUnit** (2.9.3): Unit testing framework
- **Moq** (4.20.72): Mocking framework for unit tests
- **Microsoft.AspNetCore.Mvc.Testing** (9.0.10): Integration testing framework

## Project Structure

```
Funda.RealEstateAgentsReport/
├── Configuration/
│   └── FundaApiOptions.cs
├── Models/
│   ├── FundaApiModels.cs
│   └── ReportModels.cs
├── Services/
│   ├── Contracts/
│   │   └── IServices.cs
│   ├── FundaApiService.cs
│   ├── RealEstateAgentReportService.cs
│   ├── ResultDisplayService.cs
│   └── FundaReportApplication.cs
├── Program.cs
├── appsettings.json
└── Funda.RealEstateAgentsReport.csproj

Funda.RealEstateAgentsReport.Tests/
├── UnitTests/
│   ├── FundaApiServiceTests.cs
│   ├── RealEstateAgentReportServiceTests.cs
│   └── ResultDisplayServiceTests.cs
├── IntegrationTests/
│   └── FundaReportApplicationIntegrationTests.cs
├── TestData/
│   └── sample-api-response.json
└── Funda.RealEstateAgentsReport.Tests.csproj
```

## Testing

The project includes comprehensive unit and integration tests:

### Unit Tests
- **FundaApiServiceTests**: Tests API communication, rate limiting, retry logic, and JSON deserialization
- **RealEstateAgentReportServiceTests**: Tests data aggregation, ranking logic, and error handling
- **ResultDisplayServiceTests**: Tests console output formatting and display logic

### Integration Tests
- **FundaReportApplicationIntegrationTests**: Tests full application workflow, dependency injection, configuration binding, and service interaction

### Test Coverage
- **25 total tests** covering all major functionality
- **Mock-based unit tests** for isolated component testing
- **Integration tests** for end-to-end scenarios
- **Error handling tests** for robustness validation
- **Configuration tests** for proper setup verification

## Assignment Requirements

This application fulfills all the requirements from the original assignment:

✅ **Object-oriented language**: Built with C# (.NET 8.0)  
✅ **Top 10 real estate agents in Amsterdam**: Generates ranking for all properties  
✅ **Top 10 real estate agents with gardens**: Generates ranking for properties with "tuin"  
✅ **Rate limiting**: Handles >100 requests per minute limitation  
✅ **Error handling**: Comprehensive error handling and resilience  
✅ **Readable code**: Clean architecture with clear separation of concerns  
✅ **Testable design**: Dependency injection and interface-based design  

## AI-Assisted Development

This project was completed as a coding assignment with assistance from GitHub Copilot AI. Below is a transparent disclosure of where AI was used during development:

### AI Usage Declaration

**AI Tool Used**: GitHub Copilot  
**Development Approach**: AI-augmented development with human oversight and decision-making

### Specific Areas Where AI Was Used

- **Initial Project Setup**: AI assisted with creating the .NET 8.0 project structure and dependency injection configuration
- **Boilerplate Code Generation**: AI generated model classes, service interfaces, and basic service implementations
- **API Integration**: AI helped implement the Funda API client with proper JSON deserialization and rate limiting
- **Code Refactoring**: AI assisted in renaming Dutch terms to English for better code readability
- **Documentation**: AI helped generate this README and inline code comments

### Human Decisions and Oversight

- **Business Logic**: All report logic and ranking algorithms
- **Design Decisions**: Architectural choices (sequential vs parallel execution, rate limiting strategy) 
- **Code Review**: All AI-generated code was reviewed, tested, and modified 
- **Problem Solving**: Debugging and troubleshooting were primarily human-driven with AI assistance
- **Quality Assurance**: Testing approach and error scenarios were identified and handled 

### Development Methodology

The development followed a collaborative approach where:
1. Human defined requirements and constraints
2. AI provided technical suggestions and code generation
3. Human reviewed, modified, and validated all implementations
4. AI assisted with optimization and best practice recommendations
5. Human made final decisions on architecture and implementation details

This approach demonstrates responsible AI usage in software development while maintaining code quality and meeting all assignment requirements.

## Development Notes

- The application is designed to be easily testable with mock services
- Configuration is externalized and environment-specific
- Logging provides detailed insights into the processing flow
- The architecture supports easy extension for additional analysis types
- All SOLID principles are followed for maintainable code