Imports System.ComponentModel
Imports System.Globalization

Public Class SMAPlannerForm
    Inherits Form

    Private ReadOnly _projectLibrary As New ProjectLibraryService()
    Private ReadOnly _liveProjectCatalog As New LiveProjectCatalogService()
    Private ReadOnly _projects As New BindingList(Of ProjectLibraryItem)()
    Private ReadOnly _liveProjects As New BindingList(Of LiveProjectItem)()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _liveProjectSearchBox As New TextBox()
    Private ReadOnly _liveProjectSelector As New ComboBox()
    Private ReadOnly _liveProjectSizeLabel As New Label()
    Private ReadOnly _status As New Label()

    Public Sub New()
        Text = "SMA Planner"
        StartPosition = FormStartPosition.CenterScreen
        MinimumSize = New Size(980, 640)
        Size = New Size(1180, 760)
        Font = New Font("Segoe UI", 9.0F)
        BackColor = Color.FromArgb(244, 246, 249)

        BuildLayout()
        LoadLiveProjectList()
        LoadProjectList()
    End Sub

    Private Sub BuildLayout()
        Dim header As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 210,
            .BackColor = Color.FromArgb(229, 241, 255),
            .Padding = New Padding(24, 18, 24, 18)
        }

        Dim title As New Label With {
            .Text = "SMA Planner",
            .AutoSize = True,
            .Font = New Font("Segoe UI Semibold", 20.0F),
            .ForeColor = Color.FromArgb(24, 31, 42),
            .Location = New Point(24, 18)
        }

        Dim prompt As New Label With {
            .Text = "Do you want to plan for a new project?",
            .AutoSize = True,
            .Font = New Font("Segoe UI Semibold", 11.0F),
            .ForeColor = Color.FromArgb(37, 47, 63),
            .Location = New Point(26, 76)
        }

        Dim newProjectButton As New Button With {
            .Text = "New Project",
            .Width = 140,
            .Height = 34,
            .Location = New Point(390, 70),
            .BackColor = Color.FromArgb(45, 125, 221),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        newProjectButton.FlatAppearance.BorderSize = 0
        AddHandler newProjectButton.Click, AddressOf OpenNewProject

        Dim refreshButton As New Button With {
            .Text = "Refresh List",
            .Width = 120,
            .Height = 34,
            .Location = New Point(546, 70),
            .BackColor = Color.FromArgb(35, 46, 66),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        refreshButton.FlatAppearance.BorderSize = 0
        AddHandler refreshButton.Click, Sub() LoadProjectList()

        Dim searchLabel As New Label With {
            .Text = "Find live project",
            .AutoSize = True,
            .ForeColor = Color.FromArgb(75, 85, 99),
            .Location = New Point(26, 120)
        }

        _liveProjectSearchBox.Width = 290
        _liveProjectSearchBox.Height = 28
        _liveProjectSearchBox.Location = New Point(26, 144)
        _liveProjectSearchBox.PlaceholderText = "Search by project, code, client or size"
        AddHandler _liveProjectSearchBox.TextChanged, Sub() LoadLiveProjectList()

        Dim selectorLabel As New Label With {
            .Text = "Live project",
            .AutoSize = True,
            .ForeColor = Color.FromArgb(75, 85, 99),
            .Location = New Point(336, 120)
        }

        _liveProjectSelector.DropDownStyle = ComboBoxStyle.DropDownList
        _liveProjectSelector.Width = 360
        _liveProjectSelector.Height = 28
        _liveProjectSelector.Location = New Point(336, 144)
        _liveProjectSelector.DisplayMember = NameOf(LiveProjectItem.DisplayText)
        _liveProjectSelector.DataSource = _liveProjects
        AddHandler _liveProjectSelector.SelectedIndexChanged, AddressOf LiveProjectSelectionChanged

        Dim selectLiveProjectButton As New Button With {
            .Text = "Schedule Project",
            .Width = 150,
            .Height = 34,
            .Location = New Point(716, 140),
            .BackColor = Color.FromArgb(32, 164, 112),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        selectLiveProjectButton.FlatAppearance.BorderSize = 0
        AddHandler selectLiveProjectButton.Click, AddressOf OpenSelectedLiveProjectTemplate

        _liveProjectSizeLabel.AutoSize = False
        _liveProjectSizeLabel.Width = 260
        _liveProjectSizeLabel.Height = 34
        _liveProjectSizeLabel.Location = New Point(868, 140)
        _liveProjectSizeLabel.TextAlign = ContentAlignment.MiddleLeft
        _liveProjectSizeLabel.ForeColor = Color.FromArgb(24, 31, 42)
        _liveProjectSizeLabel.Font = New Font("Segoe UI Semibold", 9.0F)

        header.Controls.Add(title)
        header.Controls.Add(prompt)
        header.Controls.Add(newProjectButton)
        header.Controls.Add(refreshButton)
        header.Controls.Add(searchLabel)
        header.Controls.Add(_liveProjectSearchBox)
        header.Controls.Add(selectorLabel)
        header.Controls.Add(_liveProjectSelector)
        header.Controls.Add(selectLiveProjectButton)
        header.Controls.Add(_liveProjectSizeLabel)

        Dim gridPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(24, 22, 24, 16),
            .BackColor = Color.White
        }

        Dim listTitle As New Label With {
            .Text = "Already Planned Projects",
            .Dock = DockStyle.Top,
            .Height = 34,
            .Font = New Font("Segoe UI Semibold", 12.0F),
            .ForeColor = Color.FromArgb(24, 31, 42)
        }

        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.AutoGenerateColumns = False
        _grid.BackgroundColor = Color.White
        _grid.BorderStyle = BorderStyle.None
        _grid.ColumnHeadersHeight = 34
        _grid.Dock = DockStyle.Fill
        _grid.EnableHeadersVisualStyles = False
        _grid.GridColor = Color.FromArgb(232, 236, 242)
        _grid.MultiSelect = False
        _grid.ReadOnly = True
        _grid.RowHeadersVisible = False
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 46, 66)
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 235, 255)
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 31, 42)
        AddHandler _grid.CellDoubleClick, AddressOf OpenSelectedExistingProject

        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.ProjectName), .HeaderText = "Project", .Width = 260})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.VersionNumber), .HeaderText = "Version", .Width = 90})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.ProjectSize), .HeaderText = "Size", .Width = 110})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.TaskCount), .HeaderText = "Tasks", .Width = 80})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.ResourceHours), .HeaderText = "Hours", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "0.##"}})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.StartDateText), .HeaderText = "Start", .Width = 120})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.FinishDateText), .HeaderText = "Finish", .Width = 120})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ProjectLibraryItem.UpdatedOn), .HeaderText = "Updated", .Width = 160, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "dd-MMM-yyyy HH:mm"}})
        _grid.DataSource = _projects

        _status.Dock = DockStyle.Bottom
        _status.Height = 28
        _status.ForeColor = Color.DimGray

        gridPanel.Controls.Add(_grid)
        gridPanel.Controls.Add(listTitle)
        gridPanel.Controls.Add(_status)

        Controls.Add(gridPanel)
        Controls.Add(header)
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
        Using scheduler As New Form1()
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

        Using scheduler As New Form1()
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

        Using scheduler As New Form1()
            scheduler.LoadProjectSnapshot(snapshot)
            FormTransitionService.ShowDialogWithMotion(Me, scheduler)
        End Using
        LoadProjectList()
    End Sub
End Class
