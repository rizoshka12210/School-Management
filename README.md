# School Management System

An ASP.NET Core MVC application for managing a school: students, teachers,
parents, groups, subjects, lessons, schedules, attendance and grades, with
a separate portal for each role.

## Tech stack

- **.NET 10 / ASP.NET Core MVC** (Areas per role: `Admin`, `Teacher`, `Parent`)
- **EF Core 10** with the **Npgsql** provider (PostgreSQL)
- **ASP.NET Core Identity** for authentication, with three roles: `Admin`,
  `Teacher`, `Parent`
- **xUnit** (`Tests/SchoolManagementSystem.Web.Tests`) with the EF Core
  InMemory provider

## Setup

1. **Install PostgreSQL** and create a database (defaults to
   `school_management_db`, see `appsettings.json`).

2. **Configure the connection string.** `appsettings.json` only ships a
   placeholder password. Set the real one via
   [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
   (already wired up via `UserSecretsId` in the `.csproj`) so it never ends
   up committed:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
     "Host=localhost;Port=5432;Database=school_management_db;Username=postgres;Password=<your-password>"
   ```

3. **Apply migrations / create the schema:**

   ```bash
   dotnet ef database update
   ```

4. **Run the app:**

   ```bash
   dotnet run
   ```

   On startup, `Data/Seed/DbSeeder.cs` seeds the three roles, an admin
   account, a demo teacher/parent, and a month of demo lessons/attendance/
   grades (skipped once the data already exists, so it's safe to restart).

5. **Run the tests:**

   ```bash
   dotnet test Tests/SchoolManagementSystem.Web.Tests
   ```

## Demo accounts

Seeded by `DbSeeder.cs` for local development:

| Role    | Email                  | Password      |
|---------|-------------------------|---------------|
| Admin   | `admin@school.com`      | `Admin123!`   |
| Teacher | `teacher@school.com`    | `Teacher123!` |
| Teacher | `teacher2@school.com`   | `Teacher123!` |
| Parent  | `parent@school.com`     | `Parent123!`  |
| Parent  | `parent2@school.com`    | `Parent123!`  |

The second teacher/parent exist specifically so the "someone else's
data" case can be exercised (see `OwnershipHelper` and the IDOR tests in
`Tests/SchoolManagementSystem.Web.Tests/OwnershipHelperTests.cs`).

## Architecture

- **Areas** (`Areas/Admin`, `Areas/Teacher`, `Areas/Parent`) split the app
  by role. Admin has full CRUD over every entity; Teacher and Parent
  controllers are scoped to the signed-in user's own data.
- **`Authorization/OwnershipHelper.cs`** is the single source of truth for
  "does this teacher own this lesson/group/student" and "does this parent
  own this student" — every Teacher/Parent controller goes through it
  before touching data, which is what prevents IDOR (one user reading or
  editing another user's records).
- **`Services/`** hold the handful of pieces of real business logic
  (salary calculation from lesson hours, attendance/grade queries, topic
  calendars) shared across controllers.
- **Salary is computed, not stored** — see the doc comment on
  `Services/SalaryService.cs` for why.
