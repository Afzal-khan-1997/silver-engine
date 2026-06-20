Imports System.ComponentModel

Public Class ScheduleEngine
    Public Property IncludeSaturdays As Boolean

    Public Sub Recalculate(tasks As BindingList(Of ScheduleTask))
        If tasks Is Nothing Then
            Return
        End If

        For Each task In tasks
            task.DurationDays = Math.Max(0D, task.DurationDays)
            task.PercentComplete = Math.Min(100, Math.Max(0, task.PercentComplete))
            task.StartDate = NextWorkingDate(task.StartDate)
            task.AssignmentDate = NextWorkingDate(task.AssignmentDate)
        Next

        Dim byId As New Dictionary(Of Integer, ScheduleTask)
        For Each task In tasks
            If task.TaskId > 0 AndAlso Not byId.ContainsKey(task.TaskId) Then
                byId.Add(task.TaskId, task)
            End If
        Next

        Dim usedHours As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)

        For Each task In tasks
            Dim suggestedStart = NextWorkingDate(task.StartDate)
            For Each predecessorId In ParsePredecessors(task.Predecessors)
                If predecessorId <> task.TaskId AndAlso byId.ContainsKey(predecessorId) Then
                    Dim predecessor = byId(predecessorId)
                    Dim predecessorReadyDate = NextWorkingDate(predecessor.FinishDate)
                    If predecessorReadyDate > suggestedStart Then
                        suggestedStart = predecessorReadyDate
                    End If
                End If
            Next

            suggestedStart = FindStartWithCapacity(suggestedStart, task, usedHours)
            Dim suggestedFinish = AllocateTaskHours(task, suggestedStart, usedHours)
            task.StartDate = suggestedStart
            task.FinishDate = suggestedFinish
        Next
    End Sub

    Public Function ParsePredecessors(value As String) As List(Of Integer)
        Dim result As New List(Of Integer)
        If String.IsNullOrWhiteSpace(value) Then
            Return result
        End If

        For Each part In value.Split({","c, ";"c, " "c}, StringSplitOptions.RemoveEmptyEntries)
            Dim parsed As Integer
            If Integer.TryParse(part.Trim(), parsed) AndAlso parsed > 0 AndAlso Not result.Contains(parsed) Then
                result.Add(parsed)
            End If
        Next

        Return result
    End Function

    Private Function AddWorkingDays(startDate As Date, durationDays As Decimal) As Date
        Dim currentDate = NextWorkingDate(startDate)
        Dim remainingDays = Math.Max(1, CInt(Math.Ceiling(durationDays)))

        While remainingDays > 1
            currentDate = currentDate.AddDays(1)
            If Not IsBlockedDate(currentDate) Then
                remainingDays -= 1
            End If
        End While

        Return currentDate
    End Function

    Private Function FindStartWithCapacity(startDate As Date, task As ScheduleTask, usedHours As Dictionary(Of String, Decimal)) As Date
        Dim candidate = NextWorkingDate(startDate)
        Dim assignments = ResourceAssignments(task)
        If assignments.Count = 0 Then
            Return candidate
        End If

        For guard = 0 To 365
            If assignments.All(Function(item) item.Value <= 0D OrElse AvailableHours(item.Key, candidate, usedHours) > 0D) Then
                Return candidate
            End If

            candidate = NextWorkingDate(candidate.AddDays(1))
        Next

        Return candidate
    End Function

    Private Function AllocateTaskHours(task As ScheduleTask, startDate As Date, usedHours As Dictionary(Of String, Decimal)) As Date
        Dim finishDate = AddWorkingDays(startDate, task.DurationDays)
        Dim assignments = ResourceAssignments(task)
        If assignments.Count = 0 Then
            Return finishDate
        End If

        For Each assignment In assignments
            Dim remainingHours = assignment.Value
            Dim currentDate = NextWorkingDate(startDate)
            Dim lastWorkDate = currentDate
            Dim guard = 0

            While remainingHours > 0D AndAlso guard < 366
                guard += 1
                Dim available = AvailableHours(assignment.Key, currentDate, usedHours)
                If available > 0D Then
                    Dim allocated = Math.Min(remainingHours, available)
                    AddUsedHours(assignment.Key, currentDate, allocated, usedHours)
                    remainingHours -= allocated
                    lastWorkDate = currentDate
                End If

                If remainingHours > 0D Then
                    currentDate = NextWorkingDate(currentDate.AddDays(1))
                End If
            End While

            If lastWorkDate > finishDate Then
                finishDate = lastWorkDate
            End If
        Next

        Return finishDate
    End Function

    Private Function ResourceAssignments(task As ScheduleTask) As Dictionary(Of String, Decimal)
        Dim result As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        If task Is Nothing Then
            Return result
        End If

        If Not String.IsNullOrWhiteSpace(task.ResourceAllocations) Then
            For Each part In task.ResourceAllocations.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
                Dim pieces = part.Split({"="c}, 2, StringSplitOptions.None)
                If pieces.Length = 2 Then
                    Dim hours As Decimal
                    If Decimal.TryParse(pieces(1).Trim(), Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, hours) OrElse
                        Decimal.TryParse(pieces(1).Trim(), hours) Then
                        Dim employeeName = pieces(0).Trim()
                        If employeeName.Length > 0 Then
                            result(employeeName) = Math.Max(0D, hours)
                        End If
                    End If
                End If
            Next
        End If

        If result.Count > 0 Then
            Return result
        End If

        Dim resourceValue = If(String.IsNullOrWhiteSpace(task.ResourceNames), task.AssignedTo, task.ResourceNames)
        Dim names = resourceValue.Split({","c, ";"c}, StringSplitOptions.RemoveEmptyEntries).
            Select(Function(name) name.Trim()).
            Where(Function(name) name.Length > 0).
            Distinct(StringComparer.OrdinalIgnoreCase).
            ToList()
        If names.Count = 0 Then
            Return result
        End If

        Dim hoursPerResource = If(names.Count = 0, 0D, task.ResourceHours / names.Count)
        For Each name In names
            result(name) = hoursPerResource
        Next

        Return result
    End Function

    Private Function AvailableHours(employeeName As String, currentDate As Date, usedHours As Dictionary(Of String, Decimal)) As Decimal
        If IsBlockedDate(currentDate) Then
            Return 0D
        End If

        Dim used As Decimal = 0D
        usedHours.TryGetValue(CapacityKey(employeeName, currentDate), used)
        Return Math.Max(0D, 8D - used)
    End Function

    Private Sub AddUsedHours(employeeName As String, currentDate As Date, hours As Decimal, usedHours As Dictionary(Of String, Decimal))
        Dim key = CapacityKey(employeeName, currentDate)
        Dim existing As Decimal = 0D
        usedHours.TryGetValue(key, existing)
        usedHours(key) = existing + Math.Max(0D, hours)
    End Sub

    Private Function CapacityKey(employeeName As String, currentDate As Date) As String
        Return employeeName.Trim() & "|" & currentDate.ToString("yyyyMMdd", Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Function NextWorkingDate(value As Date) As Date
        Dim currentDate = value.Date
        While IsBlockedDate(currentDate)
            currentDate = currentDate.AddDays(1)
        End While
        Return currentDate
    End Function

    Private Function IsBlockedDate(value As Date) As Boolean
        Return value.DayOfWeek = DayOfWeek.Sunday OrElse
            (value.DayOfWeek = DayOfWeek.Saturday AndAlso Not IncludeSaturdays)
    End Function
End Class
