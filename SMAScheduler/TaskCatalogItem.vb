Public Class TaskCatalogItem
    Public Property DatabaseTaskId As Integer
    Public Property Title As String = ""
    Public Property Predecessor As String = ""
    Public Property Summary As String = ""
    Public Property SmallHours As Decimal
    Public Property MediumHours As Decimal
    Public Property LargeHours As Decimal
    Public Property VeryLargeHours As Decimal
    Public Property Assignee As String = ""
    Public Property ModuleId As Integer

    Public Overrides Function ToString() As String
        Return DatabaseTaskId & " - " & Title
    End Function

    Public Function HoursForSize(sizeName As String) As Decimal
        Select Case If(sizeName, "").Trim().ToLowerInvariant()
            Case "medium"
                Return MediumHours
            Case "large"
                Return LargeHours
            Case "very large"
                Return VeryLargeHours
            Case Else
                Return SmallHours
        End Select
    End Function
End Class
