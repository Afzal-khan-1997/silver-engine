Imports System.Globalization

Public Class LiveProjectCatalogService
    Private ReadOnly _projects As List(Of LiveProjectItem)

    Public Sub New()
        ' SQL connection will replace this seed list later. The form and scheduler already use this service boundary.
        _projects = New List(Of LiveProjectItem) From {
            CreateSmaNewProjectTemplate(),
            New LiveProjectItem With {.ProjectCode = "TPL-BRE-ROL", .ProjectName = "SMA BRE/ROL Update", .ClientName = "Template", .VersionNumber = "1.0", .ProjectSize = "Small", .TemplateName = "BRE/ROL Update template"},
            New LiveProjectItem With {.ProjectCode = "TPL-BRE-WITHIN", .ProjectName = "SMA BRE Within Update", .ClientName = "Template", .VersionNumber = "1.0", .ProjectSize = "Small", .TemplateName = "BRE Within Update"},
            New LiveProjectItem With {.ProjectCode = "TPL-FEEDBACK", .ProjectName = "SMA Feedback update", .ClientName = "Template", .VersionNumber = "1.0", .ProjectSize = "Small", .TemplateName = "Feedback Change"}
        }
    End Sub

    Public Shared Function CreateSmaNewProjectTemplate() As LiveProjectItem
        Return New LiveProjectItem With {.ProjectCode = "TPL-NEW", .ProjectName = "SMA New Project", .ClientName = "Template", .VersionNumber = "1.0", .ProjectSize = "Small", .TemplateName = "SMA New Project"}
    End Function

    Public Function SearchProjects(searchText As String) As List(Of LiveProjectItem)
        Dim query = If(searchText, "").Trim()
        Dim matches = _projects.AsEnumerable()

        If query.Length > 0 Then
            matches = matches.Where(Function(project)
                                        Return project.ProjectCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                            project.ProjectName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                            project.ClientName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                            project.ProjectSize.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                            project.TemplateName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
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
    Public Property TemplateName As String = "New Project"

    Public ReadOnly Property DisplayText As String
        Get
            Return ProjectName
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return DisplayText
    End Function
End Class
