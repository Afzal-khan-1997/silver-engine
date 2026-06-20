Imports System.ComponentModel
Imports System.Globalization

Public Class SMAPlannerForm
    Inherits Form

    Private ReadOnly _projectLibrary As New ProjectLibraryService()
    Private ReadOnly _liveProjectCatalog As New LiveProjectCatalogService()
    Private ReadOnly _projects As New BindingList(Of ProjectLibraryItem)()
    Private ReadOnly _liveProjects As New BindingList(Of LiveProjectItem)()

    Public Sub New()
        InitializeComponent()
        ConfigurePlannerForm()
        LoadLiveProjectList()
        LoadProjectList()
    End Sub

    Private Sub ConfigurePlannerForm()
        _liveProjectSelector.DisplayMember = NameOf(LiveProjectItem.DisplayText)
        _liveProjectSelector.DataSource = _liveProjects

        _grid.DataSource = _projects
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 46, 66)
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 235, 255)
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 31, 42)

        AddHandler btnNewProject.Click, AddressOf OpenNewProject
        AddHandler btnRefreshList.Click, Sub() LoadProjectList()
        AddHandler btnScheduleProject.Click, AddressOf OpenSelectedLiveProjectTemplate
        AddHandler _liveProjectSearchBox.TextChanged, Sub() LoadLiveProjectList()
        AddHandler _liveProjectSelector.SelectedIndexChanged, AddressOf LiveProjectSelectionChanged
        AddHandler _grid.CellDoubleClick, AddressOf OpenSelectedExistingProject
    End Sub

    Private Sub LoadLiveProjectList()
        Dim selectedCode = SelectedLiveProject()?.ProjectCode
        Dim matches = _liveProjectCatalog.SearchProjects(_liveProjectSearchBox.Text)

        _liveProjectSelector.BeginUpdate()
        _liveProjectSelector.DataSource = Nothing
        _liveProjects.Clear()
        For Each project In matches
            _liveProjects.Add(project)
        Next

        _liveProjectSelector.DisplayMember = NameOf(LiveProjectItem.DisplayText)
        _liveProjectSelector.DataSource = _liveProjects

        If _liveProjectSelector.Items.Count > 0 Then
            Dim restoreIndex = -1
            If Not String.IsNullOrWhiteSpace(selectedCode) Then
                For i = 0 To _liveProjects.Count - 1
                    If String.Equals(_liveProjects(i).ProjectCode, selectedCode, StringComparison.OrdinalIgnoreCase) Then
                        restoreIndex = i
                        Exit For
                    End If
                Next
            End If
            _liveProjectSelector.SelectedIndex = If(restoreIndex >= 0, restoreIndex, 0)
        Else
            _liveProjectSelector.SelectedIndex = -1
        End If
        _liveProjectSelector.EndUpdate()
        UpdateLiveProjectSizeLabel()
    End Sub

    Private Sub LoadProjectList()
        _projects.Clear()
        For Each project In _projectLibrary.ListProjects()
            _projects.Add(project)
        Next

        _status.Text = _projects.Count.ToString(CultureInfo.InvariantCulture) & " planned project(s). Double-click a project to update its schedule."
    End Sub

    Private Function SelectedLiveProject() As LiveProjectItem
        Return TryCast(_liveProjectSelector.SelectedItem, LiveProjectItem)
    End Function

    Private Sub LiveProjectSelectionChanged(sender As Object, e As EventArgs)
        UpdateLiveProjectSizeLabel()
    End Sub

    Private Sub UpdateLiveProjectSizeLabel()
        Dim selectedProject = SelectedLiveProject()
        If selectedProject Is Nothing Then
            _liveProjectSizeLabel.Text = "No live project found"
            Return
        End If

        _liveProjectSizeLabel.Text = "Detected size: " & selectedProject.ProjectSize
    End Sub

    Private Sub OpenNewProject(sender As Object, e As EventArgs)
        Using scheduler As New SMASchedulerForm()
            scheduler.StartNewProject()
            FormTransitionService.ShowDialogWithMotion(Me, scheduler)
        End Using
        LoadProjectList()
    End Sub

    Private Sub OpenSelectedLiveProjectTemplate(sender As Object, e As EventArgs)
        Dim selectedProject = SelectedLiveProject()
        If selectedProject Is Nothing Then
            MessageBox.Show(Me, "No live project is selected.", "Live Project", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using scheduler As New SMASchedulerForm()
            scheduler.LoadLiveProjectTemplate(selectedProject)
            FormTransitionService.ShowDialogWithMotion(Me, scheduler)
        End Using
        LoadProjectList()
    End Sub

    Private Sub OpenSelectedExistingProject(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then
            Return
        End If

        Dim item = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, ProjectLibraryItem)
        If item Is Nothing Then
            Return
        End If

        Dim snapshot = _projectLibrary.LoadSnapshot(item.FilePath)
        If snapshot Is Nothing Then
            MessageBox.Show(Me, "This planned project could not be opened.", "Open Project", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            LoadProjectList()
            Return
        End If

        Using scheduler As New SMASchedulerForm()
            scheduler.LoadProjectSnapshot(snapshot)
            FormTransitionService.ShowDialogWithMotion(Me, scheduler)
        End Using
        LoadProjectList()
    End Sub
End Class
