# Event Registration API

ASP.NET Core .NET 8 Web API for managing event registration data with MySQL.

## Tech Stack

- ASP.NET Core .NET 8
- MySQL
- Dapper and MySqlConnector
- MediatR
- FluentValidation
- Serilog
- Swagger/OpenAPI

## Prerequisites

- .NET 8 SDK
- MySQL Server
- A database created for the API

## Environment Variables

Create a local `.env` file from `.env.example` and set your database connection string:

```env
DB_CONNECTION_STRING=server=localhost;port=3306;database=eventreg;user=root;password=your_password_here;
```

Do not commit `.env` or real secrets. The application reads `DB_CONNECTION_STRING` from the environment.

## Restore Packages

```powershell
dotnet restore
```

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run
```

Swagger is available at:

```text
http://localhost:5000/swagger
```

The API base URL is:

```text
http://localhost:5000/api
```

The exact local port can vary based on `Properties/launchSettings.json`.

## Main Endpoints

Categories:

- `GET /api/categories`
- `GET /api/categories/{id}`
- `POST /api/categories`
- `PUT /api/categories/{id}`
- `DELETE /api/categories/{id}`

Events:

- Event data is stored in the `Events` table and used by registration workflows.

Participants:

- `GET /api/participants?page=1&pageSize=10&search=&isActive=`
- `GET /api/participants/{id}`
- `POST /api/participants`
- `PUT /api/participants/{id}`
- `DELETE /api/participants/{id}`

Event registrations:

- `GET /api/events/{eventId}/registrations?page=1&pageSize=10&search=&status=&participantId=`
- `POST /api/events/{eventId}/registrations`
- `PATCH /api/events/{eventId}/registrations/{registrationId}/cancel`

Registration deletion is intentionally not exposed. Cancelling a registration is the supported behavior.
