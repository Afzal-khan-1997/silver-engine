Imports System.ComponentModel
Imports System.Data
Imports System.Reflection

Public Class SqlProjectRepository
    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    Public Sub TestConnection()
        Using connection = CreateConnection()
            connection.Open()
        End Using
    End Sub

    Public Sub SaveProject(projectName As String, tasks As BindingList(Of ScheduleTask), Optional projectVersion As String = "1.0", Optional totalProjectHours As Decimal = 0D, Optional resourcesNeeded As Integer = 0, Optional resourceHours As Decimal = 0D)
        Using connection = CreateConnection()
            connection.Open()
            EnsureSchema(connection)

            Dim projectId = GetOrCreateProject(connection, projectName, projectVersion, totalProjectHours, resourcesNeeded, resourceHours)
            Execute(connection, "DELETE FROM dbo.SmaScheduleTasks WHERE ProjectId = @ProjectId", New Dictionary(Of String, Object) From {{"@ProjectId", projectId}})

            For Each task In tasks
                Execute(connection,
                        "INSERT INTO dbo.SmaScheduleTasks (ProjectId, TaskId, DatabaseTaskId, TaskName, StartDate, DurationDays, FinishDate, PercentComplete, Predecessors, AssignedTo, AssignmentDate, ResourceNames, ResourceAllocations, ResourceHours, ModuleId, PlannerTaskId) VALUES (@ProjectId, @TaskId, @DatabaseTaskId, @TaskName, @StartDate, @DurationDays, @FinishDate, @PercentComplete, @Predecessors, @AssignedTo, @AssignmentDate, @ResourceNames, @ResourceAllocations, @ResourceHours, @ModuleId, @PlannerTaskId)",
                        New Dictionary(Of String, Object) From {
                            {"@ProjectId", projectId},
                            {"@TaskId", task.TaskId},
                            {"@DatabaseTaskId", task.DatabaseTaskId},
                            {"@TaskName", task.TaskName},
                            {"@StartDate", task.StartDate},
                            {"@DurationDays", task.DurationDays},
                            {"@FinishDate", task.FinishDate},
                            {"@PercentComplete", task.PercentComplete},
                            {"@Predecessors", task.Predecessors},
                            {"@AssignedTo", task.AssignedTo},
                            {"@AssignmentDate", task.AssignmentDate},
                            {"@ResourceNames", task.ResourceNames},
                            {"@ResourceAllocations", task.ResourceAllocations},
                            {"@ResourceHours", task.ResourceHours},
                            {"@ModuleId", task.ModuleId},
                            {"@PlannerTaskId", task.PlannerTaskId}
                        })
            Next
        End Using
    End Sub

    Public Function LoadProject(projectName As String) As BindingList(Of ScheduleTask)
        Dim tasks As New BindingList(Of ScheduleTask)
        Using connection = CreateConnection()
            connection.Open()
            EnsureSchema(connection)

            Using command = connection.CreateCommand()
                command.CommandText = "SELECT t.TaskId, t.DatabaseTaskId, t.TaskName, t.StartDate, t.DurationDays, t.FinishDate, t.PercentComplete, t.Predecessors, t.AssignedTo, t.AssignmentDate, t.ResourceNames, t.ResourceAllocations, t.ResourceHours, t.ModuleId, t.PlannerTaskId FROM dbo.SmaScheduleTasks t INNER JOIN dbo.SmaScheduleProjects p ON p.ProjectId = t.ProjectId WHERE p.ProjectName = @ProjectName ORDER BY t.TaskId"
                AddParameter(command, "@ProjectName", projectName)

                Using reader = command.ExecuteReader()
                    While reader.Read()
                        tasks.Add(New ScheduleTask With {
                            .TaskId = CInt(reader("TaskId")),
                            .DatabaseTaskId = If(reader("DatabaseTaskId") Is DBNull.Value, 0, CInt(reader("DatabaseTaskId"))),
                            .TaskName = CStr(reader("TaskName")),
                            .StartDate = CDate(reader("StartDate")),
                            .DurationDays = If(reader("DurationDays") Is DBNull.Value, 0D, CDec(reader("DurationDays"))),
                            .FinishDate = CDate(reader("FinishDate")),
                            .PercentComplete = CInt(reader("PercentComplete")),
                            .Predecessors = If(reader("Predecessors") Is DBNull.Value, "", CStr(reader("Predecessors"))),
                            .AssignedTo = If(reader("AssignedTo") Is DBNull.Value, "", CStr(reader("AssignedTo"))),
                            .AssignmentDate = If(reader("AssignmentDate") Is DBNull.Value, CDate(reader("StartDate")), CDate(reader("AssignmentDate"))),
                            .ResourceNames = If(reader("ResourceNames") Is DBNull.Value, "", CStr(reader("ResourceNames"))),
                            .ResourceAllocations = If(reader("ResourceAllocations") Is DBNull.Value, "", CStr(reader("ResourceAllocations"))),
                            .ResourceHours = If(reader("ResourceHours") Is DBNull.Value, 0D, CDec(reader("ResourceHours"))),
                            .ModuleId = If(reader("ModuleId") Is DBNull.Value, 0, CInt(reader("ModuleId"))),
                            .PlannerTaskId = If(reader("PlannerTaskId") Is DBNull.Value, "", CStr(reader("PlannerTaskId")))
                        })
                    End While
                End Using
            End Using
        End Using

        Return tasks
    End Function

    Private Function CreateConnection() As IDbConnection
        Dim connectionType = ResolveSqlConnectionType()
        Dim connection = DirectCast(Activator.CreateInstance(connectionType), IDbConnection)
        connection.ConnectionString = _connectionString
        Return connection
    End Function

    Private Function ResolveSqlConnectionType() As Type
        For Each name In {"Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient", "System.Data.SqlClient.SqlConnection, System.Data.SqlClient"}
            Dim resolved = Type.GetType(name, False)
            If resolved IsNot Nothing Then
                Return resolved
            End If
        Next

        Throw New InvalidOperationException("No SQL Server provider was found. Add Microsoft.Data.SqlClient to the project or install the SQL Server client provider on this machine.")
    End Function

    Private Sub EnsureSchema(connection As IDbConnection)
        Execute(connection,
                "IF OBJECT_ID('dbo.SmaScheduleProjects', 'U') IS NULL CREATE TABLE dbo.SmaScheduleProjects (ProjectId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, ProjectName NVARCHAR(200) NOT NULL UNIQUE, ProjectVersion NVARCHAR(50) NOT NULL DEFAULT '1.0', TotalProjectHours DECIMAL(12,2) NOT NULL DEFAULT 0, ResourcesNeeded INT NOT NULL DEFAULT 0, ResourceHours DECIMAL(12,2) NOT NULL DEFAULT 0, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME())",
                Nothing)
        Execute(connection,
                "IF OBJECT_ID('dbo.SmaScheduleTasks', 'U') IS NULL CREATE TABLE dbo.SmaScheduleTasks (Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY, ProjectId INT NOT NULL, TaskId INT NOT NULL, DatabaseTaskId INT NULL, TaskName NVARCHAR(300) NOT NULL, StartDate DATE NOT NULL, DurationDays DECIMAL(12,3) NOT NULL, FinishDate DATE NOT NULL, PercentComplete INT NOT NULL, Predecessors NVARCHAR(200) NULL, AssignedTo NVARCHAR(200) NULL, AssignmentDate DATE NULL, ResourceNames NVARCHAR(500) NULL, ResourceAllocations NVARCHAR(1000) NULL, ResourceHours DECIMAL(12,2) NOT NULL DEFAULT 0, ModuleId INT NULL, PlannerTaskId NVARCHAR(200) NULL, CONSTRAINT FK_SmaScheduleTasks_Project FOREIGN KEY(ProjectId) REFERENCES dbo.SmaScheduleProjects(ProjectId), CONSTRAINT UQ_SmaScheduleTasks_Project_Task UNIQUE(ProjectId, TaskId))",
                Nothing)
    End Sub

    Private Function GetOrCreateProject(connection As IDbConnection, projectName As String, projectVersion As String, totalProjectHours As Decimal, resourcesNeeded As Integer, resourceHours As Decimal) As Integer
        Execute(connection,
                "IF NOT EXISTS (SELECT 1 FROM dbo.SmaScheduleProjects WHERE ProjectName = @ProjectName) INSERT INTO dbo.SmaScheduleProjects (ProjectName, ProjectVersion, TotalProjectHours, ResourcesNeeded, ResourceHours) VALUES (@ProjectName, @ProjectVersion, @TotalProjectHours, @ResourcesNeeded, @ResourceHours) ELSE UPDATE dbo.SmaScheduleProjects SET ProjectVersion = @ProjectVersion, TotalProjectHours = @TotalProjectHours, ResourcesNeeded = @ResourcesNeeded, ResourceHours = @ResourceHours, UpdatedAt = SYSUTCDATETIME() WHERE ProjectName = @ProjectName",
                New Dictionary(Of String, Object) From {
                    {"@ProjectName", projectName},
                    {"@ProjectVersion", If(String.IsNullOrWhiteSpace(projectVersion), "1.0", projectVersion)},
                    {"@TotalProjectHours", totalProjectHours},
                    {"@ResourcesNeeded", resourcesNeeded},
                    {"@ResourceHours", resourceHours}
                })

        Using command = connection.CreateCommand()
            command.CommandText = "SELECT ProjectId FROM dbo.SmaScheduleProjects WHERE ProjectName = @ProjectName"
            AddParameter(command, "@ProjectName", projectName)
            Return CInt(command.ExecuteScalar())
        End Using
    End Function

    Private Sub Execute(connection As IDbConnection, sql As String, parameters As Dictionary(Of String, Object))
        Using command = connection.CreateCommand()
            command.CommandText = sql
            If parameters IsNot Nothing Then
                For Each item In parameters
                    AddParameter(command, item.Key, item.Value)
                Next
            End If
            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub AddParameter(command As IDbCommand, name As String, value As Object)
        Dim parameter = command.CreateParameter()
        parameter.ParameterName = name
        parameter.Value = If(value, DBNull.Value)
        command.Parameters.Add(parameter)
    End Sub
End Class
