# 🍽️ Recipe Tracker

> A unified digital experience for recipe storage, nutritional analysis, and grocery planning.

> ⚠️ **Status: Early concept / Work in Progress** — This project is under active development. Features and documentation are subject to change.

---

## Overview

This is the **backend API** for Recipe Tracker — a .NET application built with clean architecture and CQRS patterns. It handles all business logic and data persistence, exposing a REST API consumed by the [React frontend](https://github.com/your-username/recipe-tracker-frontend).

The full application integrates recipe storage, nutritional analysis, and grocery planning into a unified digital experience.

---

## Tech Stack

| | Technology |
|---|---|
| Language | C# / .NET |
| Database | PostgreSQL |

---

## Architecture

The backend follows **Clean Architecture** principles with a **CQRS (Command Query Responsibility Segregation)** pattern, keeping business logic decoupled from infrastructure concerns and making the codebase easy to test and extend.

| Layer | Responsibility |
|---|---|
| **API** | HTTP entry point — controllers, middleware, dependency injection setup |
| **Application** | Use cases — commands, queries, handlers, and DTOs |
| **Domain** | Core business logic — entities, value objects, domain events |
| **Infrastructure** | External integrations — third-party services, email, etc. |
| **Persistence** | Data access — EF Core DbContext, repositories, migrations |

---

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (v7+)
- [PostgreSQL](https://www.postgresql.org/) (v17+)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/jnbofori/Prepora.git
   cd Prepora
   ```

2. **Set up the database**
   ```bash
   createdb prepora
   ```

3. **Configure environment variables**
   ```bash
   cp appsettings.example.json appsettings.Development.json
   # Update the connection string with your PostgreSQL credentials
   ```

4. **Run the API**
   ```bash
   cd API
   dotnet restore
   dotnet run
   ```

> The frontend repo is available [here](https://github.com/your-username/recipe-tracker-frontend).

---

## Project Structure

```
recipe-tracker-backend/
├── API/                   # Entry point — controllers, middleware, DI
├── Application/           # CQRS handlers, commands, queries, DTOs
├── Domain/                # Entities, value objects, domain events
├── Infrastructure/        # External services and integrations
└── Persistence/           # EF Core, repositories, migrations
```

---

## Roadmap

- [x] User authentication
- [ ] Recipe CRUD (create, read, update, delete)
- [ ] Recipe import from URL
- [ ] Ingredient and nutritional data integration
- [ ] Grocery list generation
- [ ] Meal planning calendar

---

## License

This project is licensed under the [MIT License](LICENSE).
