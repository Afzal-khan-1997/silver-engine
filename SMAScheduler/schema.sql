IF OBJECT_ID('dbo.SmaScheduleTasks', 'U') IS NOT NULL
    DROP TABLE dbo.SmaScheduleTasks;

IF OBJECT_ID('dbo.SmaScheduleProjects', 'U') IS NOT NULL
    DROP TABLE dbo.SmaScheduleProjects;

CREATE TABLE dbo.SmaScheduleProjects
(
    ProjectId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProjectName NVARCHAR(200) NOT NULL UNIQUE,
    ProjectVersion NVARCHAR(50) NOT NULL DEFAULT '1.0',
    TotalProjectHours DECIMAL(12,2) NOT NULL DEFAULT 0,
    ResourcesNeeded INT NOT NULL DEFAULT 0,
    ResourceHours DECIMAL(12,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.SmaScheduleTasks
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProjectId INT NOT NULL,
    TaskId INT NOT NULL,
    DatabaseTaskId INT NULL,
    TaskName NVARCHAR(300) NOT NULL,
    StartDate DATE NOT NULL,
    DurationDays DECIMAL(12,3) NOT NULL,
    FinishDate DATE NOT NULL,
    PercentComplete INT NOT NULL,
    Predecessors NVARCHAR(200) NULL,
    AssignedTo NVARCHAR(200) NULL,
    AssignmentDate DATE NULL,
    ResourceNames NVARCHAR(500) NULL,
    ResourceAllocations NVARCHAR(1000) NULL,
    ResourceHours DECIMAL(12,2) NOT NULL DEFAULT 0,
    ModuleId INT NULL,
    PlannerTaskId NVARCHAR(200) NULL,
    CONSTRAINT FK_SmaScheduleTasks_Project FOREIGN KEY(ProjectId) REFERENCES dbo.SmaScheduleProjects(ProjectId),
    CONSTRAINT UQ_SmaScheduleTasks_Project_Task UNIQUE(ProjectId, TaskId)
);
