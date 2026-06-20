# SMA Scheduler

SMA Scheduler is a colorful VB.NET Windows Forms planner inspired by the core Microsoft Project workflow.

## What is included

- Editable task sheet with task name, duration, start, finish, percent complete, predecessors, resources, and Planner task id.
- Project planning fields for project name, total project hours, resources needed, and hours per resource.
- Project version field, ready to be populated from SQL when the project table is connected.
- Database task selection from a SQL-ready task catalog.
- Employee assignment dropdown using the employee names provided from the database screenshot.
- Task assignment fields for assigned employee, assignment date, and planned resource hours.
- Excel export from the Save button using `ProjectName_Version.xlsx`.
- Monthly resource-hours Excel export with employee/date allocation, 8-hour daily cap, Sunday holidays, and optional Saturdays.
- Automatic finish-date calculation from start date and duration.
- Finish-to-start dependency behavior using predecessor task ids.
- Gantt-style visual timeline with weekend shading, today marker, progress bars, milestones, and dependency arrows.
- Local open/save support using `.smaschedule` files.
- Project summary strip for task count, schedule dates, average progress, and resources.
- Microsoft Planner sync preview surface, ready to connect to Microsoft Graph.
- SQL-ready repository and schema for later database storage.

## Run

```powershell
dotnet run --project .\SMAScheduler.vbproj
```

## Project Files

Use **Open** and **Save** in the app toolbar to work with local `.smaschedule` files. This keeps the current version usable without SQL Server setup.

## SQL Server

SQL support is kept in `SqlProjectRepository.vb` and `schema.sql`. The schema already includes project version, project hours, resources needed, resource hours, database task id, module id, assigned employee, assignment date, and task resource hours so SQL storage can be enabled when the SQL task-name and employee tables are available.

When SQL is needed later, the planned tables are:

- `dbo.SmaScheduleProjects`
- `dbo.SmaScheduleTasks`

The same schema is available in `schema.sql` if you want to create it manually.

The default connection string is:

```text
Server=.;Database=SMAScheduler;Trusted_Connection=True;TrustServerCertificate=True
```

For .NET 9, install the SQL Server provider package before enabling SQL actions:

```powershell
dotnet add package Microsoft.Data.SqlClient
```

## Microsoft Planner

The Planner button currently shows the task payload that should be sent to Microsoft Planner. To make it live, register an Azure application, grant Microsoft Graph Planner permissions, and replace `PlannerSyncService` with calls to the Planner endpoints.
