# Remaining localization work — split for 2 developers

## Current state

Localization infrastructure is fully built and working (EN/RU, cookie-based
switcher in `_Layout.cshtml`, `IStringLocalizer<SharedResource>` injected
globally via both `_ViewImports.cshtml` files). **196 keys** already exist in
`Resources/SharedResource.resx` / `Resources/SharedResource.ru.resx`.

Already localized (do not touch, already done and pushed):
- `Views/Auth/*`, `Views/Home/*`
- `Areas/Admin/Views/Dashboard`, `Areas/Teacher/Views/Dashboard`, `Areas/Parent/Views/Dashboard`
- `Areas/Admin/Views/Groups`, `Areas/Admin/Views/Students`, `Areas/Admin/Views/Parents` (views only — their controllers' TempData messages are NOT done yet, see Part A)

**Remaining: 74 files** — 55 views + 12 controllers (TempData) + 7 ViewModels
(validation messages). Split below into two independent, low-conflict parts.

## Before you start — read this

1. **The pattern**: open any already-localized file (e.g.
   `Areas/Admin/Views/Groups/Index.cshtml`) as your template. Every
   user-facing string becomes `@Localizer["Exact English Text"]`. For strings
   with a value inside them use the format-string form:
   `@Localizer["Showing {0} group(s)", Model.Count()]`.
2. **Reuse before adding.** Many keys you need already exist (`Edit`, `Delete`,
   `Cancel`, `Save Changes`, `Search`, `Clear`, `Back`, `Yes, Delete`,
   `Are you sure you want to delete`, `Actions`, `View`, `Details`, `Group`,
   `Subject`, `Teacher`, `Student`, `No group`, etc.). **Grep
   `Resources/SharedResource.resx` for your string before adding a new key.**
3. **resx keys are case-insensitive.** `"View All"` and `"View all"` collide
   and fail the build (`MSB3568`). If you get that warning, rename one of the
   two colliding keys to something distinct.
4. **Controller messages**: inject `IStringLocalizer<SharedResource>` into the
   controller (see `Controllers/AuthController.cs` for the pattern — add
   `using SchoolManagementSystem.Web;` and `using
   Microsoft.Extensions.Localization;`), then wrap:
   `TempData["Success"] = _localizer["Lesson created successfully."];`
5. **ValidationMessages (ViewModels)**: `AddDataAnnotationsLocalization` is
   already wired up in `Program.cs` — just add a resx entry whose key is the
   *exact* literal `ErrorMessage` text and it's picked up automatically. No
   code change needed in the ViewModel itself.
6. Every entry goes into **both** `SharedResource.resx` (English — value =
   the key, unchanged) **and** `SharedResource.ru.resx` (Russian translation).
7. **Build after every file or two**: `dotnet build`. Catches Razor syntax
   errors and resx collisions immediately.
8. Follow `.claude/CLAUDE.md`: commit only the files for what you just did,
   with a clear message, then `git push origin main`.

## ⚠️ Git conflict warning (read before merging)

Both parts edit the **same two resx files**. If you both work in parallel and
push at different times, whoever pushes second will likely get a merge
conflict right at the end of the file (near `</root>`) — this is a normal,
easy conflict (both sides just added new `<data>` blocks): keep both blocks,
don't discard either side. Recommended flow:
- Push in small batches (after each entity/area), not one giant commit at the
  end — smaller diffs mean smaller, easier conflicts.
- Whoever pushes second runs `git pull --rebase origin main` before pushing,
  resolves the resx conflict by keeping both additions, then continues.

---

## Part A — Admin core entities (~35 files)

**Scope: Teachers, Subjects, Lessons, Schedule views, plus the TempData
messages for Groups/Parents/Students/Teachers/Subjects/Lessons/Schedule
controllers (their views are already localized, only the controller messages
remain).**

### Views (24 files) — use `Areas/Admin/Views/Students/*` as your template

- `Areas/Admin/Views/Teachers/{Index,Create,Edit,Delete,Details,_Form}.cshtml`
- `Areas/Admin/Views/Subjects/{Index,Create,Edit,Delete,Details,_Form}.cshtml`
- `Areas/Admin/Views/Lessons/{Index,Create,Edit,Delete,Details,_Form}.cshtml`
- `Areas/Admin/Views/Schedule/{Index,Create,Edit,Delete,Details,_Form}.cshtml`

### Controllers — TempData messages (7 files, 25 messages)

- `Areas/Admin/Controllers/GroupsController.cs` (4 messages)
- `Areas/Admin/Controllers/ParentsController.cs` (3)
- `Areas/Admin/Controllers/StudentsController.cs` (3)
- `Areas/Admin/Controllers/TeachersController.cs` (4)
- `Areas/Admin/Controllers/SubjectsController.cs` (4)
- `Areas/Admin/Controllers/LessonsController.cs` (4)
- `Areas/Admin/Controllers/ScheduleController.cs` (3)

### ViewModels — validation messages (4 files)

- `ViewModels/Admin/GroupFormViewModel.cs`
- `ViewModels/Admin/SubjectFormViewModel.cs`
- `ViewModels/Admin/LessonFormViewModel.cs`
- `ViewModels/Admin/ScheduleFormViewModel.cs`

### Verification

- Log in as `admin@school.com` / `Admin123!`, click through every page above
  in both EN and RU, switch language via the topbar switcher.
- Trigger at least one success message (e.g. edit a lesson) and one
  validation error (e.g. submit an empty required field) in RU to confirm the
  controller/ViewModel messages localize too.

---

## Part B — Attendance/Grades everywhere + Teacher & Parent portals (~39 files)

**Scope: Admin's Attendance/Grades/Salary, all of the Teacher area, all of
the Parent area, and their controllers' TempData/validation messages.**

### Admin views (8 files) — use `Areas/Admin/Views/Grades/Index.cshtml`
(already partly styled) and the Groups/Students CRUD files as templates

- `Areas/Admin/Views/Attendance/{Index,Edit,Delete,Journal}.cshtml`
- `Areas/Admin/Views/Grades/{Index,Edit,Delete}.cshtml`
- `Areas/Admin/Views/Salary/Index.cshtml`

### Teacher area views (15 files)

- `Areas/Teacher/Views/Attendance/{Index,Journal,Mark}.cshtml`
- `Areas/Teacher/Views/Grades/{Index,Add}.cshtml`
- `Areas/Teacher/Views/Groups/{Index,Details}.cshtml`
- `Areas/Teacher/Views/Lessons/{Index,Details}.cshtml`
- `Areas/Teacher/Views/Students/{Index,Details}.cshtml`
- `Areas/Teacher/Views/Topics/{Index,Edit}.cshtml`
- `Areas/Teacher/Views/Schedule/Index.cshtml`
- `Areas/Teacher/Views/Salary/Index.cshtml`

### Parent area views (8 files)

- `Areas/Parent/Views/Attendance/Index.cshtml`
- `Areas/Parent/Views/Grades/Index.cshtml`
- `Areas/Parent/Views/Schedule/Index.cshtml`
- `Areas/Parent/Views/Subjects/Index.cshtml`
- `Areas/Parent/Views/Topics/Index.cshtml`
- `Areas/Parent/Views/Child/Details.cshtml`
- `Areas/Parent/Views/Progress/Index.cshtml` (also localize the "Your Child
  vs. Group Average" card header added for the chart feature)
- `Areas/Parent/Views/Shared/_LineChart.cshtml` (just the legend text: "Your
  child" / "Group average")

### Controllers — TempData messages (5 files, 7 messages)

- `Areas/Admin/Controllers/AttendanceController.cs` (2)
- `Areas/Admin/Controllers/GradesController.cs` (2)
- `Areas/Teacher/Controllers/AttendanceController.cs` (1)
- `Areas/Teacher/Controllers/GradesController.cs` (1)
- `Areas/Teacher/Controllers/TopicsController.cs` (1)

### ViewModels — validation messages (3 files)

- `ViewModels/Admin/GradeFormViewModel.cs`
- `ViewModels/Teacher/GradeEntryViewModel.cs`
- `ViewModels/Auth/LoginViewModel.cs`

### Verification

- Log in as `teacher@school.com` / `Teacher123!` and `parent@school.com` /
  `Parent123!` (plus `admin@school.com` for the Attendance/Grades/Salary
  pages), click through every page above in both EN and RU.
- Specifically check the Attendance Journal grid and the Progress chart —
  they have the most custom markup (status badges, SVG legend).

---

## After both parts are merged

1. `dotnet build` and `dotnet test Tests/SchoolManagementSystem.Web.Tests`
   must both be clean.
2. Spot-check one page per portal in both languages one more time after the
   merge, since a resx conflict resolution is the one place a typo could slip
   in unnoticed.
3. Delete this file (`LOCALIZATION_PLAN.md`) once the sweep is complete — it's
   a working checklist, not permanent documentation.
