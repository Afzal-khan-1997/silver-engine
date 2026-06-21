Imports System.ComponentModel

Public Class ScheduleTask
    Implements INotifyPropertyChanged

    Private _taskId As Integer
    Private _databaseTaskId As Integer
    Private _taskName As String = ""
    Private _startDate As Date = Date.Today
    Private _durationDays As Decimal = 1D
    Private _finishDate As Date = Date.Today
    Private _percentComplete As Integer
    Private _predecessors As String = ""
    Private _dependencyType As String = "FS"
    Private _assignedTo As String = ""
    Private _assignmentDate As Date = Date.Today
    Private _resourceNames As String = ""
    Private _resourceAllocations As String = ""
    Private _dailyResourceAllocations As String = ""
    Private _resourceHours As Decimal
    Private _moduleId As Integer
    Private _plannerTaskId As String = ""

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Property TaskId As Integer
        Get
            Return _taskId
        End Get
        Set(value As Integer)
            If _taskId <> value Then
                _taskId = value
                Notify(NameOf(TaskId))
            End If
        End Set
    End Property

    Public Property DatabaseTaskId As Integer
        Get
            Return _databaseTaskId
        End Get
        Set(value As Integer)
            If _databaseTaskId <> value Then
                _databaseTaskId = value
                Notify(NameOf(DatabaseTaskId))
            End If
        End Set
    End Property

    Public Property TaskName As String
        Get
            Return _taskName
        End Get
        Set(value As String)
            If _taskName <> value Then
                _taskName = value
                Notify(NameOf(TaskName))
            End If
        End Set
    End Property

    Public Property StartDate As Date
        Get
            Return _startDate
        End Get
        Set(value As Date)
            Dim normalized = value.Date
            If _startDate <> normalized Then
                _startDate = normalized
                Notify(NameOf(StartDate))
            End If
        End Set
    End Property

    Public Property DurationDays As Decimal
        Get
            Return _durationDays
        End Get
        Set(value As Decimal)
            Dim safeValue = Math.Max(0D, value)
            If _durationDays <> safeValue Then
                _durationDays = safeValue
                Notify(NameOf(DurationDays))
            End If
        End Set
    End Property

    Public Property FinishDate As Date
        Get
            Return _finishDate
        End Get
        Set(value As Date)
            Dim normalized = value.Date
            If _finishDate <> normalized Then
                _finishDate = normalized
                Notify(NameOf(FinishDate))
            End If
        End Set
    End Property

    Public Property PercentComplete As Integer
        Get
            Return _percentComplete
        End Get
        Set(value As Integer)
            Dim safeValue = Math.Min(100, Math.Max(0, value))
            If _percentComplete <> safeValue Then
                _percentComplete = safeValue
                Notify(NameOf(PercentComplete))
            End If
        End Set
    End Property

    Public Property Predecessors As String
        Get
            Return _predecessors
        End Get
        Set(value As String)
            Dim safeValue = If(value, "").Trim()
            If _predecessors <> safeValue Then
                _predecessors = safeValue
                Notify(NameOf(Predecessors))
                Notify(NameOf(PredecessorLink))
            End If
        End Set
    End Property

    Public Property DependencyType As String
        Get
            Return _dependencyType
        End Get
        Set(value As String)
            Dim safeValue = NormalizeDependencyType(value)
            If _dependencyType <> safeValue Then
                _dependencyType = safeValue
                Notify(NameOf(DependencyType))
                Notify(NameOf(PredecessorLink))
            End If
        End Set
    End Property

    Public Property PredecessorLink As String
        Get
            If String.IsNullOrWhiteSpace(_predecessors) Then
                Return ""
            End If
            Return _dependencyType
        End Get
        Set(value As String)
            DependencyType = value
        End Set
    End Property

    Public Property AssignedTo As String
        Get
            Return _assignedTo
        End Get
        Set(value As String)
            Dim safeValue = If(value, "").Trim()
            If _assignedTo <> safeValue Then
                _assignedTo = safeValue
                Notify(NameOf(AssignedTo))
            End If
        End Set
    End Property

    Public Property AssignmentDate As Date
        Get
            Return _assignmentDate
        End Get
        Set(value As Date)
            Dim normalized = value.Date
            If _assignmentDate <> normalized Then
                _assignmentDate = normalized
                Notify(NameOf(AssignmentDate))
            End If
        End Set
    End Property

    Public Property ResourceNames As String
        Get
            Return _resourceNames
        End Get
        Set(value As String)
            Dim safeValue = If(value, "").Trim()
            If _resourceNames <> safeValue Then
                _resourceNames = safeValue
                Notify(NameOf(ResourceNames))
            End If
        End Set
    End Property

    Public Property ResourceAllocations As String
        Get
            Return _resourceAllocations
        End Get
        Set(value As String)
            Dim safeValue = If(value, "").Trim()
            If _resourceAllocations <> safeValue Then
                _resourceAllocations = safeValue
                Notify(NameOf(ResourceAllocations))
            End If
        End Set
    End Property

    Public Property DailyResourceAllocations As String
        Get
            Return _dailyResourceAllocations
        End Get
        Set(value As String)
            Dim safeValue = If(value, "").Trim()
            If _dailyResourceAllocations <> safeValue Then
                _dailyResourceAllocations = safeValue
                Notify(NameOf(DailyResourceAllocations))
            End If
        End Set
    End Property

    Public Property ResourceHours As Decimal
        Get
            Return _resourceHours
        End Get
        Set(value As Decimal)
            Dim safeValue = Math.Max(0D, value)
            If _resourceHours <> safeValue Then
                _resourceHours = safeValue
                Notify(NameOf(ResourceHours))
            End If
        End Set
    End Property

    Public Property ModuleId As Integer
        Get
            Return _moduleId
        End Get
        Set(value As Integer)
            If _moduleId <> value Then
                _moduleId = value
                Notify(NameOf(ModuleId))
            End If
        End Set
    End Property

    Public Property PlannerTaskId As String
        Get
            Return _plannerTaskId
        End Get
        Set(value As String)
            Dim safeValue = If(value, "").Trim()
            If _plannerTaskId <> safeValue Then
                _plannerTaskId = safeValue
                Notify(NameOf(PlannerTaskId))
            End If
        End Set
    End Property

    Private Sub Notify(propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private Shared Function NormalizeDependencyType(value As String) As String
        Dim safeValue = If(value, "").Trim().ToUpperInvariant()
        Select Case safeValue
            Case "SS", "FF", "SF"
                Return safeValue
            Case Else
                Return "FS"
        End Select
    End Function
End Class
