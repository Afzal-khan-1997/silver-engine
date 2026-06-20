Imports System.ComponentModel
Imports System.Text

Public Class PlannerSyncService
    Public Function BuildSyncPreview(planName As String, tasks As BindingList(Of ScheduleTask)) As String
        Dim builder As New StringBuilder()
        builder.AppendLine("Microsoft Planner sync preview")
        builder.AppendLine("Plan: " & If(String.IsNullOrWhiteSpace(planName), "(not configured)", planName.Trim()))
        builder.AppendLine()

        For Each task In tasks
            builder.AppendLine(String.Format("#{0} DB:{1} {2} | {3:yyyy-MM-dd} to {4:yyyy-MM-dd} | Assigned: {5} on {6:yyyy-MM-dd} | Hours: {7} | Planner Id: {8}",
                                             task.TaskId,
                                             If(task.DatabaseTaskId = 0, "-", task.DatabaseTaskId.ToString()),
                                             task.TaskName,
                                             task.StartDate,
                                             task.FinishDate,
                                             If(String.IsNullOrWhiteSpace(task.AssignedTo), "Unassigned", task.AssignedTo),
                                             task.AssignmentDate,
                                             task.ResourceHours.ToString("0.##"),
                                             If(String.IsNullOrWhiteSpace(task.PlannerTaskId), "(new)", task.PlannerTaskId)))
        Next

        builder.AppendLine()
        builder.AppendLine("Connect this service to Microsoft Graph Planner endpoints after registering an Azure app and granting Planner.ReadWrite.All.")
        Return builder.ToString()
    End Function
End Class
