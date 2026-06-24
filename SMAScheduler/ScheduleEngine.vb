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
            task.DependencyType = NormalizeDependencyType(task.DependencyType)
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
            For Each dependency In ParseDependencies(task.Predecessors, task.DependencyType)
                If dependency.PredecessorId <> task.TaskId AndAlso byId.ContainsKey(dependency.PredecessorId) Then
                    Dim predecessor = byId(dependency.PredecessorId)
                    Dim dependencyStart = RequiredStartForDependency(task, predecessor, dependency.LinkType)
                    If dependencyStart > suggestedStart Then
                        suggestedStart = dependencyStart
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
        Return ParseDependencies(value, "FS").
            Select(Function(dependency) dependency.PredecessorId).
            Distinct().
            ToList()
    End Function

    Public Function ParseDependencies(value As String, defaultDependencyType As String) As List(Of ScheduleDependency)
        Dim result As New List(Of ScheduleDependency)
        If String.IsNullOrWhiteSpace(value) Then
            Return result
        End If

        For Each part In value.Split({","c, ";"c, " "c}, StringSplitOptions.RemoveEmptyEntries)
            Dim dependency = ParseDependencyToken(part.Trim(), defaultDependencyType)
            If dependency IsNot Nothing AndAlso Not result.Any(Function(item) item.PredecessorId = dependency.PredecessorId) Then
                result.Add(dependency)
            End If
        Next

        Return result
    End Function

    Private Function ParseDependencyToken(value As String, defaultDependencyType As String) As ScheduleDependency
        If String.IsNullOrWhiteSpace(value) Then
            Return Nothing
        End If

        Dim index = 0
        While index < value.Length AndAlso Char.IsDigit(value(index))
            index += 1
        End While

        If index = 0 Then
            Return Nothing
        End If

        Dim predecessorId As Integer
        If Not Integer.TryParse(value.Substring(0, index), predecessorId) OrElse predecessorId <= 0 Then
            Return Nothing
        End If

        Dim linkType = NormalizeDependencyType(defaultDependencyType)
        Dim suffix = value.Substring(index).Trim().TrimStart("-"c, ":"c).ToUpperInvariant()
        If suffix.Length >= 2 Then
            linkType = NormalizeDependencyType(suffix.Substring(0, 2))
        End If

        Return New ScheduleDependency With {.PredecessorId = predecessorId, .LinkType = linkType}
    End Function

    Private Function RequiredStartForDependency(task As ScheduleTask, predecessor As ScheduleTask, linkType As String) As Date
        Select Case NormalizeDependencyType(linkType)
            Case "SS"
                Return NextWorkingDate(predecessor.StartDate)
            Case "FF"
                Return StartForWorkingFinish(predecessor.FinishDate, task.DurationDays)
            Case "SF"
                Return StartForWorkingFinish(predecessor.StartDate, task.DurationDays)
            Case Else
                Return NextWorkingDate(predecessor.FinishDate)
        End Select
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

    Private Function StartForWorkingFinish(finishDate As Date, durationDays As Decimal) As Date
        Dim currentDate = PreviousOrSameWorkingDate(finishDate)
        Dim remainingDays = Math.Max(1, CInt(Math.Ceiling(durationDays)))

        While remainingDays > 1
            currentDate = currentDate.AddDays(-1)
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

    Private Function PreviousOrSameWorkingDate(value As Date) As Date
        Dim currentDate = value.Date
        While IsBlockedDate(currentDate)
            currentDate = currentDate.AddDays(-1)
        End While
        Return currentDate
    End Function

    Private Shared Function NormalizeDependencyType(value As String) As String
        Dim safeValue = If(value, "").Trim().ToUpperInvariant()
        Select Case safeValue
            Case "SS", "FF", "SF"
                Return safeValue
            Case Else
                Return "FS"
        End Select
    End Function

    Private Function IsBlockedDate(value As Date) As Boolean
        Return Not IncludeSaturdays AndAlso
            (value.DayOfWeek = DayOfWeek.Saturday OrElse value.DayOfWeek = DayOfWeek.Sunday)
    End Function
End Class

Public Class ScheduleDependency
    Public Property PredecessorId As Integer
    Public Property LinkType As String = "FS"
End Class
