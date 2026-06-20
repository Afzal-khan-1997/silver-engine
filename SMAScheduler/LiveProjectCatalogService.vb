Imports System.Globalization

Public Class LiveProjectCatalogService
    Private ReadOnly _projects As List(Of LiveProjectItem)

    Public Sub New()
        ' SQL connection will replace this seed list later. The form and scheduler already use this service boundary.
        _projects = New List(Of LiveProjectItem) From {
            New LiveProjectItem With {.ProjectCode = "LIVE-1001", .ProjectName = "SMA Villa Extension", .ClientName = "Client A", .VersionNumber = "1.0", .ProjectSize = "Small"},
            New LiveProjectItem With {.ProjectCode = "LIVE-1002", .ProjectName = "SMA Apartment Redevelopment", .ClientName = "Client B", .VersionNumber = "1.0", .ProjectSize = "Medium"},
            New LiveProjectItem With {.ProjectCode = "LIVE-1003", .ProjectName = "SMA Commercial Block", .ClientName = "Client C", .VersionNumber = "1.0", .ProjectSize = "Large"},
            New LiveProjectItem With {.ProjectCode = "LIVE-1004", .ProjectName = "SMA Masterplan Estate", .ClientName = "Client D", .VersionNumber = "1.0", .ProjectSize = "Very Large"}
        }
    End Sub

    Public Function SearchProjects(searchText As String) As List(Of LiveProjectItem)
        Dim query = If(searchText, "").Trim()
        Dim matches = _projects.AsEnumerable()

        If query.Length > 0 Then
            matches = matches.Where(Function(project)
                                        Return project.ProjectCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                            project.ProjectName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                            project.ClientName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                            project.ProjectSize.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                                    End Function)
        End If

        Return matches.OrderBy(Function(project) project.ProjectName).ToList()
    End Function
End Class

Public Class LiveProjectItem
    Public Property ProjectCode As String = ""
    Public Property ProjectName As String = ""
    Public Property ClientName As String = ""
    Public Property VersionNumber As String = "1.0"
    Public Property ProjectSize As String = "Small"

    Public ReadOnly Property DisplayText As String
        Get
            Dim codePart = If(String.IsNullOrWhiteSpace(ProjectCode), "", ProjectCode.Trim() & " - ")
            Return codePart & ProjectName & " (" & ProjectSize & ")"
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return DisplayText
    End Function
End Class
