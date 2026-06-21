Imports System.ComponentModel
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.IO
Imports System.Text.Json

Public Class SMASchedulerForm
    Private ReadOnly _tasks As New BindingList(Of ScheduleTask)
    Private ReadOnly _engine As New ScheduleEngine()
    Private ReadOnly _taskCatalogService As New TaskCatalogService()
    Private ReadOnly _employeeCatalogService As New EmployeeCatalogService()
    Private ReadOnly _xlsxExportService As New XlsxExportService()
    Private ReadOnly _projectLibrary As New ProjectLibraryService()
    Private ReadOnly _taskCatalog As New BindingList(Of TaskCatalogItem)
    Private ReadOnly _employees As New BindingList(Of String)

    Private WithEvents _grid As DataGridView
    Private _gantt As GanttPanel
    Private _status As ToolStripStatusLabel
    Private _projectName As TextBox
    Private _versionNumber As TextBox
    Private _summaryTitle As Label
    Private _summaryDates As Label
    Private _summaryProgress As Label
    Private _summaryResources As Label
    Private _detailsPanel As Panel
    Private _taskCatalogSelector As ComboBox
    Private _projectSizeSelector As ComboBox
    Private _includeSaturdays As CheckBox
    Private _remainingHoursLabel As Label
    Private _capacityGrid As DataGridView
    Private _plannerPieChart As PlannerPieChartPanel
    Private _plannerLegendGrid As DataGridView
    Private _plannerTaskCountLabel As Label
    Private _plannerDurationLabel As Label
    Private _themeSelector As ComboBox
    Private _themeLabel As Label
    Private _currentTheme As SchedulerThemePalette = SchedulerThemePalette.ThemeByName("Fresh")
    Private ReadOnly _capacityDateColumns As New Dictionary(Of Integer, Date)
    Private _isRecalculating As Boolean
    Private _isLoadingCatalogControls As Boolean
    Private _suspendTaskEvents As Boolean
    Private _isLoadingCapacityGrid As Boolean

    Public Sub New()
        InitializeComponent()
        ConfigureDesignerUi()
        AddHandler _tasks.ListChanged, AddressOf TasksChanged
        RecalculateAndRefresh("Ready")
        ClearPlanningInputDisplays()
    End Sub

    Public Sub StartNewProject()
        ClearProjectForNewSchedule()
        SetStatus("New project ready")
    End Sub

    Public Sub LoadLiveProjectTemplate(liveProject As LiveProjectItem)
        If liveProject Is Nothing Then
            Return
        End If

        ClearProjectForNewSchedule()
        _projectName.Text = If(String.IsNullOrWhiteSpace(liveProject.ProjectName), "SMA Scheduler", liveProject.ProjectName.Trim())
        _versionNumber.Text = If(String.IsNullOrWhiteSpace(liveProject.VersionNumber), "1.0", liveProject.VersionNumber.Trim())
        SelectProjectSize(liveProject.ProjectSize)

        Dim projectSize = CStr(_projectSizeSelector.SelectedItem)
        ClearPlanningInputDisplays()
        RecalculateAndRefresh("Project shell loaded for " & projectSize & " project")
    End Sub

    Public Sub LoadProjectSnapshot(snapshot As ProjectSnapshot)
        If snapshot Is Nothing Then
            Return
        End If

        _tasks.Clear()
        _projectName.Text = snapshot.ProjectName
        _versionNumber.Text = If(String.IsNullOrWhiteSpace(snapshot.VersionNumber), "1.0", snapshot.VersionNumber)
        SelectProjectSize(snapshot.ProjectSize)
        _totalProjectHours.Value = ClampDecimal(snapshot.TotalProjectHours, _totalProjectHours.Minimum, _totalProjectHours.Maximum)
        _resourcesNeeded.Value = ClampDecimal(snapshot.ResourcesNeeded, _resourcesNeeded.Minimum, _resourcesNeeded.Maximum)
        If snapshot.Tasks IsNot Nothing Then
            For Each task In snapshot.Tasks
                _tasks.Add(task)
            Next
        End If
        RenumberTasks()
        RecalculateAndRefresh("Project opened")
    End Sub

    Private Sub SelectProjectSize(sizeName As String)
        If _projectSizeSelector Is Nothing OrElse _projectSizeSelector.Items.Count = 0 Then
            Return
        End If

        Dim normalizedSize = If(String.IsNullOrWhiteSpace(sizeName), "Small", sizeName.Trim())
        For i = 0 To _projectSizeSelector.Items.Count - 1
            If String.Equals(Convert.ToString(_projectSizeSelector.Items(i), CultureInfo.InvariantCulture), normalizedSize, StringComparison.OrdinalIgnoreCase) Then
                _projectSizeSelector.SelectedIndex = i
                Return
            End If
        Next

        _projectSizeSelector.SelectedIndex = 0
    End Sub

    Private Sub ConfigureDesignerUi()
        _grid.AutoGenerateColumns = False
        _grid.Columns.Clear()
        _gantt.Tasks = _tasks
        StyleGrid()
        ConfigureCapacityPlanningGrid()
        LoadCatalogControls()
        AddGridColumns()
        _grid.DataSource = _tasks
        ApplySchedulerHeaderLayout()
        ConfigureThemeSelector()
        ApplyTheme()
        AddHandler _grid.SelectionChanged, AddressOf ScheduleSelectionChanged
        AddHandler btnNew.Click, AddressOf NewProject
        AddHandler btnOpen.Click, AddressOf OpenProjectFile
        AddHandler btnSave.Click, AddressOf SaveProjectFile
        AddHandler btnRefreshCapacity.Click, AddressOf RefreshCapacityPlanning
        AddHandler btnAddTask.Click, AddressOf AddTask
        AddHandler btnDelete.Click, AddressOf DeleteTask
        AddHandler btnMoveUp.Click, AddressOf MoveTaskUp
        AddHandler btnMoveDown.Click, AddressOf MoveTaskDown
        AddHandler btnLink.Click, AddressOf LinkSelectedTask
        AddHandler btnUnlink.Click, AddressOf UnlinkSelectedTask
        AddHandler btnMilestone.Click, AddressOf ToggleMilestone
        AddHandler btnSchedulePlanner.Click, AddressOf ScheduleTasksThroughMsPlanner
        AddHandler _totalProjectHours.ValueChanged, AddressOf ProjectInputsChanged
        AddHandler _resourcesNeeded.ValueChanged, AddressOf ProjectInputsChanged
        AddHandler _includeSaturdays.CheckedChanged, AddressOf AllocationInputsChanged
        AddHandler _taskCatalogSelector.SelectedIndexChanged, AddressOf CatalogSelectionChanged
        AddHandler _projectSizeSelector.SelectedIndexChanged, AddressOf CatalogSelectionChanged
    End Sub

    Private Sub ConfigureThemeSelector()
        If _themeSelector Is Nothing Then
            _themeLabel = New Label With {
                .AutoSize = True,
                .Text = "Theme",
                .Location = New Point(865, 42),
                .Name = "_themeLabel"
            }
            _themeSelector = New ComboBox With {
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Location = New Point(865, 66),
                .Name = "_themeSelector",
                .Size = New Size(164, 28)
            }
            _themeSelector.Items.AddRange(SchedulerThemePalette.AllNames())
            _themeSelector.SelectedItem = _currentTheme.Name
            headerPanel.Controls.Add(_themeLabel)
            headerPanel.Controls.Add(_themeSelector)
            AddHandler _themeSelector.SelectedIndexChanged, AddressOf ThemeSelectionChanged
        End If
    End Sub

    Private Sub ThemeSelectionChanged(sender As Object, e As EventArgs)
        Dim selectedName = Convert.ToString(_themeSelector.SelectedItem, CultureInfo.InvariantCulture)
        _currentTheme = SchedulerThemePalette.ThemeByName(selectedName)
        ApplyTheme()
        _gantt.Invalidate()
        If _plannerPieChart IsNot Nothing Then
            _plannerPieChart.Invalidate()
        End If
    End Sub

    Private Sub ApplySchedulerHeaderLayout()
        If resourcesNeededLabel.Parent IsNot Nothing Then
            resourcesNeededLabel.Parent.Controls.Remove(resourcesNeededLabel)
        End If
        If _resourcesNeeded.Parent IsNot Nothing Then
            _resourcesNeeded.Parent.Controls.Remove(_resourcesNeeded)
        End If
        If _remainingHoursLabel.Parent IsNot Nothing Then
            _remainingHoursLabel.Parent.Controls.Remove(_remainingHoursLabel)
        End If
        btnSchedulePlanner.Location = New Point(650, 58)
        btnSchedulePlanner.Size = New Size(190, 38)
        btnSchedulePlanner.Font = New Font("Segoe UI Semibold", 9.0F)
    End Sub

    Private Sub ApplyTheme()
        Dim theme = _currentTheme
        BackColor = theme.WindowBack
        commandBar.BackColor = theme.CommandBack
        For Each item As ToolStripItem In commandBar.Items
            item.ForeColor = theme.CommandText
        Next

        headerPanel.BackColor = theme.HeaderBack
        appTitle.ForeColor = theme.Text
        btnSchedulePlanner.BackColor = theme.Action
        btnSchedulePlanner.ForeColor = Color.White
        If _themeLabel IsNot Nothing Then
            _themeLabel.ForeColor = theme.MutedText
        End If

        For Each label In {projectLabel, versionLabel, totalHoursLabel, taskCatalogLabel, projectSizeLabel, resourcesNeededLabel}
            If label IsNot Nothing Then
                label.ForeColor = theme.MutedText
            End If
        Next

        _summaryTitle.BackColor = theme.TileOne
        _summaryDates.BackColor = theme.TileTwo
        _summaryProgress.BackColor = theme.TileThree
        _summaryResources.BackColor = theme.TileFour
        For Each label In {_summaryTitle, _summaryDates, _summaryProgress, _summaryResources}
            label.ForeColor = theme.Text
        Next

        contentSplit.BackColor = theme.Divider
        mainSplit.BackColor = theme.Divider
        _detailsPanel.BackColor = theme.PanelBack
        taskWorkspaceTitle.ForeColor = theme.Text
        statusBar.BackColor = theme.PanelBack
        _status.ForeColor = theme.MutedText
        _gantt.BackColor = theme.PanelBack

        ApplyGridTheme(_grid, theme)
        If _capacityGrid IsNot Nothing Then
            ApplyGridTheme(_capacityGrid, theme)
        End If
        If _plannerLegendGrid IsNot Nothing Then
            ApplyGridTheme(_plannerLegendGrid, theme)
        End If
        For Each label In {_plannerTaskCountLabel, _plannerDurationLabel}
            If label IsNot Nothing Then
                label.BackColor = theme.TileThree
                label.ForeColor = theme.Text
            End If
        Next
    End Sub

    Private Sub ApplyGridTheme(grid As DataGridView, theme As SchedulerThemePalette)
        If grid Is Nothing Then
            Return
        End If

        grid.BackgroundColor = theme.PanelBack
        grid.GridColor = theme.GridLine
        grid.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeader
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        grid.DefaultCellStyle.BackColor = theme.PanelBack
        grid.DefaultCellStyle.ForeColor = theme.Text
        grid.DefaultCellStyle.SelectionBackColor = theme.Selection
        grid.DefaultCellStyle.SelectionForeColor = theme.Text
        grid.AlternatingRowsDefaultCellStyle.BackColor = theme.AlternatingRow
        grid.EnableHeadersVisualStyles = False
    End Sub

    Private Sub LoadCatalogControls()
        _isLoadingCatalogControls = True
        _taskCatalog.Clear()
        For Each item In _taskCatalogService.LoadAvailableTasks()
            _taskCatalog.Add(item)
        Next

        _employees.Clear()
        For Each employeeName In _employeeCatalogService.LoadEmployees()
            If Not _employees.Contains(employeeName) Then
                _employees.Add(employeeName)
            End If
        Next

        _taskCatalogSelector.DataSource = _taskCatalog
        _taskCatalogSelector.DisplayMember = NameOf(TaskCatalogItem.Title)
        _projectSizeSelector.Items.AddRange({"Small", "Medium", "Large", "Very Large"})
        _projectSizeSelector.SelectedIndex = 0

        _isLoadingCatalogControls = False
        UpdateAllocationHoursFromSelectedCatalog()
        RecalculateAndRefresh("Working days updated")
    End Sub

    Private Sub ProjectInputsChanged(sender As Object, e As EventArgs)
        UpdateSummary()
        SetStatus("Project planning values updated")
    End Sub

    Private Sub ClearPlanningInputDisplays()
        Dim totalHoursInput = TryCast(_totalProjectHours, BlankNumericUpDown)
        If totalHoursInput IsNot Nothing Then
            totalHoursInput.ClearDisplayedValue()
        ElseIf _totalProjectHours IsNot Nothing Then
            _totalProjectHours.Text = ""
        End If

        Dim resourcesInput = TryCast(_resourcesNeeded, BlankNumericUpDown)
        If resourcesInput IsNot Nothing Then
            resourcesInput.ClearDisplayedValue()
        ElseIf _resourcesNeeded IsNot Nothing Then
            _resourcesNeeded.Text = ""
        End If

        UpdateRemainingHoursDisplay()
    End Sub

    Private Sub CatalogSelectionChanged(sender As Object, e As EventArgs)
        If _isLoadingCatalogControls Then
            Return
        End If
        UpdateAllocationHoursFromSelectedCatalog()
    End Sub

    Private Sub AllocationInputsChanged(sender As Object, e As EventArgs)
        If _isLoadingCatalogControls OrElse _isRecalculating Then
            Return
        End If
        RecalculateAndRefresh("Working days updated")
    End Sub

    Private Sub UpdateAllocationHoursFromSelectedCatalog()
        Dim selectedCatalogTask = TryCast(_taskCatalogSelector.SelectedItem, TaskCatalogItem)
        If selectedCatalogTask Is Nothing OrElse _projectSizeSelector.SelectedItem Is Nothing Then
            Return
        End If

        UpdateRemainingHoursDisplay()
    End Sub

    Private Sub StyleGrid()
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 46, 66)
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 235, 255)
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 31, 42)
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255)
        _grid.DefaultCellStyle.BackColor = Color.White
        _grid.DefaultCellStyle.ForeColor = Color.FromArgb(37, 47, 63)
    End Sub

    Private Sub ConfigureCapacityPlanningGrid()
        If _detailsPanel Is Nothing OrElse contentSplit Is Nothing Then
            Return
        End If

        taskWorkspaceTitle.Text = "Capacity Planning"
        contentSplit.Panel2Collapsed = False
        contentSplit.Panel2MinSize = 260
        AddHandler contentSplit.SizeChanged, Sub()
                                                 ApplyResponsiveSplitter(contentSplit, 420, 260, 0.64R)
                                             End Sub

        _capacityGrid = New DataGridView With {
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ColumnHeadersHeight = 32,
            .Dock = DockStyle.Fill,
            .EnableHeadersVisualStyles = False,
            .GridColor = Color.FromArgb(232, 236, 242),
            .MultiSelect = False,
            .Name = "_capacityGrid",
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.CellSelect
        }
        _capacityGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 46, 66)
        _capacityGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _capacityGrid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        _capacityGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 235, 255)
        _capacityGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 31, 42)

        Dim lowerSplit As New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterWidth = 5,
            .BackColor = Color.FromArgb(224, 229, 236),
            .Panel1MinSize = 1,
            .Panel2MinSize = 1
        }
        AddHandler lowerSplit.SizeChanged, Sub()
                                               ApplyResponsiveSplitter(lowerSplit, 480, 430, 0.55R)
                                           End Sub

        Dim capacityPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(18, 14, 18, 16)
        }
        taskWorkspaceTitle.Text = "Capacity Planning"
        taskWorkspaceTitle.Dock = DockStyle.Top
        taskWorkspaceTitle.Height = 38
        capacityPanel.Controls.Add(_capacityGrid)
        capacityPanel.Controls.Add(taskWorkspaceTitle)
        taskWorkspaceTitle.BringToFront()

        Dim plannerPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(14, 10, 14, 12)
        }
        Dim plannerTitle As New Label With {
            .Text = "Planner Preview",
            .Dock = DockStyle.Top,
            .Height = 32,
            .Font = New Font("Segoe UI Semibold", 12.0F),
            .ForeColor = Color.FromArgb(24, 31, 42)
        }
        Dim plannerSummary As New FlowLayoutPanel With {
            .Dock = DockStyle.Left,
            .Width = 210,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .Padding = New Padding(0, 0, 10, 6)
        }
        _plannerTaskCountLabel = PlannerPreviewBadge("Scheduled Tasks: 0")
        _plannerDurationLabel = PlannerPreviewBadge("Duration: 0 days")
        plannerSummary.Controls.Add(plannerTitle)
        plannerSummary.Controls.Add(_plannerTaskCountLabel)
        plannerSummary.Controls.Add(_plannerDurationLabel)

        Dim plannerContent As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(2, 0, 2, 0)
        }
        _plannerPieChart = New PlannerPieChartPanel(_tasks) With {.Dock = DockStyle.Fill, .BackColor = Color.White}
        _plannerLegendGrid = Nothing
        plannerContent.Controls.Add(_plannerPieChart)

        Dim plannerBody As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White
        }
        plannerBody.Controls.Add(plannerContent)
        plannerBody.Controls.Add(plannerSummary)

        plannerPanel.Controls.Add(plannerBody)

        lowerSplit.Panel1.Controls.Add(capacityPanel)
        lowerSplit.Panel2.Controls.Add(plannerPanel)

        _detailsPanel.Controls.Clear()
        _detailsPanel.Controls.Add(lowerSplit)
        ApplyResponsiveSplitter(contentSplit, 420, 260, 0.64R)
        ApplyResponsiveSplitter(lowerSplit, 480, 430, 0.55R)

        AddHandler _capacityGrid.CellParsing, AddressOf CapacityGridCellParsing
        AddHandler _capacityGrid.CellEndEdit, AddressOf CapacityGridCellEndEdit
        AddHandler _capacityGrid.DataError, AddressOf CapacityGridDataError
    End Sub

    Private Sub ApplyResponsiveSplitter(split As SplitContainer, preferredPanel1Min As Integer, preferredPanel2Min As Integer, panel1Ratio As Double)
        If split Is Nothing OrElse split.IsDisposed Then
            Return
        End If

        Dim available = If(split.Orientation = Orientation.Vertical, split.Width, split.Height) - split.SplitterWidth
        If available < 20 Then
            Return
        End If

        Dim panel1Min = preferredPanel1Min
        Dim panel2Min = preferredPanel2Min
        Dim preferredTotal = Math.Max(1, panel1Min + panel2Min)
        If preferredTotal > available Then
            Dim scale = available / CDbl(preferredTotal)
            panel1Min = Math.Max(1, CInt(Math.Floor(panel1Min * scale)))
            panel2Min = Math.Max(1, available - panel1Min)
        End If

        If panel1Min + panel2Min > available Then
            panel2Min = Math.Max(1, available - panel1Min)
        End If

        Dim minDistance = Math.Max(1, panel1Min)
        Dim maxDistance = Math.Max(minDistance, available - Math.Max(1, panel2Min))
        Dim desired = CInt(Math.Round(available * Math.Max(0.1R, Math.Min(0.9R, panel1Ratio))))
        desired = Math.Max(minDistance, Math.Min(maxDistance, desired))

        Try
            split.Panel1MinSize = 1
            split.Panel2MinSize = 1
            split.SplitterDistance = desired
            split.Panel1MinSize = panel1Min
            split.Panel2MinSize = panel2Min
        Catch ex As InvalidOperationException
            ' WinForms can report temporary invalid sizes while the designer/runtime is still laying out.
        End Try
    End Sub

    Private Function PlannerPreviewBadge(text As String) As Label
        Return New Label With {
            .AutoSize = False,
            .Text = text,
            .Width = 210,
            .Height = 28,
            .Margin = New Padding(0, 2, 0, 4),
            .BackColor = Color.FromArgb(225, 239, 255),
            .ForeColor = Color.FromArgb(24, 31, 42),
            .Font = New Font("Segoe UI Semibold", 9.0F),
            .TextAlign = ContentAlignment.MiddleCenter
        }
    End Function

    Private Function PlannerLegendGrid() As DataGridView
        Dim legend As New DataGridView With {
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoGenerateColumns = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ColumnHeadersHeight = 30,
            .Dock = DockStyle.Fill,
            .EnableHeadersVisualStyles = False,
            .GridColor = Color.FromArgb(232, 236, 242),
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .RowTemplate = New DataGridViewRow With {.Height = 24},
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        }
        legend.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 46, 66)
        legend.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        legend.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        legend.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 235, 255)
        legend.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 31, 42)
        legend.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(PlannerPreviewRow.ColorName), .HeaderText = "", .Width = 34, .Resizable = DataGridViewTriState.False})
        legend.Columns.Add(New DataGridViewTextBoxColumn With {
            .DataPropertyName = NameOf(PlannerPreviewRow.TaskName),
            .HeaderText = "Task",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .MinimumWidth = 220,
            .DefaultCellStyle = New DataGridViewCellStyle With {.WrapMode = DataGridViewTriState.True}
        })
        legend.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(PlannerPreviewRow.DurationText), .HeaderText = "Duration", .Width = 86, .Resizable = DataGridViewTriState.False})
        AddHandler legend.CellFormatting, AddressOf PlannerLegendCellFormatting
        AddHandler legend.CellToolTipTextNeeded, AddressOf PlannerLegendToolTipTextNeeded
        Return legend
    End Function

    Private Sub AddGridColumns()
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ScheduleTask.TaskId), .HeaderText = "ID", .Width = 46, .ReadOnly = True})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ScheduleTask.TaskName), .HeaderText = "Task Name", .Width = 303})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ScheduleTask.DurationDays), .HeaderText = "Duration (Days)", .Width = 104, .ValueType = GetType(Decimal), .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "0.###"}})
        _grid.Columns.Add(New CalendarColumn With {.DataPropertyName = NameOf(ScheduleTask.StartDate), .HeaderText = "Start", .Width = 104})
        _grid.Columns.Add(New CalendarColumn With {.DataPropertyName = NameOf(ScheduleTask.FinishDate), .HeaderText = "Finish", .Width = 104})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ScheduleTask.PercentComplete), .HeaderText = "%", .Width = 52})
        Dim predecessorColumn As New DataGridViewComboBoxColumn With {
            .DataPropertyName = NameOf(ScheduleTask.PredecessorLink),
            .HeaderText = "Predecessors",
            .Width = 108,
            .FlatStyle = FlatStyle.Flat,
            .DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            .ValueType = GetType(String)
        }
        predecessorColumn.Items.AddRange("", "FS", "SS", "FF", "SF")
        _grid.Columns.Add(predecessorColumn)
        _grid.Columns.Add(New ResourceChecklistColumn(_employees) With {.DataPropertyName = NameOf(ScheduleTask.AssignedTo), .HeaderText = "Assigned To", .Width = 210})
        _grid.Columns.Add(New CalendarColumn With {.DataPropertyName = NameOf(ScheduleTask.AssignmentDate), .HeaderText = "Assign Date", .Width = 104})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ScheduleTask.ResourceHours), .HeaderText = "Resource Hours", .Width = 108, .ValueType = GetType(Decimal), .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "0.##"}})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ScheduleTask.ModuleId), .HeaderText = "Module", .Width = 62})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(ScheduleTask.PlannerTaskId), .HeaderText = "Planner ID", .Width = 110})
    End Sub

    Private Sub SeedProject()
        _totalProjectHours.Value = 176
        _resourcesNeeded.Value = 5

        _tasks.Add(New ScheduleTask With {.TaskId = 1, .DatabaseTaskId = 1, .TaskName = "Input study and Copy files to M Files", .StartDate = Date.Today, .AssignmentDate = Date.Today, .DurationDays = 1, .PercentComplete = 0, .AssignedTo = "Devarajan", .ResourceNames = "Devarajan", .ResourceAllocations = "Devarajan=8", .ResourceHours = 8, .ModuleId = 1})
        _tasks.Add(New ScheduleTask With {.TaskId = 2, .DatabaseTaskId = 6, .TaskName = "Create Scope of work", .StartDate = Date.Today, .AssignmentDate = Date.Today, .DurationDays = DurationFromHours(16D), .PercentComplete = 0, .Predecessors = "1", .AssignedTo = "Mahaboob Basha", .ResourceNames = "Mahaboob Basha", .ResourceAllocations = "Mahaboob Basha=16", .ResourceHours = 16, .ModuleId = 1})
        _tasks.Add(New ScheduleTask With {.TaskId = 3, .DatabaseTaskId = 28, .TaskName = "3D Modeling Proposed", .StartDate = Date.Today, .AssignmentDate = Date.Today, .DurationDays = DurationFromHours(12D), .Predecessors = "2", .AssignedTo = "Aashiq Aliuddin", .ResourceNames = "Aashiq Aliuddin", .ResourceAllocations = "Aashiq Aliuddin=12", .ResourceHours = 12, .ModuleId = 4})
    End Sub

    Private Sub NewProject(sender As Object, e As EventArgs)
        If MessageBox.Show("Clear the current schedule and start a new project?", "New Project", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then
            Return
        End If

        ClearProjectForNewSchedule()
    End Sub

    Private Sub ClearProjectForNewSchedule()
        _tasks.Clear()
        _projectName.Text = "SMA Scheduler"
        _versionNumber.Text = "1.0"
        _totalProjectHours.Value = 0
        _resourcesNeeded.Value = 0
        RecalculateAndRefresh("New project created")
        ClearPlanningInputDisplays()
    End Sub

    Private Sub AddTask(sender As Object, e As EventArgs)
        Dim selectedCatalogTask = TryCast(_taskCatalogSelector.SelectedItem, TaskCatalogItem)
        If selectedCatalogTask IsNot Nothing Then
            AddTaskFromCatalog(selectedCatalogTask)
            Return
        End If

        Dim nextId = If(_tasks.Count = 0, 1, _tasks.Max(Function(t) t.TaskId) + 1)
        Dim startDate = NextWorkingDate(If(_tasks.Count = 0, Date.Today, _tasks.Max(Function(t) t.FinishDate).AddDays(1)))
        _tasks.Add(New ScheduleTask With {
                   .TaskId = nextId,
                   .TaskName = "New task " & nextId,
                   .StartDate = startDate,
                   .AssignmentDate = NextWorkingDate(Date.Today),
                   .DurationDays = 1,
                   .PercentComplete = 0,
                   .AssignedTo = "",
                   .ResourceNames = "",
                   .ResourceAllocations = "",
                   .DailyResourceAllocations = "",
                   .ResourceHours = 0D})
        SelectTask(nextId)
        RecalculateAndRefresh("Task added")
    End Sub

    Private Sub AddTaskFromCatalog(selectedCatalogTask As TaskCatalogItem)
        Dim nextId = If(_tasks.Count = 0, 1, _tasks.Max(Function(t) t.TaskId) + 1)
        Dim requestedHours = DefaultTaskHours(selectedCatalogTask)
        Dim startDate = PreferredTaskStartDate()

        _tasks.Add(New ScheduleTask With {
                   .TaskId = nextId,
                   .DatabaseTaskId = selectedCatalogTask.DatabaseTaskId,
                   .TaskName = selectedCatalogTask.Title,
                   .StartDate = startDate,
                   .AssignmentDate = startDate,
                   .DurationDays = DurationFromHours(requestedHours),
                   .PercentComplete = 0,
                   .Predecessors = If(_tasks.Count = 0, "", (_tasks.Count).ToString()),
                   .AssignedTo = "",
                   .ResourceNames = "",
                   .ResourceAllocations = "",
                   .DailyResourceAllocations = "",
                   .ResourceHours = 0D,
                   .ModuleId = selectedCatalogTask.ModuleId})

        SelectTask(nextId)
        RecalculateAndRefresh("Task added from allocation panel")
    End Sub

    Private Sub DeleteTask(sender As Object, e As EventArgs)
        Dim selected = SelectedTask()
        If selected Is Nothing Then
            Return
        End If

        Dim removedTaskId = selected.TaskId
        _tasks.Remove(selected)
        For Each task In _tasks
            Dim updatedPredecessors = _engine.ParsePredecessors(task.Predecessors).
                Where(Function(id) id <> removedTaskId).
                Select(Function(id) If(id > removedTaskId, id - 1, id)).
                ToList()
            task.Predecessors = String.Join(",", updatedPredecessors)
        Next
        RenumberTasks()
        RecalculateAndRefresh("Task deleted")
    End Sub

    Private Sub MoveTaskUp(sender As Object, e As EventArgs)
        MoveSelectedTask(-1)
    End Sub

    Private Sub MoveTaskDown(sender As Object, e As EventArgs)
        MoveSelectedTask(1)
    End Sub

    Private Sub MoveSelectedTask(direction As Integer)
        If _grid.CurrentRow Is Nothing Then
            Return
        End If

        Dim index = _grid.CurrentRow.Index
        Dim target = index + direction
        If target < 0 OrElse target >= _tasks.Count Then
            Return
        End If

        Dim task = _tasks(index)
        _suspendTaskEvents = True
        Try
            _tasks.RemoveAt(index)
            _tasks.Insert(target, task)
            RemapPredecessorsForCurrentOrder()
            RenumberTasks()
        Finally
            _suspendTaskEvents = False
        End Try

        _grid.ClearSelection()
        _grid.Rows(target).Selected = True
        _grid.CurrentCell = _grid.Rows(target).Cells(1)
        RecalculateAndRefresh("Task moved")
    End Sub

    Private Sub LinkSelectedTask(sender As Object, e As EventArgs)
        If _grid.CurrentRow Is Nothing OrElse _grid.CurrentRow.Index <= 0 Then
            Return
        End If

        Dim task = DirectCast(_grid.CurrentRow.DataBoundItem, ScheduleTask)
        Dim predecessor = _tasks(_grid.CurrentRow.Index - 1)
        task.Predecessors = predecessor.TaskId.ToString()
        task.DependencyType = "FS"
        RecalculateAndRefresh("Linked to previous task")
    End Sub

    Private Sub UnlinkSelectedTask(sender As Object, e As EventArgs)
        Dim task = SelectedTask()
        If task IsNot Nothing Then
            task.Predecessors = ""
            RecalculateAndRefresh("Dependencies cleared")
        End If
    End Sub

    Private Sub ToggleMilestone(sender As Object, e As EventArgs)
        Dim task = SelectedTask()
        If task Is Nothing Then
            Return
        End If

        task.DurationDays = 1
        task.TaskName = If(task.TaskName.StartsWith("Milestone: "), task.TaskName.Replace("Milestone: ", ""), "Milestone: " & task.TaskName)
        RecalculateAndRefresh("Milestone updated")
    End Sub

    Private Sub SaveProjectFile(sender As Object, e As EventArgs)
        If _tasks.Count = 0 Then
            MessageBox.Show(Me, "There is no task assigned for the project.", "No Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dialog As New SaveFileDialog()
            dialog.Title = "Export SMA schedule to Excel"
            dialog.Filter = "Excel workbook (*.xlsx)|*.xlsx"
            dialog.FileName = MakeSafeFileName(_projectName.Text & "_" & _versionNumber.Text) & ".xlsx"

            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Dim projectPath = dialog.FileName
            Dim folder = Path.GetDirectoryName(projectPath)
            Dim resourceMonth = ResourceWorkbookMonth()
            Dim resourcePath = Path.Combine(folder, "Capacity Planning_" & resourceMonth.ToString("yyyyMM") & ".xlsx")

            ApplyCapacityGridToSelectedTask()
            RecalculateAndRefresh("Project saved")
            _xlsxExportService.ExportProjectPlan(projectPath, _projectName.Text, _versionNumber.Text, _tasks)
            _xlsxExportService.ExportResourceMonth(resourcePath, _projectName.Text, _versionNumber.Text, _employees, _tasks, resourceMonth, _includeSaturdays.Checked)
            SaveEmployeeWorkspaceWorkbook()
            SaveProjectSnapshotToLibrary()
            MessageBox.Show(Me, "Project has been saved and Capacity Planning has been updated.", "Project Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
            SetStatus("Excel exported: " & Path.GetFileName(projectPath) & " and " & Path.GetFileName(resourcePath))
        End Using
    End Sub

    Private Sub RefreshCapacityPlanning(sender As Object, e As EventArgs)
        ApplyCapacityGridToSelectedTask()
        RecalculateAndRefresh("Capacity planning refreshed")
        If SaveEmployeeWorkspaceWorkbook() Then
            SaveProjectSnapshotToLibrary()
        End If
        MessageBox.Show(Me, "Capacity Planning has been refreshed.", "Capacity Planning", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub OpenProjectFile(sender As Object, e As EventArgs)
        Using dialog As New OpenFileDialog()
            dialog.Title = "Open SMA schedule"
            dialog.Filter = "SMA schedule (*.smaschedule;*.json)|*.smaschedule;*.json"

            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Dim snapshot = JsonSerializer.Deserialize(Of ProjectSnapshot)(File.ReadAllText(dialog.FileName))
            If snapshot Is Nothing Then
                Return
            End If

            LoadProjectSnapshot(snapshot)
        End Using
    End Sub

    Private Sub ScheduleTasksThroughMsPlanner(sender As Object, e As EventArgs)
        If _tasks.Count = 0 Then
            MessageBox.Show(Me, "There is no task assigned for the project.", "No Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        SaveProjectSnapshotToLibrary()
        MessageBox.Show(Me, "The tasks have been assigned successfully.", "MS Planner", MessageBoxButtons.OK, MessageBoxIcon.Information)
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub GridCellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles _grid.CellValueChanged
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 OrElse _isRecalculating Then
            Return
        End If

        Dim task = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, ScheduleTask)
        If task Is Nothing Then
            Return
        End If

        NormalizeGridEdit(task, _grid.Columns(e.ColumnIndex).DataPropertyName)
        If Not _suspendTaskEvents Then
            RecalculateAndRefresh("Schedule recalculated")
        End If
    End Sub

    Private Sub GridCellBeginEdit(sender As Object, e As DataGridViewCellCancelEventArgs) Handles _grid.CellBeginEdit
        If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 AndAlso IsScheduleEditColumn(_grid.Columns(e.ColumnIndex).DataPropertyName) Then
            _suspendTaskEvents = True
        End If
    End Sub

    Private Sub GridCellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles _grid.CellEndEdit
        If _suspendTaskEvents Then
            _suspendTaskEvents = False
            RecalculateAndRefresh("Schedule recalculated")
        End If
    End Sub

    Private Sub GridCellParsing(sender As Object, e As DataGridViewCellParsingEventArgs) Handles _grid.CellParsing
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then
            Return
        End If

        Dim propertyName = _grid.Columns(e.ColumnIndex).DataPropertyName
        If propertyName = NameOf(ScheduleTask.ResourceHours) OrElse propertyName = NameOf(ScheduleTask.DurationDays) Then
            Dim parsedDecimal As Decimal
            If Decimal.TryParse(Convert.ToString(e.Value, CultureInfo.CurrentCulture), NumberStyles.Number, CultureInfo.CurrentCulture, parsedDecimal) OrElse
                Decimal.TryParse(Convert.ToString(e.Value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, parsedDecimal) Then
                e.Value = Math.Max(0D, parsedDecimal)
                e.ParsingApplied = True
            End If
            Return
        End If

        If propertyName <> NameOf(ScheduleTask.StartDate) AndAlso propertyName <> NameOf(ScheduleTask.FinishDate) AndAlso propertyName <> NameOf(ScheduleTask.AssignmentDate) Then
            Return
        End If

        Dim parsedDate As Date
        If TryParseGridDate(Convert.ToString(e.Value, CultureInfo.CurrentCulture), parsedDate) Then
            e.Value = parsedDate.Date
            e.ParsingApplied = True
        End If
    End Sub

    Private Sub GridCurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles _grid.CurrentCellDirtyStateChanged
        If _grid.IsCurrentCellDirty AndAlso (TypeOf _grid.EditingControl Is DataGridViewComboBoxEditingControl OrElse TypeOf _grid.EditingControl Is CalendarEditingControl) Then
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub GridDataError(sender As Object, e As DataGridViewDataErrorEventArgs) Handles _grid.DataError
        e.ThrowException = False
        SetStatus("Please enter a valid value")
    End Sub

    Private Sub TasksChanged(sender As Object, e As ListChangedEventArgs)
        If _suspendTaskEvents Then
            Return
        End If

        RecalculateAndRefresh("Schedule updated")
        UpdateCapacityPlanningGrid()
    End Sub

    Private Sub NormalizeGridEdit(task As ScheduleTask, propertyName As String)
        Select Case propertyName
            Case NameOf(ScheduleTask.StartDate)
                task.StartDate = NormalizePickedWorkingDate(task.StartDate)
                If task.FinishDate < task.StartDate Then
                    task.FinishDate = task.StartDate
                End If
                task.FinishDate = NormalizePickedWorkingDate(task.FinishDate)
                ApplyDurationFromDateRange(task)
                CapHoursToDuration(task)
                task.DailyResourceAllocations = BuildDefaultDailyAllocations(task, ParseResourceAllocations(task.ResourceAllocations))

            Case NameOf(ScheduleTask.FinishDate)
                task.FinishDate = NormalizePickedWorkingDate(task.FinishDate)
                If task.FinishDate < task.StartDate Then
                    task.FinishDate = task.StartDate
                End If
                ApplyDurationFromDateRange(task)
                CapHoursToDuration(task)
                task.DailyResourceAllocations = BuildDefaultDailyAllocations(task, ParseResourceAllocations(task.ResourceAllocations))

            Case NameOf(ScheduleTask.DurationDays)
                task.DurationDays = Math.Max(0D, task.DurationDays)
                task.StartDate = NextWorkingDate(task.StartDate)
                task.FinishDate = FinishFromWorkingDuration(task.StartDate, task.DurationDays)
                CapHoursToDuration(task)
                task.DailyResourceAllocations = BuildDefaultDailyAllocations(task, ParseResourceAllocations(task.ResourceAllocations))

            Case NameOf(ScheduleTask.AssignedTo)
                task.AssignedTo = NormalizeResourceList(task.AssignedTo)
                task.ResourceNames = task.AssignedTo
                KeepOnlySelectedResources(task)
                UpdateCapacityPlanningGrid()
                SetStatus("Resources selected. Edit planned hours in Capacity Planning.")

            Case NameOf(ScheduleTask.AssignmentDate)
                task.AssignmentDate = NormalizePickedWorkingDate(task.AssignmentDate)

            Case NameOf(ScheduleTask.DependencyType), NameOf(ScheduleTask.PredecessorLink)
                task.DependencyType = NormalizeDependencyType(task.DependencyType)

            Case NameOf(ScheduleTask.ResourceHours)
                task.ResourceHours = Math.Max(0D, task.ResourceHours)
                If String.IsNullOrWhiteSpace(task.ResourceAllocations) OrElse ResourceNamesFromAssignment(task.ResourceAllocations).Count <= 1 Then
                    Dim names = ResourceNamesFromAssignment(task.AssignedTo)
                    If names.Count = 1 Then
                        task.ResourceAllocations = BuildResourceAllocationsString(New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase) From {{names(0), task.ResourceHours}})
                    End If
                End If
                task.DurationDays = DurationFromHours(task.ResourceHours)
                task.StartDate = NextWorkingDate(task.StartDate)
                task.FinishDate = FinishFromWorkingDuration(task.StartDate, task.DurationDays)
                task.DailyResourceAllocations = BuildDefaultDailyAllocations(task, ParseResourceAllocations(task.ResourceAllocations))
                SaveEmployeeWorkspaceWorkbook()
        End Select
    End Sub

    Private Sub ApplyDurationFromDateRange(task As ScheduleTask)
        task.DurationDays = WorkingDaysBetween(task.StartDate, task.FinishDate)
    End Sub

    Private Sub CapHoursToDuration(task As ScheduleTask)
        task.ResourceHours = ClampDecimal(task.ResourceHours, 0D, task.DurationDays * 8D)
    End Sub

    Private Function DurationFromHours(hours As Decimal) As Decimal
        If hours <= 0D Then
            Return 0D
        End If

        Return hours / 8D
    End Function

    Private Function NormalizePickedWorkingDate(value As Date) As Date
        Dim adjustedDate = NextWorkingDate(value)
        If adjustedDate <> value.Date Then
            MessageBox.Show(value.ToString("dd-MM-yyyy") & " is a non-working day. Date moved to " & adjustedDate.ToString("dd-MM-yyyy") & ".", "Non-working day", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
        Return adjustedDate
    End Function

    Private Function NextWorkingDate(value As Date) As Date
        Dim currentDate = value.Date
        While IsBlockedScheduleDate(currentDate)
            currentDate = currentDate.AddDays(1)
        End While
        Return currentDate
    End Function

    Private Function FinishFromWorkingDuration(startDate As Date, durationDays As Decimal) As Date
        Dim currentDate = NextWorkingDate(startDate)
        Dim remainingDays = Math.Max(1, CInt(Math.Ceiling(durationDays)))

        While remainingDays > 1
            currentDate = currentDate.AddDays(1)
            If Not IsBlockedScheduleDate(currentDate) Then
                remainingDays -= 1
            End If
        End While

        Return currentDate
    End Function

    Private Function WorkingDaysBetween(startDate As Date, finishDate As Date) As Decimal
        Dim currentDate = NextWorkingDate(startDate)
        Dim endDate = NextWorkingDate(finishDate)
        If endDate < currentDate Then
            Return 1
        End If

        Dim dayCount = 0
        While currentDate <= endDate
            If Not IsBlockedScheduleDate(currentDate) Then
                dayCount += 1
            End If
            currentDate = currentDate.AddDays(1)
        End While

        Return Math.Max(1, dayCount)
    End Function

    Private Function IsBlockedScheduleDate(value As Date) As Boolean
        Return value.DayOfWeek = DayOfWeek.Sunday OrElse
            (value.DayOfWeek = DayOfWeek.Saturday AndAlso Not _includeSaturdays.Checked)
    End Function

    Private Function NormalizeResourceList(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then
            Return ""
        End If

        Dim names = value.Split({","c, ";"c}, StringSplitOptions.RemoveEmptyEntries).
            Select(Function(name) name.Trim()).
            Where(Function(name) name.Length > 0).
            Distinct(StringComparer.OrdinalIgnoreCase)

        Return String.Join("; ", names)
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

    Private Sub KeepOnlySelectedResources(task As ScheduleTask)
        Dim selectedNames = ResourceNamesFromAssignment(task.AssignedTo)
        If selectedNames.Count = 0 Then
            task.ResourceAllocations = ""
            task.DailyResourceAllocations = ""
            task.ResourceHours = 0D
            Return
        End If

        Dim selectedSet = New HashSet(Of String)(selectedNames, StringComparer.OrdinalIgnoreCase)
        Dim daily = ParseDailyAllocations(task.DailyResourceAllocations).
            Where(Function(item)
                      Dim resourceName = item.Key.Split({"|"c}, 2, StringSplitOptions.None)(0)
                      Return selectedSet.Contains(resourceName)
                  End Function).
            ToDictionary(Function(item) item.Key, Function(item) item.Value, StringComparer.OrdinalIgnoreCase)

        Dim totals = selectedNames.ToDictionary(Function(name) name, Function(name) 0D, StringComparer.OrdinalIgnoreCase)
        If daily.Count > 0 Then
            For Each item In daily
                Dim resourceName = item.Key.Split({"|"c}, 2, StringSplitOptions.None)(0)
                totals(resourceName) += item.Value
            Next
        Else
            For Each item In ParseResourceAllocations(task.ResourceAllocations)
                If selectedSet.Contains(item.Key) Then
                    totals(item.Key) = item.Value
                End If
            Next
        End If

        task.ResourceAllocations = BuildResourceAllocationsString(totals)
        task.ResourceHours = totals.Values.Sum()
        task.DailyResourceAllocations = BuildDailyAllocationsString(daily)
        If task.ResourceHours > 0D Then
            task.DurationDays = DurationFromHours(task.ResourceHours)
        End If
    End Sub

    Private Function ResourceNamesFromAssignment(value As String) As List(Of String)
        If String.IsNullOrWhiteSpace(value) Then
            Return New List(Of String)()
        End If

        Return value.Split({","c, ";"c}, StringSplitOptions.RemoveEmptyEntries).
            Select(Function(name) name.Trim()).
            Where(Function(name) name.Length > 0).
            Distinct(StringComparer.OrdinalIgnoreCase).
            ToList()
    End Function

    Private Function ParseResourceAllocations(value As String) As Dictionary(Of String, Decimal)
        Dim result As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        If String.IsNullOrWhiteSpace(value) Then
            Return result
        End If

        For Each part In value.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
            Dim pieces = part.Split({"="c}, 2, StringSplitOptions.None)
            If pieces.Length <> 2 Then
                Continue For
            End If

            Dim hours As Decimal
            If Decimal.TryParse(pieces(1).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, hours) OrElse
                Decimal.TryParse(pieces(1).Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, hours) Then
                Dim name = pieces(0).Trim()
                If name.Length > 0 Then
                    result(name) = Math.Max(0D, hours)
                End If
            End If
        Next

        Return result
    End Function

    Private Function BuildResourceAllocationsString(resourceHours As Dictionary(Of String, Decimal)) As String
        Return String.Join("; ", resourceHours.
            Where(Function(item) Not String.IsNullOrWhiteSpace(item.Key) AndAlso item.Value > 0D).
            Select(Function(item) item.Key.Trim() & "=" & item.Value.ToString("0.##", CultureInfo.InvariantCulture)))
    End Function

    Private Sub ScheduleSelectionChanged(sender As Object, e As EventArgs)
        UpdateCapacityPlanningGrid()
    End Sub

    Private Sub UpdateEmbeddedPlannerPreview()
        If _plannerPieChart Is Nothing Then
            Return
        End If

        Dim taskList = _tasks.ToList()
        _plannerPieChart.UpdateTasks(taskList)
        If _plannerLegendGrid IsNot Nothing Then
            _plannerLegendGrid.DataSource = taskList.Select(Function(task, index) PlannerPreviewRow.FromTask(task, index)).ToList()
        End If

        If _plannerTaskCountLabel IsNot Nothing Then
            _plannerTaskCountLabel.Text = "Scheduled Tasks: " & taskList.Count.ToString(CultureInfo.InvariantCulture)
        End If
        If _plannerDurationLabel IsNot Nothing Then
            Dim duration = taskList.Sum(Function(task) Math.Max(0D, task.DurationDays))
            _plannerDurationLabel.Text = "Duration: " & duration.ToString("0.##", CultureInfo.InvariantCulture) & " days"
        End If
    End Sub

    Private Sub PlannerLegendCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> 0 Then
            Return
        End If

        Dim color = PlannerPieChartPanel.SliceColor(e.RowIndex)
        e.CellStyle.BackColor = color
        e.CellStyle.ForeColor = color
        e.Value = ""
        e.FormattingApplied = True
    End Sub

    Private Sub PlannerLegendToolTipTextNeeded(sender As Object, e As DataGridViewCellToolTipTextNeededEventArgs)
        If e.RowIndex < 0 OrElse _plannerLegendGrid Is Nothing Then
            Return
        End If

        Dim row = TryCast(_plannerLegendGrid.Rows(e.RowIndex).DataBoundItem, PlannerPreviewRow)
        If row Is Nothing Then
            Return
        End If

        e.ToolTipText = row.TaskName & " - " & row.DurationText
    End Sub

    Private Sub UpdateCapacityPlanningGrid()
        If _capacityGrid Is Nothing Then
            Return
        End If

        _isLoadingCapacityGrid = True
        Try
            _capacityDateColumns.Clear()
            _capacityGrid.Columns.Clear()
            _capacityGrid.Rows.Clear()

            Dim selected = SelectedTask()
            Dim names = If(selected Is Nothing, New List(Of String)(), ResourceNamesFromAssignment(selected.AssignedTo))
            If selected Is Nothing OrElse names.Count = 0 Then
                taskWorkspaceTitle.Text = "Capacity Planning"
                _capacityGrid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Capacity Planning", .Width = 520, .ReadOnly = True})
                _capacityGrid.Rows.Add("Select resources in the Assigned To column to show available hours and edit date-wise capacity.")
                Return
            End If

            _capacityGrid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Resource", .Width = 210, .ReadOnly = True, .Frozen = True})

            Dim startDate = selected.StartDate.Date
            Dim finishDate = If(selected.FinishDate < selected.StartDate, selected.StartDate.Date, selected.FinishDate.Date)
            taskWorkspaceTitle.Text = "Capacity Planning - " & If(startDate = finishDate, startDate.ToString("dd-MMM-yyyy"), startDate.ToString("dd-MMM-yyyy") & " to " & finishDate.ToString("dd-MMM-yyyy"))
            Dim currentDate = startDate
            While currentDate <= finishDate
                Dim column As New DataGridViewTextBoxColumn With {
                    .HeaderText = currentDate.ToString("dd-MMM"),
                    .Width = 112,
                    .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "0.##"}
                }
                Dim columnIndex = _capacityGrid.Columns.Add(column)
                _capacityDateColumns(columnIndex) = currentDate
                currentDate = currentDate.AddDays(1)
            End While

            Dim dateRowIndex = _capacityGrid.Rows.Add()
            Dim dateRow = _capacityGrid.Rows(dateRowIndex)
            dateRow.Tag = "DateHeader"
            dateRow.ReadOnly = True
            dateRow.DefaultCellStyle.BackColor = Color.FromArgb(225, 239, 255)
            dateRow.DefaultCellStyle.ForeColor = Color.FromArgb(24, 31, 42)
            dateRow.DefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
            dateRow.Cells(0).Value = "Date"
            For Each dateColumn In _capacityDateColumns
                dateRow.Cells(dateColumn.Key).Value = dateColumn.Value.ToString("dd-MMM-yyyy")
            Next

            For Each resourceName In names
                Dim availableRowIndex = _capacityGrid.Rows.Add()
                Dim availableRow = _capacityGrid.Rows(availableRowIndex)
                availableRow.Tag = "Available"
                availableRow.ReadOnly = True
                availableRow.Cells(0).Value = resourceName & " - Available"
                availableRow.DefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252)
                availableRow.DefaultCellStyle.ForeColor = Color.FromArgb(37, 47, 63)
                availableRow.DefaultCellStyle.Font = New Font("Segoe UI", 9.0F, FontStyle.Italic)
                For Each dateColumn In _capacityDateColumns
                    Dim workDate = dateColumn.Value
                    Dim cell = availableRow.Cells(dateColumn.Key)
                    If IsBlockedScheduleDate(workDate) Then
                        cell.Value = "HOLIDAY"
                        cell.Style.BackColor = Color.FromArgb(238, 240, 244)
                        cell.Style.ForeColor = Color.DimGray
                    Else
                        Dim availableHours = AvailableHoursForSelection(resourceName, workDate, selected.TaskId)
                        cell.Value = availableHours
                        cell.Style.BackColor = If(availableHours <= 0D, Color.FromArgb(255, 238, 200), Color.FromArgb(209, 242, 224))
                        cell.Style.ForeColor = If(availableHours <= 0D, Color.FromArgb(120, 70, 0), Color.FromArgb(22, 101, 52))
                    End If
                    cell.ToolTipText = "Available hours for " & resourceName & " on " & workDate.ToString("dd-MMM-yyyy")
                Next

                Dim rowIndex = _capacityGrid.Rows.Add()
                Dim row = _capacityGrid.Rows(rowIndex)
                row.Cells(0).Value = resourceName
                For Each dateColumn In _capacityDateColumns
                    Dim workDate = dateColumn.Value
                    Dim cell = row.Cells(dateColumn.Key)
                    If IsBlockedScheduleDate(workDate) Then
                        cell.Value = "HOLIDAY"
                        cell.ReadOnly = True
                    Else
                        cell.Value = TaskHoursOnDate(selected, resourceName, workDate)
                        cell.ReadOnly = False
                    End If
                    StyleCapacityCell(cell, resourceName, workDate, selected.TaskId)
                Next
            Next
        Finally
            _isLoadingCapacityGrid = False
        End Try
    End Sub

    Private Sub StyleCapacityCell(cell As DataGridViewCell, resourceName As String, workDate As Date, ignoredTaskId As Integer)
        If IsBlockedScheduleDate(workDate) Then
            cell.Style.BackColor = Color.FromArgb(238, 240, 244)
            cell.Style.ForeColor = Color.DimGray
            cell.ToolTipText = "Holiday / non-working day"
            Return
        End If

        Dim requested = DecimalCellValue(cell.Value)
        Dim available = AvailableHoursForSelection(resourceName, workDate, ignoredTaskId)
        cell.ToolTipText = "Available before this task: " & available.ToString("0.##") & " hrs"

        If requested > available OrElse requested > 8D Then
            cell.Style.BackColor = Color.FromArgb(255, 210, 210)
            cell.Style.ForeColor = Color.Firebrick
        ElseIf available <= 0D Then
            cell.Style.BackColor = Color.FromArgb(255, 238, 200)
            cell.Style.ForeColor = Color.FromArgb(120, 70, 0)
        Else
            cell.Style.BackColor = Color.FromArgb(224, 245, 232)
            cell.Style.ForeColor = Color.FromArgb(34, 94, 74)
        End If
    End Sub

    Private Sub CapacityGridCellParsing(sender As Object, e As DataGridViewCellParsingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 OrElse Not _capacityDateColumns.ContainsKey(e.ColumnIndex) Then
            Return
        End If

        Dim parsedDecimal As Decimal
        If Decimal.TryParse(Convert.ToString(e.Value, CultureInfo.CurrentCulture), NumberStyles.Number, CultureInfo.CurrentCulture, parsedDecimal) OrElse
            Decimal.TryParse(Convert.ToString(e.Value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, parsedDecimal) Then
            e.Value = Math.Max(0D, parsedDecimal)
            e.ParsingApplied = True
        End If
    End Sub

    Private Sub CapacityGridCellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If _isLoadingCapacityGrid OrElse e.RowIndex < 0 OrElse e.ColumnIndex < 0 OrElse Not _capacityDateColumns.ContainsKey(e.ColumnIndex) Then
            Return
        End If

        ApplyCapacityGridToSelectedTask()
    End Sub

    Private Sub CapacityGridDataError(sender As Object, e As DataGridViewDataErrorEventArgs)
        e.ThrowException = False
        SetStatus("Please enter a valid capacity hour value")
    End Sub

    Private Sub ApplyCapacityGridToSelectedTask()
        Dim task = SelectedTask()
        If task Is Nothing OrElse _capacityGrid Is Nothing OrElse _capacityDateColumns.Count = 0 Then
            Return
        End If

        Dim daily = New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        Dim totals = New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)

        For Each row As DataGridViewRow In _capacityGrid.Rows
            Dim rowKind = Convert.ToString(row.Tag, CultureInfo.InvariantCulture)
            If row.IsNewRow OrElse String.Equals(rowKind, "DateHeader", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(rowKind, "Available", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            Dim resourceName = Convert.ToString(row.Cells(0).Value, CultureInfo.CurrentCulture).Trim()
            If String.IsNullOrWhiteSpace(resourceName) Then
                Continue For
            End If

            totals(resourceName) = 0D
            For Each dateColumn In _capacityDateColumns
                Dim workDate = dateColumn.Value
                If IsBlockedScheduleDate(workDate) Then
                    Continue For
                End If

                Dim hours = Math.Max(0D, DecimalCellValue(row.Cells(dateColumn.Key).Value))
                row.Cells(dateColumn.Key).Value = hours
                StyleCapacityCell(row.Cells(dateColumn.Key), resourceName, workDate, task.TaskId)
                If hours > 0D Then
                    daily(DailyAllocationKey(resourceName, workDate)) = hours
                    totals(resourceName) += hours
                End If
            Next
        Next

        _suspendTaskEvents = True
        Try
            task.ResourceAllocations = BuildResourceAllocationsString(totals)
            task.ResourceHours = totals.Values.Sum()
            task.DailyResourceAllocations = BuildDailyAllocationsString(daily)
            task.DurationDays = DurationFromHours(task.ResourceHours)
            Dim activeDates = daily.Keys.Select(Function(key) key.Substring(key.LastIndexOf("|"c) + 1)).
                Select(Function(value) Date.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture)).
                ToList()
            If activeDates.Count > 0 Then
                task.StartDate = activeDates.Min()
                task.FinishDate = activeDates.Max()
            End If
        Finally
            _suspendTaskEvents = False
        End Try

        UpdateSummary()
        _grid.Refresh()
        _gantt.Invalidate()
        UpdateEmbeddedPlannerPreview()
        SetStatus("Capacity edited. Click Refresh Capacity Planning to update the workbook.")
    End Sub

    Private Function TaskHoursOnDate(task As ScheduleTask, resourceName As String, workDate As Date) As Decimal
        Dim daily = ParseDailyAllocations(task.DailyResourceAllocations)
        Dim value As Decimal
        If daily.TryGetValue(DailyAllocationKey(resourceName, workDate), value) Then
            Return value
        End If

        Return DistributedTaskHoursOnDate(task, resourceName, workDate)
    End Function

    Private Function DistributedTaskHoursOnDate(task As ScheduleTask, resourceName As String, workDate As Date) As Decimal
        If task Is Nothing OrElse task.ResourceHours <= 0D OrElse workDate < task.StartDate.Date OrElse workDate > task.FinishDate.Date OrElse IsBlockedScheduleDate(workDate) Then
            Return 0D
        End If

        Dim assigned = ParseResourceAllocations(task.ResourceAllocations)
        If assigned.Count = 0 OrElse Not assigned.ContainsKey(resourceName) Then
            Return 0D
        End If

        Dim previousWorkingDays = 0
        Dim cursor = task.StartDate.Date
        While cursor < workDate.Date
            If Not IsBlockedScheduleDate(cursor) Then
                previousWorkingDays += 1
            End If
            cursor = cursor.AddDays(1)
        End While

        Dim remainingForDate = assigned(resourceName) - previousWorkingDays * 8D
        Return ClampDecimal(remainingForDate, 0D, 8D)
    End Function

    Private Function BuildDefaultDailyAllocations(task As ScheduleTask, totals As Dictionary(Of String, Decimal)) As String
        Dim daily = New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        For Each item In totals
            Dim remaining = Math.Max(0D, item.Value)
            Dim workDate = task.StartDate.Date
            While remaining > 0D
                If Not IsBlockedScheduleDate(workDate) Then
                    Dim hours = Math.Min(8D, remaining)
                    daily(DailyAllocationKey(item.Key, workDate)) = hours
                    remaining -= hours
                End If
                workDate = workDate.AddDays(1)
            End While
        Next
        Return BuildDailyAllocationsString(daily)
    End Function

    Private Function ParseDailyAllocations(value As String) As Dictionary(Of String, Decimal)
        Dim result As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        If String.IsNullOrWhiteSpace(value) Then
            Return result
        End If

        For Each part In value.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
            Dim pieces = part.Split({"="c}, 2, StringSplitOptions.None)
            If pieces.Length <> 2 Then
                Continue For
            End If

            Dim hours As Decimal
            If Decimal.TryParse(pieces(1).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, hours) OrElse
                Decimal.TryParse(pieces(1).Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, hours) Then
                Dim key = pieces(0).Trim()
                If key.Length > 0 Then
                    result(key) = Math.Max(0D, hours)
                End If
            End If
        Next

        Return result
    End Function

    Private Function BuildDailyAllocationsString(daily As Dictionary(Of String, Decimal)) As String
        Return String.Join("; ", daily.
            Where(Function(item) item.Value > 0D).
            OrderBy(Function(item) item.Key).
            Select(Function(item) item.Key & "=" & item.Value.ToString("0.##", CultureInfo.InvariantCulture)))
    End Function

    Private Function DailyAllocationKey(resourceName As String, workDate As Date) As String
        Return resourceName.Trim() & "|" & workDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    End Function

    Private Function DecimalCellValue(value As Object) As Decimal
        If value Is Nothing Then
            Return 0D
        End If

        If TypeOf value Is Decimal OrElse TypeOf value Is Integer OrElse TypeOf value Is Double Then
            Return Convert.ToDecimal(value, CultureInfo.InvariantCulture)
        End If

        Dim parsed As Decimal
        If Decimal.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), NumberStyles.Number, CultureInfo.CurrentCulture, parsed) OrElse
            Decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, parsed) Then
            Return parsed
        End If

        Return 0D
    End Function

    Private Function SaveEmployeeWorkspaceWorkbook() As Boolean
        Try
            Dim resourceMonth = ResourceWorkbookMonth()
            Dim filePath = CapacityPlanningWorkbookPath(resourceMonth)
            Directory.CreateDirectory(Path.GetDirectoryName(filePath))
            Dim fileName = Path.GetFileName(filePath)
            _xlsxExportService.ExportResourceMonth(filePath, _projectName.Text, _versionNumber.Text, _employees, _tasks, resourceMonth, _includeSaturdays.Checked)
            SetStatus("Capacity Planning updated: " & fileName)
            Return True
        Catch ex As IOException
            SetStatus("Capacity Planning workbook is open or locked. Close the Excel file and save again.")
        Catch ex As UnauthorizedAccessException
            SetStatus("Capacity Planning workbook could not be saved. Check folder permission.")
        End Try
        Return False
    End Function

    Private Function BuildProjectSnapshot() As ProjectSnapshot
        Return New ProjectSnapshot With {
            .ProjectName = _projectName.Text,
            .VersionNumber = _versionNumber.Text,
            .ProjectSize = If(_projectSizeSelector.SelectedItem Is Nothing, "Small", CStr(_projectSizeSelector.SelectedItem)),
            .TotalProjectHours = _totalProjectHours.Value,
            .ResourcesNeeded = CInt(_resourcesNeeded.Value),
            .Tasks = _tasks.ToList(),
            .UpdatedOn = Date.Now
        }
    End Function

    Private Sub SaveProjectSnapshotToLibrary()
        Try
            _projectLibrary.SaveSnapshot(BuildProjectSnapshot())
        Catch ex As IOException
            SetStatus("Project schedule could not be stored in the planner library.")
        Catch ex As UnauthorizedAccessException
            SetStatus("Project schedule could not be stored. Check folder permission.")
        End Try
    End Sub

    Private Function CapacityPlanningWorkbookPath(monthDate As Date) As String
        Dim folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMA Scheduler", "Capacity Planning")
        Return Path.Combine(folder, "Capacity Planning_" & monthDate.ToString("yyyyMM") & ".xlsx")
    End Function

    Private Function IsScheduleEditColumn(propertyName As String) As Boolean
        Return propertyName = NameOf(ScheduleTask.StartDate) OrElse
            propertyName = NameOf(ScheduleTask.FinishDate) OrElse
            propertyName = NameOf(ScheduleTask.DurationDays) OrElse
            propertyName = NameOf(ScheduleTask.DependencyType) OrElse
            propertyName = NameOf(ScheduleTask.PredecessorLink) OrElse
            propertyName = NameOf(ScheduleTask.AssignedTo) OrElse
            propertyName = NameOf(ScheduleTask.AssignmentDate) OrElse
            propertyName = NameOf(ScheduleTask.ResourceHours)
    End Function

    Private Function SelectedTask() As ScheduleTask
        If _grid.CurrentRow Is Nothing Then
            Return Nothing
        End If
        Return TryCast(_grid.CurrentRow.DataBoundItem, ScheduleTask)
    End Function

    Private Sub SelectTask(taskId As Integer)
        For Each row As DataGridViewRow In _grid.Rows
            Dim task = TryCast(row.DataBoundItem, ScheduleTask)
            If task IsNot Nothing AndAlso task.TaskId = taskId Then
                row.Selected = True
                _grid.CurrentCell = row.Cells(1)
                Exit For
            End If
        Next
    End Sub

    Private Sub RenumberTasks()
        For i = 0 To _tasks.Count - 1
            _tasks(i).TaskId = i + 1
        Next
    End Sub

    Private Sub RemapPredecessorsForCurrentOrder()
        Dim idMap As New Dictionary(Of Integer, Integer)
        For i = 0 To _tasks.Count - 1
            If Not idMap.ContainsKey(_tasks(i).TaskId) Then
                idMap.Add(_tasks(i).TaskId, i + 1)
            End If
        Next

        For Each task In _tasks
            Dim currentNewId = If(idMap.ContainsKey(task.TaskId), idMap(task.TaskId), task.TaskId)
            Dim updatedPredecessors = _engine.ParsePredecessors(task.Predecessors).
                Where(Function(id) idMap.ContainsKey(id)).
                Select(Function(id) idMap(id)).
                Where(Function(id) id <> currentNewId).
                Distinct().
                ToList()
            task.Predecessors = String.Join(",", updatedPredecessors)
        Next
    End Sub

    Private Sub RecalculateAndRefresh(message As String)
        If _isRecalculating Then
            Return
        End If

        Try
            _isRecalculating = True
            _engine.IncludeSaturdays = _includeSaturdays.Checked
            _engine.Recalculate(_tasks)
            _grid.Refresh()
            _gantt.Invalidate()
            UpdateSummary()
            UpdateCapacityPlanningGrid()
            UpdateEmbeddedPlannerPreview()
            SetStatus(message)
        Finally
            _isRecalculating = False
        End Try
    End Sub

    Private Sub UpdateSummary()
        If _tasks.Count = 0 Then
            _summaryTitle.Text = "0 tasks"
            _summaryDates.Text = "No dates"
            _summaryProgress.Text = "0% complete"
            _summaryResources.Text = "No resources"
            Return
        End If

        Dim startDate = _tasks.Min(Function(t) t.StartDate)
        Dim finishDate = _tasks.Max(Function(t) t.FinishDate)
        Dim progress = CInt(Math.Round(_tasks.Average(Function(t) t.PercentComplete)))
        Dim assignedHours = _tasks.Sum(Function(t) t.ResourceHours)
        Dim resourceCount = _tasks.SelectMany(Function(t) t.ResourceNames.Split({","c, ";"c}, StringSplitOptions.RemoveEmptyEntries)).
            Select(Function(r) r.Trim()).
            Where(Function(r) r.Length > 0).
            Distinct(StringComparer.OrdinalIgnoreCase).
            Count()

        _summaryTitle.Text = _tasks.Count & " tasks"
        _summaryDates.Text = startDate.ToString("dd MMM") & " - " & finishDate.ToString("dd MMM yyyy")
        _summaryProgress.Text = progress & "% complete / " & assignedHours.ToString("0.##") & " hrs"
        _summaryResources.Text = resourceCount & " resources / " & assignedHours.ToString("0.##") & " hrs"
        UpdateRemainingHoursDisplay()
    End Sub

    Private Sub SetStatus(message As String)
        If _status IsNot Nothing Then
            _status.Text = message & "  |  Tasks: " & _tasks.Count & "  |  Updated: " & Date.Now.ToString("HH:mm:ss")
        End If
    End Sub

    Private Function MakeSafeFileName(value As String) As String
        Dim safeValue = If(String.IsNullOrWhiteSpace(value), "SMA Scheduler", value)
        For Each character In Path.GetInvalidFileNameChars()
            safeValue = safeValue.Replace(character, "_"c)
        Next
        Return safeValue
    End Function

    Private Function ClampDecimal(value As Decimal, minimum As Decimal, maximum As Decimal) As Decimal
        Return Math.Min(maximum, Math.Max(minimum, value))
    End Function

    Private Function AvailableHoursForSelection(employeeName As String, currentDate As Date, ignoredTaskId As Integer) As Decimal
        If String.IsNullOrWhiteSpace(employeeName) OrElse IsBlockedScheduleDate(currentDate) Then
            Return 0D
        End If

        Dim currentProjectUsed = 8D - _xlsxExportService.RemainingHours(employeeName, currentDate.Date, _tasks, _includeSaturdays.Checked, ignoredTaskId)
        Dim capacityWorkbookPath = CapacityPlanningWorkbookPath(New Date(currentDate.Year, currentDate.Month, 1))
        Dim currentProjectLabel = _xlsxExportService.ProjectVersionLabel(_projectName.Text, _versionNumber.Text)
        Dim existingExternalUsed = _xlsxExportService.AllocatedHoursFromCapacityPlanning(capacityWorkbookPath, employeeName, currentDate.Date, currentProjectLabel)

        Return Math.Max(0D, 8D - currentProjectUsed - existingExternalUsed)
    End Function

    Private Sub UpdateRemainingHoursDisplay()
        If _remainingHoursLabel Is Nothing OrElse _totalProjectHours Is Nothing Then
            Return
        End If

        Dim remainingHours = Math.Max(0D, _totalProjectHours.Value - _tasks.Sum(Function(task) task.ResourceHours))

        _remainingHoursLabel.Text = "Remaining: " & remainingHours.ToString("0.##") & " hrs"
    End Sub

    Private Function NextTaskStartDate() As Date
        If _tasks.Count = 0 Then
            Return NextWorkingDate(Date.Today)
        End If

        Return NextWorkingDate(_tasks.Max(Function(t) t.FinishDate).AddDays(1))
    End Function

    Private Function PreferredTaskStartDate() As Date
        Return NextTaskStartDate()
    End Function

    Private Function FindAvailableAllocationDate(employeeName As String, proposedDate As Date) As Date
        For offset = 0 To 365
            Dim candidate = proposedDate.Date.AddDays(offset)
            If IsBlockedScheduleDate(candidate) Then
                Continue For
            End If
            If AvailableHoursForSelection(employeeName, candidate, 0) > 0D Then
                Return candidate
            End If
        Next

        Return NextWorkingDate(proposedDate)
    End Function

    Private Function DefaultTaskHours(selectedCatalogTask As TaskCatalogItem) As Decimal
        Dim requestedHours = selectedCatalogTask.HoursForSize(CStr(_projectSizeSelector.SelectedItem))
        If requestedHours > 0D Then
            Return requestedHours
        End If

        Return 8D
    End Function

    Private Function DefaultEmployeeForTask(selectedCatalogTask As TaskCatalogItem) As String
        If selectedCatalogTask IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(selectedCatalogTask.Assignee) AndAlso _employees.Contains(selectedCatalogTask.Assignee) Then
            Return selectedCatalogTask.Assignee
        End If

        If _employees.Count > 0 Then
            Return _employees(0)
        End If

        Return "Unassigned"
    End Function

    Private Function ResourceWorkbookMonth() As Date
        If _tasks.Count > 0 Then
            Dim startDate = _tasks.Min(Function(task) task.StartDate)
            Return New Date(startDate.Year, startDate.Month, 1)
        End If

        Return New Date(Date.Today.Year, Date.Today.Month, 1)
    End Function

    Private Function TryParseGridDate(value As String, ByRef parsedDate As Date) As Boolean
        Dim formats = {"dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy"}
        Return Date.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, parsedDate) OrElse
            Date.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, parsedDate)
    End Function


    Private Sub _grid_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles _grid.CellContentClick

    End Sub

End Class

Public Class ProjectSnapshot
    Public Property ProjectName As String = "SMA Scheduler"
    Public Property VersionNumber As String = "1.0"
    Public Property PlannerPlan As String = "SMA Planner"
    Public Property ProjectSize As String = "Small"
    Public Property TotalProjectHours As Decimal
    Public Property ResourcesNeeded As Integer = 1
    Public Property ResourceHours As Decimal
    Public Property Tasks As New List(Of ScheduleTask)
    Public Property UpdatedOn As Date
End Class

Public Class SchedulerThemePalette
    Public Property Name As String = ""
    Public Property WindowBack As Color
    Public Property PanelBack As Color
    Public Property HeaderBack As Color
    Public Property CommandBack As Color
    Public Property CommandText As Color
    Public Property GridHeader As Color
    Public Property GridLine As Color
    Public Property Selection As Color
    Public Property AlternatingRow As Color
    Public Property Text As Color
    Public Property MutedText As Color
    Public Property Action As Color
    Public Property Divider As Color
    Public Property TileOne As Color
    Public Property TileTwo As Color
    Public Property TileThree As Color
    Public Property TileFour As Color

    Public Shared Function AllNames() As Object()
        Return Themes().Select(Function(theme) CObj(theme.Name)).ToArray()
    End Function

    Public Shared Function ThemeByName(themeName As String) As SchedulerThemePalette
        Dim selected = Themes().FirstOrDefault(Function(theme) String.Equals(theme.Name, themeName, StringComparison.OrdinalIgnoreCase))
        Return If(selected, Themes().First())
    End Function

    Private Shared Function Themes() As List(Of SchedulerThemePalette)
        Return New List(Of SchedulerThemePalette) From {
            New SchedulerThemePalette With {
                .Name = "Fresh",
                .WindowBack = Color.FromArgb(244, 246, 249),
                .PanelBack = Color.White,
                .HeaderBack = Color.FromArgb(229, 241, 255),
                .CommandBack = Color.FromArgb(35, 46, 66),
                .CommandText = Color.White,
                .GridHeader = Color.FromArgb(35, 46, 66),
                .GridLine = Color.FromArgb(232, 236, 242),
                .Selection = Color.FromArgb(219, 235, 255),
                .AlternatingRow = Color.FromArgb(250, 252, 255),
                .Text = Color.FromArgb(24, 31, 42),
                .MutedText = Color.FromArgb(75, 85, 99),
                .Action = Color.FromArgb(32, 164, 112),
                .Divider = Color.FromArgb(224, 229, 236),
                .TileOne = Color.FromArgb(223, 245, 232),
                .TileTwo = Color.FromArgb(255, 243, 205),
                .TileThree = Color.FromArgb(225, 239, 255),
                .TileFour = Color.FromArgb(248, 222, 234)
            },
            New SchedulerThemePalette With {
                .Name = "Sunrise",
                .WindowBack = Color.FromArgb(250, 247, 242),
                .PanelBack = Color.FromArgb(255, 253, 250),
                .HeaderBack = Color.FromArgb(255, 235, 214),
                .CommandBack = Color.FromArgb(95, 55, 50),
                .CommandText = Color.White,
                .GridHeader = Color.FromArgb(95, 55, 50),
                .GridLine = Color.FromArgb(236, 226, 214),
                .Selection = Color.FromArgb(255, 223, 186),
                .AlternatingRow = Color.FromArgb(255, 250, 245),
                .Text = Color.FromArgb(48, 38, 35),
                .MutedText = Color.FromArgb(102, 82, 75),
                .Action = Color.FromArgb(212, 95, 56),
                .Divider = Color.FromArgb(231, 216, 202),
                .TileOne = Color.FromArgb(225, 245, 232),
                .TileTwo = Color.FromArgb(255, 232, 190),
                .TileThree = Color.FromArgb(221, 238, 255),
                .TileFour = Color.FromArgb(255, 218, 226)
            },
            New SchedulerThemePalette With {
                .Name = "Mint",
                .WindowBack = Color.FromArgb(241, 248, 246),
                .PanelBack = Color.White,
                .HeaderBack = Color.FromArgb(215, 242, 235),
                .CommandBack = Color.FromArgb(26, 82, 83),
                .CommandText = Color.White,
                .GridHeader = Color.FromArgb(26, 82, 83),
                .GridLine = Color.FromArgb(220, 236, 234),
                .Selection = Color.FromArgb(202, 238, 230),
                .AlternatingRow = Color.FromArgb(248, 253, 252),
                .Text = Color.FromArgb(25, 43, 45),
                .MutedText = Color.FromArgb(71, 96, 97),
                .Action = Color.FromArgb(42, 157, 143),
                .Divider = Color.FromArgb(205, 225, 223),
                .TileOne = Color.FromArgb(211, 245, 229),
                .TileTwo = Color.FromArgb(255, 241, 194),
                .TileThree = Color.FromArgb(218, 234, 255),
                .TileFour = Color.FromArgb(240, 222, 255)
            },
            New SchedulerThemePalette With {
                .Name = "Graphite",
                .WindowBack = Color.FromArgb(241, 243, 245),
                .PanelBack = Color.FromArgb(252, 252, 253),
                .HeaderBack = Color.FromArgb(232, 235, 239),
                .CommandBack = Color.FromArgb(42, 45, 52),
                .CommandText = Color.White,
                .GridHeader = Color.FromArgb(42, 45, 52),
                .GridLine = Color.FromArgb(224, 228, 234),
                .Selection = Color.FromArgb(225, 233, 246),
                .AlternatingRow = Color.FromArgb(248, 249, 251),
                .Text = Color.FromArgb(24, 27, 32),
                .MutedText = Color.FromArgb(89, 96, 107),
                .Action = Color.FromArgb(50, 132, 120),
                .Divider = Color.FromArgb(212, 218, 226),
                .TileOne = Color.FromArgb(220, 240, 230),
                .TileTwo = Color.FromArgb(255, 239, 201),
                .TileThree = Color.FromArgb(226, 236, 251),
                .TileFour = Color.FromArgb(244, 225, 235)
            }
        }
    End Function
End Class

Public Class BlankNumericUpDown
    Inherits NumericUpDown

    Private _isDisplayBlank As Boolean

    Public Sub ClearDisplayedValue()
        _isDisplayBlank = True
        Text = ""
    End Sub

    Protected Overrides Sub UpdateEditText()
        If _isDisplayBlank Then
            Text = ""
            Return
        End If

        MyBase.UpdateEditText()
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If _isDisplayBlank Then
            _isDisplayBlank = False
            Text = ""
        End If

        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        If _isDisplayBlank Then
            _isDisplayBlank = False
            Text = ""
        End If

        MyBase.OnKeyPress(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If _isDisplayBlank Then
            _isDisplayBlank = False
            UpdateEditText()
        End If

        MyBase.OnMouseDown(e)
    End Sub
End Class

Public Class ResourceChecklistColumn
    Inherits DataGridViewColumn

    Public Sub New(employeeNames As IEnumerable(Of String))
        MyBase.New(New ResourceChecklistCell())
        Me.EmployeeNames = employeeNames.ToList()
    End Sub

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EmployeeNames As List(Of String)

    Public Overrides Function Clone() As Object
        Dim clonedColumn = CType(MyBase.Clone(), ResourceChecklistColumn)
        clonedColumn.EmployeeNames = New List(Of String)(EmployeeNames)
        Return clonedColumn
    End Function

    Public Overrides Property CellTemplate As DataGridViewCell
        Get
            Return MyBase.CellTemplate
        End Get
        Set(value As DataGridViewCell)
            If value IsNot Nothing AndAlso Not value.GetType().IsAssignableFrom(GetType(ResourceChecklistCell)) Then
                Throw New InvalidCastException("ResourceChecklistColumn requires a ResourceChecklistCell template.")
            End If

            MyBase.CellTemplate = value
        End Set
    End Property
End Class

Public Class ResourceChecklistCell
    Inherits DataGridViewTextBoxCell

    Public Overrides Sub InitializeEditingControl(rowIndex As Integer, initialFormattedValue As Object, dataGridViewCellStyle As DataGridViewCellStyle)
        MyBase.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle)
        Dim control = CType(DataGridView.EditingControl, ResourceChecklistEditingControl)
        Dim checklistColumn = TryCast(OwningColumn, ResourceChecklistColumn)
        control.SetEmployees(If(checklistColumn Is Nothing, Enumerable.Empty(Of String)(), checklistColumn.EmployeeNames))
        control.EditingControlFormattedValue = If(Value, "")
    End Sub

    Public Overrides ReadOnly Property EditType As Type
        Get
            Return GetType(ResourceChecklistEditingControl)
        End Get
    End Property

    Public Overrides ReadOnly Property ValueType As Type
        Get
            Return GetType(String)
        End Get
    End Property

    Public Overrides ReadOnly Property DefaultNewRowValue As Object
        Get
            Return ""
        End Get
    End Property
End Class

Public Class ResourceChecklistEditingControl
    Inherits UserControl
    Implements IDataGridViewEditingControl

    Private ReadOnly _textBox As New TextBox()
    Private ReadOnly _dropButton As New Button()
    Private ReadOnly _employees As New List(Of String)()
    Private _dataGridView As DataGridView
    Private _dropDown As ToolStripDropDown
    Private _checkedList As CheckedListBox
    Private _rowIndex As Integer
    Private _valueChanged As Boolean
    Private _isClosingDropDown As Boolean

    Public Sub New()
        BorderStyle = BorderStyle.None
        Margin = Padding.Empty
        Padding = Padding.Empty

        _textBox.BorderStyle = BorderStyle.FixedSingle
        _textBox.Dock = DockStyle.Fill
        _textBox.ReadOnly = True

        _dropButton.Dock = DockStyle.Right
        _dropButton.Width = 24
        _dropButton.Text = "v"
        _dropButton.FlatStyle = FlatStyle.Flat
        _dropButton.FlatAppearance.BorderSize = 0

        Controls.Add(_textBox)
        Controls.Add(_dropButton)

        AddHandler _textBox.Click, Sub() ShowDropDown()
        AddHandler _dropButton.Click, Sub() ShowDropDown()
        AddHandler _textBox.KeyDown, AddressOf EditorKeyDown
        AddHandler _dropButton.KeyDown, AddressOf EditorKeyDown
    End Sub

    Public Sub SetEmployees(employeeNames As IEnumerable(Of String))
        _employees.Clear()
        _employees.AddRange(employeeNames.Where(Function(name) Not String.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
    End Sub

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlFormattedValue As Object Implements IDataGridViewEditingControl.EditingControlFormattedValue
        Get
            Return _textBox.Text
        End Get
        Set(value As Object)
            _textBox.Text = NormalizeResourceValue(Convert.ToString(value, CultureInfo.CurrentCulture))
        End Set
    End Property

    Public Function GetEditingControlFormattedValue(context As DataGridViewDataErrorContexts) As Object Implements IDataGridViewEditingControl.GetEditingControlFormattedValue
        Return EditingControlFormattedValue
    End Function

    Public Sub ApplyCellStyleToEditingControl(dataGridViewCellStyle As DataGridViewCellStyle) Implements IDataGridViewEditingControl.ApplyCellStyleToEditingControl
        _textBox.Font = dataGridViewCellStyle.Font
        _textBox.ForeColor = dataGridViewCellStyle.ForeColor
        _textBox.BackColor = dataGridViewCellStyle.BackColor
        Font = dataGridViewCellStyle.Font
    End Sub

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlRowIndex As Integer Implements IDataGridViewEditingControl.EditingControlRowIndex
        Get
            Return _rowIndex
        End Get
        Set(value As Integer)
            _rowIndex = value
        End Set
    End Property

    Public Function EditingControlWantsInputKey(keyData As Keys, dataGridViewWantsInputKey As Boolean) As Boolean Implements IDataGridViewEditingControl.EditingControlWantsInputKey
        Select Case keyData And Keys.KeyCode
            Case Keys.Enter, Keys.Space, Keys.Down, Keys.Up, Keys.Left, Keys.Right, Keys.Escape
                Return True
            Case Else
                Return Not dataGridViewWantsInputKey
        End Select
    End Function

    Public Sub PrepareEditingControlForEdit(selectAll As Boolean) Implements IDataGridViewEditingControl.PrepareEditingControlForEdit
        BeginInvoke(New MethodInvoker(AddressOf ShowDropDown))
    End Sub

    Public ReadOnly Property RepositionEditingControlOnValueChange As Boolean Implements IDataGridViewEditingControl.RepositionEditingControlOnValueChange
        Get
            Return False
        End Get
    End Property

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlDataGridView As DataGridView Implements IDataGridViewEditingControl.EditingControlDataGridView
        Get
            Return _dataGridView
        End Get
        Set(value As DataGridView)
            _dataGridView = value
        End Set
    End Property

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlValueChanged As Boolean Implements IDataGridViewEditingControl.EditingControlValueChanged
        Get
            Return _valueChanged
        End Get
        Set(value As Boolean)
            _valueChanged = value
        End Set
    End Property

    Public ReadOnly Property EditingPanelCursor As Cursor Implements IDataGridViewEditingControl.EditingPanelCursor
        Get
            Return Cursors.IBeam
        End Get
    End Property

    Private Sub ShowDropDown()
        If _dropDown IsNot Nothing AndAlso Not _dropDown.IsDisposed AndAlso _dropDown.Visible Then
            Return
        End If

        _checkedList = New CheckedListBox With {
            .CheckOnClick = True,
            .BorderStyle = BorderStyle.None,
            .IntegralHeight = False,
            .Width = Math.Max(260, Width),
            .Height = Math.Min(320, Math.Max(120, _employees.Count * 24 + 8))
        }

        Dim selectedNames = SelectedResources(_textBox.Text)
        For Each employeeName In _employees
            _checkedList.Items.Add(employeeName, selectedNames.Contains(employeeName, StringComparer.OrdinalIgnoreCase))
        Next

        AddHandler _checkedList.ItemCheck, AddressOf CheckedListItemCheck
        AddHandler _checkedList.KeyDown, AddressOf EditorKeyDown

        Dim host As New ToolStripControlHost(_checkedList) With {
            .Margin = Padding.Empty,
            .Padding = Padding.Empty,
            .AutoSize = False,
            .Size = _checkedList.Size
        }

        _dropDown = New ToolStripDropDown With {
            .Padding = Padding.Empty,
            .AutoClose = True
        }
        _dropDown.Items.Add(host)
        AddHandler _dropDown.Closed, AddressOf DropDownClosed
        _dropDown.Show(Me, New Point(0, Height))
        _checkedList.Focus()
    End Sub

    Private Sub CheckedListItemCheck(sender As Object, e As ItemCheckEventArgs)
        BeginInvoke(New MethodInvoker(Sub()
                                          UpdateTextFromCheckedList()
                                          MarkDirty()
                                      End Sub))
    End Sub

    Private Sub DropDownClosed(sender As Object, e As ToolStripDropDownClosedEventArgs)
        If _isClosingDropDown Then
            Return
        End If

        _isClosingDropDown = True
        Dim closedDropDown = TryCast(sender, ToolStripDropDown)
        UpdateTextFromCheckedList()
        MarkDirty()

        If closedDropDown IsNot Nothing Then
            RemoveHandler closedDropDown.Closed, AddressOf DropDownClosed
        End If

        If Object.ReferenceEquals(_dropDown, closedDropDown) Then
            _dropDown = Nothing
        End If
        _checkedList = Nothing
        _isClosingDropDown = False
    End Sub

    Private Sub EditorKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            CommitSelection()
            e.Handled = True
            e.SuppressKeyPress = True
        ElseIf e.KeyCode = Keys.Escape Then
            If _dropDown IsNot Nothing AndAlso Not _dropDown.IsDisposed Then
                _dropDown.Close()
            End If
            e.Handled = True
            e.SuppressKeyPress = True
        ElseIf e.KeyCode = Keys.Space AndAlso Not (_dropDown IsNot Nothing AndAlso Not _dropDown.IsDisposed AndAlso _dropDown.Visible) Then
            ShowDropDown()
            e.Handled = True
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub CommitSelection()
        UpdateTextFromCheckedList()
        MarkDirty()

        Dim activeDropDown = _dropDown
        If activeDropDown IsNot Nothing AndAlso Not activeDropDown.IsDisposed Then
            _dropDown = Nothing
            activeDropDown.Close()
        End If

        If _dataGridView IsNot Nothing Then
            _dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit)
            _dataGridView.EndEdit()
        End If
    End Sub

    Private Sub UpdateTextFromCheckedList()
        If _checkedList Is Nothing Then
            Return
        End If

        _textBox.Text = String.Join("; ", _checkedList.CheckedItems.Cast(Of Object)().Select(Function(item) CStr(item)))
    End Sub

    Private Sub MarkDirty()
        _valueChanged = True
        If _dataGridView IsNot Nothing Then
            _dataGridView.NotifyCurrentCellDirty(True)
        End If
    End Sub

    Private Function NormalizeResourceValue(value As String) As String
        Return String.Join("; ", SelectedResources(value))
    End Function

    Private Function SelectedResources(value As String) As List(Of String)
        If String.IsNullOrWhiteSpace(value) Then
            Return New List(Of String)()
        End If

        Return value.Split({","c, ";"c}, StringSplitOptions.RemoveEmptyEntries).
            Select(Function(name) name.Trim()).
            Where(Function(name) name.Length > 0).
            Distinct(StringComparer.OrdinalIgnoreCase).
            ToList()
    End Function
End Class

Public Class CalendarColumn
    Inherits DataGridViewColumn

    Public Sub New()
        MyBase.New(New CalendarCell())
    End Sub

    Public Overrides Property CellTemplate As DataGridViewCell
        Get
            Return MyBase.CellTemplate
        End Get
        Set(value As DataGridViewCell)
            If value IsNot Nothing AndAlso Not value.GetType().IsAssignableFrom(GetType(CalendarCell)) Then
                Throw New InvalidCastException("CalendarColumn requires a CalendarCell template.")
            End If

            MyBase.CellTemplate = value
        End Set
    End Property
End Class

Public Class CalendarCell
    Inherits DataGridViewTextBoxCell

    Public Sub New()
        Style.Format = "dd-MM-yyyy"
    End Sub

    Public Overrides Sub InitializeEditingControl(rowIndex As Integer, initialFormattedValue As Object, dataGridViewCellStyle As DataGridViewCellStyle)
        MyBase.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle)
        Dim control = CType(DataGridView.EditingControl, CalendarEditingControl)
        If Value Is Nothing OrElse Value Is DBNull.Value Then
            control.Value = Date.Today
        Else
            control.Value = CDate(Value)
        End If
    End Sub

    Public Overrides ReadOnly Property EditType As Type
        Get
            Return GetType(CalendarEditingControl)
        End Get
    End Property

    Public Overrides ReadOnly Property ValueType As Type
        Get
            Return GetType(Date)
        End Get
    End Property

    Public Overrides ReadOnly Property DefaultNewRowValue As Object
        Get
            Return Date.Today
        End Get
    End Property
End Class

Public Class CalendarEditingControl
    Inherits DateTimePicker
    Implements IDataGridViewEditingControl

    Private _dataGridView As DataGridView
    Private _valueChanged As Boolean
    Private _rowIndex As Integer

    Public Sub New()
        Format = DateTimePickerFormat.Custom
        CustomFormat = "dd-MM-yyyy"
        ShowUpDown = False
    End Sub

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlFormattedValue As Object Implements IDataGridViewEditingControl.EditingControlFormattedValue
        Get
            Return Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
        End Get
        Set(value As Object)
            Dim parsedDate As Date
            If value IsNot Nothing AndAlso Date.TryParse(CStr(value), parsedDate) Then
                Value = parsedDate
            End If
        End Set
    End Property

    Public Function GetEditingControlFormattedValue(context As DataGridViewDataErrorContexts) As Object Implements IDataGridViewEditingControl.GetEditingControlFormattedValue
        Return EditingControlFormattedValue
    End Function

    Public Sub ApplyCellStyleToEditingControl(dataGridViewCellStyle As DataGridViewCellStyle) Implements IDataGridViewEditingControl.ApplyCellStyleToEditingControl
        Font = dataGridViewCellStyle.Font
        CalendarForeColor = dataGridViewCellStyle.ForeColor
        CalendarMonthBackground = dataGridViewCellStyle.BackColor
    End Sub

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlRowIndex As Integer Implements IDataGridViewEditingControl.EditingControlRowIndex
        Get
            Return _rowIndex
        End Get
        Set(value As Integer)
            _rowIndex = value
        End Set
    End Property

    Public Function EditingControlWantsInputKey(keyData As Keys, dataGridViewWantsInputKey As Boolean) As Boolean Implements IDataGridViewEditingControl.EditingControlWantsInputKey
        Select Case keyData And Keys.KeyCode
            Case Keys.Up, Keys.Down, Keys.PageDown, Keys.PageUp, Keys.F4, Keys.Enter, Keys.Escape, Keys.Tab
                Return True
            Case Else
                Return False
        End Select
    End Function

    Public Sub PrepareEditingControlForEdit(selectAll As Boolean) Implements IDataGridViewEditingControl.PrepareEditingControlForEdit
        BeginInvoke(New MethodInvoker(Sub() SendKeys.SendWait("%{DOWN}")))
    End Sub

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        e.Handled = True
        MyBase.OnKeyPress(e)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.Up, Keys.Down, Keys.PageDown, Keys.PageUp, Keys.F4, Keys.Enter, Keys.Escape, Keys.Tab
                MyBase.OnKeyDown(e)
            Case Else
                e.Handled = True
                e.SuppressKeyPress = True
        End Select
    End Sub

    Public ReadOnly Property RepositionEditingControlOnValueChange As Boolean Implements IDataGridViewEditingControl.RepositionEditingControlOnValueChange
        Get
            Return False
        End Get
    End Property

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlDataGridView As DataGridView Implements IDataGridViewEditingControl.EditingControlDataGridView
        Get
            Return _dataGridView
        End Get
        Set(value As DataGridView)
            _dataGridView = value
        End Set
    End Property

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property EditingControlValueChanged As Boolean Implements IDataGridViewEditingControl.EditingControlValueChanged
        Get
            Return _valueChanged
        End Get
        Set(value As Boolean)
            _valueChanged = value
        End Set
    End Property

    Public ReadOnly Property EditingPanelCursor As Cursor Implements IDataGridViewEditingControl.EditingPanelCursor
        Get
            Return MyBase.Cursor
        End Get
    End Property

    Protected Overrides Sub OnValueChanged(eventargs As EventArgs)
        _valueChanged = True
        If _dataGridView IsNot Nothing Then
            _dataGridView.NotifyCurrentCellDirty(True)
        End If

        MyBase.OnValueChanged(eventargs)
    End Sub
End Class

Public Class GanttPanel
    Inherits Panel

    Private ReadOnly _motionTimer As Timer
    Private _motionPhase As Single

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Tasks As BindingList(Of ScheduleTask)

    Public Sub New()
        DoubleBuffered = True
        BackColor = Color.White
        AutoScroll = True
        _motionTimer = New Timer With {.Interval = 45}
        AddHandler _motionTimer.Tick, AddressOf MotionTimerTick
        _motionTimer.Start()
    End Sub

    Private Sub MotionTimerTick(sender As Object, e As EventArgs)
        If IsDisposed Then
            _motionTimer.Stop()
            Return
        End If

        _motionPhase += 0.035F
        If _motionPhase > 1.0F Then
            _motionPhase -= 1.0F
        End If
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        If Tasks Is Nothing OrElse Tasks.Count = 0 Then
            TextRenderer.DrawText(e.Graphics, "No tasks", Font, New Point(24, 24), Color.DimGray)
            Return
        End If

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        Dim minDate = Tasks.Min(Function(t) t.StartDate).Date
        Dim maxDate = Tasks.Max(Function(t) t.FinishDate).Date
        Dim totalDays = Math.Max(1, CInt((maxDate - minDate).TotalDays) + 1)
        Dim dayWidth = Math.Max(20, Math.Min(38, (ClientSize.Width - 110) \ Math.Max(1, totalDays)))
        Dim left = 96
        Dim top = 68
        Dim rowHeight = 34
        Dim chartWidth = left + totalDays * dayWidth + 160
        Dim chartHeight = top + Tasks.Count * rowHeight + 64
        If AutoScrollMinSize.Width <> chartWidth OrElse AutoScrollMinSize.Height <> chartHeight Then
            AutoScrollMinSize = New Size(chartWidth, chartHeight)
        End If

        Dim scrollOffset = AutoScrollPosition
        e.Graphics.TranslateTransform(scrollOffset.X, scrollOffset.Y)
        Dim visibleLeft = -scrollOffset.X
        Dim visibleTop = -scrollOffset.Y

        Using headerBrush As New SolidBrush(Color.FromArgb(248, 250, 252)),
              weekendBrush As New SolidBrush(Color.FromArgb(245, 247, 250)),
              gridPen As New Pen(Color.FromArgb(226, 230, 235)),
              todayPen As New Pen(Color.FromArgb(220, 53, 69), 2),
              barBrush As New SolidBrush(Color.FromArgb(43, 120, 228)),
              milestoneBrush As New SolidBrush(Color.FromArgb(255, 152, 0)),
              progressBrush As New SolidBrush(Color.FromArgb(32, 156, 110)),
              dependencyPen As New Pen(Color.FromArgb(88, 101, 125), 1.4F)

            e.Graphics.FillRectangle(headerBrush, visibleLeft, visibleTop, ClientSize.Width, top - 8)
            dependencyPen.CustomEndCap = New AdjustableArrowCap(4, 5)

            Using monthFont As New Font(Font, FontStyle.Bold)
                Dim monthStart = New Date(minDate.Year, minDate.Month, 1)
                While monthStart <= maxDate
                    Dim monthEnd = monthStart.AddMonths(1).AddDays(-1)
                    Dim segmentStart = If(monthStart < minDate, minDate, monthStart)
                    Dim segmentEnd = If(monthEnd > maxDate, maxDate, monthEnd)

                    Dim startOffset = CInt((segmentStart - minDate).TotalDays)
                    Dim endOffset = CInt((segmentEnd - minDate).TotalDays) + 1
                    Dim monthRectangle = New Rectangle(left + startOffset * dayWidth, 8, Math.Max(dayWidth, (endOffset - startOffset) * dayWidth), 22)
                    If monthRectangle.Width >= 64 Then
                        TextRenderer.DrawText(e.Graphics, monthStart.ToString("MMM yyyy"), monthFont, monthRectangle, Color.FromArgb(30, 36, 48), TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
                    ElseIf monthRectangle.Width >= 34 Then
                        TextRenderer.DrawText(e.Graphics, monthStart.ToString("MMM"), monthFont, monthRectangle, Color.FromArgb(30, 36, 48), TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
                    End If

                    monthStart = monthStart.AddMonths(1)
                End While
            End Using

            For day = 0 To totalDays - 1
                Dim dateValue = minDate.AddDays(day)
                Dim x = left + day * dayWidth
                If dateValue.DayOfWeek = DayOfWeek.Saturday OrElse dateValue.DayOfWeek = DayOfWeek.Sunday Then
                    e.Graphics.FillRectangle(weekendBrush, x, top - 8, dayWidth, chartHeight - top + 8)
                End If
                e.Graphics.DrawLine(gridPen, x, top - 8, x, chartHeight)
                If dayWidth >= 18 Then
                    TextRenderer.DrawText(e.Graphics, dateValue.ToString("dd"), Font, New Rectangle(x, 34, dayWidth, 20), Color.DimGray, TextFormatFlags.HorizontalCenter)
                End If
            Next

            Dim todayOffset = CInt((Date.Today - minDate).TotalDays)
            If todayOffset >= 0 AndAlso todayOffset <= totalDays Then
                Dim todayX = left + todayOffset * dayWidth
                e.Graphics.DrawLine(todayPen, todayX, top - 8, todayX, chartHeight)
            End If
            Dim bars As New Dictionary(Of Integer, Rectangle)
            For i = 0 To Tasks.Count - 1
                Dim task = Tasks(i)
                Dim y = top + i * rowHeight
                Dim x = left + CInt((task.StartDate.Date - minDate).TotalDays) * dayWidth
                Dim width = Math.Max(dayWidth, CInt(Math.Ceiling(task.DurationDays)) * dayWidth)
                Dim bar = New Rectangle(x, y + 9, width, 16)
                bars(task.TaskId) = bar

                e.Graphics.DrawLine(gridPen, visibleLeft, y, Math.Max(chartWidth, visibleLeft + ClientSize.Width), y)
                TextRenderer.DrawText(e.Graphics, task.TaskId.ToString(), Font, New Rectangle(8, y + 5, 34, 24), Color.DimGray, TextFormatFlags.Right)
                TextRenderer.DrawText(e.Graphics, task.TaskName, Font, New Rectangle(48, y + 5, Math.Max(20, left - 54), 24), Color.FromArgb(45, 48, 56), TextFormatFlags.EndEllipsis)

                If task.TaskName.StartsWith("Milestone: ") Then
                    DrawMilestone(e.Graphics, milestoneBrush, New Point(bar.Left + 8, bar.Top + 8))
                Else
                    Dim progressWidth = CInt(width * (task.PercentComplete / 100.0R))
                    e.Graphics.FillRoundedRectangle(barBrush, bar, 4)
                    DrawMovingBarHighlight(e.Graphics, bar)
                    If progressWidth > 0 Then
                        e.Graphics.FillRoundedRectangle(progressBrush, New Rectangle(bar.X, bar.Y, progressWidth, bar.Height), 4)
                    End If
                End If

                TextRenderer.DrawText(e.Graphics, task.PercentComplete & "%", Font, New Rectangle(bar.Right + 6, y + 4, 46, 24), Color.DimGray)
            Next

            Dim parser As New ScheduleEngine()
            For Each task In Tasks
                If Not bars.ContainsKey(task.TaskId) Then
                    Continue For
                End If

                For Each predecessorId In parser.ParsePredecessors(task.Predecessors)
                    If bars.ContainsKey(predecessorId) Then
                        Dim fromBar = bars(predecessorId)
                        Dim toBar = bars(task.TaskId)
                        Dim fromPoint = New Point(fromBar.Right, fromBar.Top + fromBar.Height \ 2)
                        Dim toPoint = New Point(toBar.Left, toBar.Top + toBar.Height \ 2)
                        e.Graphics.DrawLine(dependencyPen, fromPoint, toPoint)
                    End If
                Next
            Next
        End Using
    End Sub

    Private Sub DrawMilestone(graphics As Graphics, brush As Brush, center As Point)
        Dim points = {
            New Point(center.X, center.Y - 9),
            New Point(center.X + 9, center.Y),
            New Point(center.X, center.Y + 9),
            New Point(center.X - 9, center.Y)
        }
        graphics.FillPolygon(brush, points)
    End Sub

    Private Sub DrawMovingBarHighlight(graphics As Graphics, bar As Rectangle)
        If bar.Width < 18 OrElse bar.Height < 8 Then
            Return
        End If

        Dim sweepWidth = Math.Max(14, Math.Min(34, bar.Width \ 2))
        Dim travel = bar.Width + sweepWidth * 2
        Dim sweepLeft = bar.Left - sweepWidth + CInt(travel * _motionPhase)
        Dim sweepBounds = New Rectangle(sweepLeft, bar.Top, sweepWidth, bar.Height)

        Using path As New GraphicsPath()
            Dim radius = 8
            path.AddArc(bar.Left, bar.Top, radius, radius, 180, 90)
            path.AddArc(bar.Right - radius, bar.Top, radius, radius, 270, 90)
            path.AddArc(bar.Right - radius, bar.Bottom - radius, radius, radius, 0, 90)
            path.AddArc(bar.Left, bar.Bottom - radius, radius, radius, 90, 90)
            path.CloseFigure()

            Dim previousClip = graphics.Clip
            graphics.SetClip(path, CombineMode.Intersect)
            Using brush As New LinearGradientBrush(sweepBounds, Color.FromArgb(0, Color.White), Color.FromArgb(105, Color.White), LinearGradientMode.Horizontal)
                Dim blend As New Blend With {
                    .Positions = New Single() {0.0F, 0.5F, 1.0F},
                    .Factors = New Single() {0.0F, 1.0F, 0.0F}
                }
                brush.Blend = blend
                graphics.FillRectangle(brush, sweepBounds)
            End Using
            graphics.Clip = previousClip
        End Using
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso _motionTimer IsNot Nothing Then
            _motionTimer.Stop()
            RemoveHandler _motionTimer.Tick, AddressOf MotionTimerTick
            _motionTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

Public Module GraphicsExtensions
    <Runtime.CompilerServices.Extension>
    Public Sub FillRoundedRectangle(graphics As Graphics, brush As Brush, bounds As Rectangle, radius As Integer)
        Using path As New GraphicsPath()
            Dim diameter = radius * 2
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90)
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90)
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90)
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90)
            path.CloseFigure()
            graphics.FillPath(brush, path)
        End Using
    End Sub
End Module

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class SMASchedulerForm
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        commandBar = New ToolStrip()
        btnNew = New ToolStripButton()
        btnOpen = New ToolStripButton()
        btnSave = New ToolStripButton()
        btnRefreshCapacity = New ToolStripButton()
        sepFile = New ToolStripSeparator()
        btnAddTask = New ToolStripButton()
        btnDelete = New ToolStripButton()
        btnMoveUp = New ToolStripButton()
        btnMoveDown = New ToolStripButton()
        sepTasks = New ToolStripSeparator()
        btnLink = New ToolStripButton()
        btnUnlink = New ToolStripButton()
        btnMilestone = New ToolStripButton()
        headerPanel = New Panel()
        appTitle = New Label()
        projectLabel = New Label()
        _projectName = New TextBox()
        versionLabel = New Label()
        _versionNumber = New TextBox()
        totalHoursLabel = New Label()
        _totalProjectHours = New BlankNumericUpDown()
        taskCatalogLabel = New Label()
        _taskCatalogSelector = New ComboBox()
        projectSizeLabel = New Label()
        _projectSizeSelector = New ComboBox()
        _includeSaturdays = New CheckBox()
        btnSchedulePlanner = New Button()
        _summaryTitle = New Label()
        _summaryDates = New Label()
        _summaryProgress = New Label()
        _summaryResources = New Label()
        resourcesNeededLabel = New Label()
        _resourcesNeeded = New BlankNumericUpDown()
        _remainingHoursLabel = New Label()
        contentSplit = New SplitContainer()
        mainSplit = New SplitContainer()
        _grid = New DataGridView()
        _gantt = New GanttPanel()
        _detailsPanel = New Panel()
        taskWorkspaceTitle = New Label()
        statusBar = New StatusStrip()
        _status = New ToolStripStatusLabel()
        commandBar.SuspendLayout()
        headerPanel.SuspendLayout()
        CType(_totalProjectHours, ComponentModel.ISupportInitialize).BeginInit()
        CType(_resourcesNeeded, ComponentModel.ISupportInitialize).BeginInit()
        CType(contentSplit, ComponentModel.ISupportInitialize).BeginInit()
        contentSplit.Panel1.SuspendLayout()
        contentSplit.Panel2.SuspendLayout()
        contentSplit.SuspendLayout()
        CType(mainSplit, ComponentModel.ISupportInitialize).BeginInit()
        mainSplit.Panel1.SuspendLayout()
        mainSplit.Panel2.SuspendLayout()
        mainSplit.SuspendLayout()
        CType(_grid, ComponentModel.ISupportInitialize).BeginInit()
        _detailsPanel.SuspendLayout()
        statusBar.SuspendLayout()
        SuspendLayout()
        ' 
        ' commandBar
        ' 
        commandBar.BackColor = Color.FromArgb(CByte(35), CByte(46), CByte(66))
        commandBar.GripStyle = ToolStripGripStyle.Hidden
        commandBar.ImageScalingSize = New Size(18, 18)
        commandBar.Items.AddRange(New ToolStripItem() {btnNew, btnOpen, btnSave, btnRefreshCapacity, sepFile, btnAddTask, btnDelete, btnMoveUp, btnMoveDown, sepTasks, btnLink, btnUnlink, btnMilestone})
        commandBar.Location = New Point(0, 0)
        commandBar.Name = "commandBar"
        commandBar.Padding = New Padding(11, 9, 11, 9)
        commandBar.Size = New Size(1577, 45)
        commandBar.TabIndex = 0
        ' 
        ' btnNew
        ' 
        btnNew.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnNew.ForeColor = Color.White
        btnNew.Name = "btnNew"
        btnNew.Size = New Size(43, 24)
        btnNew.Text = "New"
        ' 
        ' btnOpen
        ' 
        btnOpen.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnOpen.ForeColor = Color.White
        btnOpen.Name = "btnOpen"
        btnOpen.Size = New Size(49, 24)
        btnOpen.Text = "Open"
        ' 
        ' btnSave
        ' 
        btnSave.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnSave.ForeColor = Color.White
        btnSave.Name = "btnSave"
        btnSave.Size = New Size(44, 24)
        btnSave.Text = "Save"
        ' 
        ' btnRefreshCapacity
        ' 
        btnRefreshCapacity.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnRefreshCapacity.ForeColor = Color.White
        btnRefreshCapacity.Name = "btnRefreshCapacity"
        btnRefreshCapacity.Size = New Size(184, 24)
        btnRefreshCapacity.Text = "Refresh Capacity Planning"
        ' 
        ' sepFile
        ' 
        sepFile.Name = "sepFile"
        sepFile.Size = New Size(6, 27)
        ' 
        ' btnAddTask
        ' 
        btnAddTask.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnAddTask.ForeColor = Color.White
        btnAddTask.Name = "btnAddTask"
        btnAddTask.Size = New Size(72, 24)
        btnAddTask.Text = "Add Task"
        ' 
        ' btnDelete
        ' 
        btnDelete.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnDelete.ForeColor = Color.White
        btnDelete.Name = "btnDelete"
        btnDelete.Size = New Size(57, 24)
        btnDelete.Text = "Delete"
        ' 
        ' btnMoveUp
        ' 
        btnMoveUp.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnMoveUp.ForeColor = Color.White
        btnMoveUp.Name = "btnMoveUp"
        btnMoveUp.Size = New Size(73, 24)
        btnMoveUp.Text = "Move Up"
        ' 
        ' btnMoveDown
        ' 
        btnMoveDown.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnMoveDown.ForeColor = Color.White
        btnMoveDown.Name = "btnMoveDown"
        btnMoveDown.Size = New Size(93, 24)
        btnMoveDown.Text = "Move Down"
        ' 
        ' sepTasks
        ' 
        sepTasks.Name = "sepTasks"
        sepTasks.Size = New Size(6, 27)
        ' 
        ' btnLink
        ' 
        btnLink.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnLink.ForeColor = Color.White
        btnLink.Name = "btnLink"
        btnLink.Size = New Size(39, 24)
        btnLink.Text = "Link"
        ' 
        ' btnUnlink
        ' 
        btnUnlink.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnUnlink.ForeColor = Color.White
        btnUnlink.Name = "btnUnlink"
        btnUnlink.Size = New Size(54, 24)
        btnUnlink.Text = "Unlink"
        ' 
        ' btnMilestone
        ' 
        btnMilestone.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnMilestone.ForeColor = Color.White
        btnMilestone.Name = "btnMilestone"
        btnMilestone.Size = New Size(78, 24)
        btnMilestone.Text = "Milestone"
        ' 
        ' headerPanel
        ' 
        headerPanel.BackColor = Color.FromArgb(CByte(229), CByte(241), CByte(255))
        headerPanel.Controls.Add(appTitle)
        headerPanel.Controls.Add(projectLabel)
        headerPanel.Controls.Add(_projectName)
        headerPanel.Controls.Add(versionLabel)
        headerPanel.Controls.Add(_versionNumber)
        headerPanel.Controls.Add(totalHoursLabel)
        headerPanel.Controls.Add(_totalProjectHours)
        headerPanel.Controls.Add(taskCatalogLabel)
        headerPanel.Controls.Add(_taskCatalogSelector)
        headerPanel.Controls.Add(projectSizeLabel)
        headerPanel.Controls.Add(_projectSizeSelector)
        headerPanel.Controls.Add(_includeSaturdays)
        headerPanel.Controls.Add(btnSchedulePlanner)
        headerPanel.Controls.Add(_summaryTitle)
        headerPanel.Controls.Add(_summaryDates)
        headerPanel.Controls.Add(_summaryProgress)
        headerPanel.Controls.Add(_summaryResources)
        headerPanel.Dock = DockStyle.Top
        headerPanel.Location = New Point(0, 45)
        headerPanel.Margin = New Padding(3, 4, 3, 4)
        headerPanel.Name = "headerPanel"
        headerPanel.Padding = New Padding(18, 16, 18, 16)
        headerPanel.Size = New Size(1577, 252)
        headerPanel.TabIndex = 1
        ' 
        ' appTitle
        ' 
        appTitle.AutoSize = True
        appTitle.Font = New Font("Segoe UI Semibold", 16.0F)
        appTitle.ForeColor = Color.FromArgb(CByte(24), CByte(31), CByte(42))
        appTitle.Location = New Point(0, 0)
        appTitle.Name = "appTitle"
        appTitle.Size = New Size(203, 37)
        appTitle.TabIndex = 0
        appTitle.Text = "SMA Scheduler"
        ' 
        ' projectLabel
        ' 
        projectLabel.AutoSize = True
        projectLabel.ForeColor = Color.DimGray
        projectLabel.Location = New Point(13, 42)
        projectLabel.Name = "projectLabel"
        projectLabel.Size = New Size(55, 20)
        projectLabel.TabIndex = 1
        projectLabel.Text = "Project"
        ' 
        ' _projectName
        ' 
        _projectName.BorderStyle = BorderStyle.FixedSingle
        _projectName.Location = New Point(13, 66)
        _projectName.Margin = New Padding(3, 4, 3, 4)
        _projectName.Name = "_projectName"
        _projectName.Size = New Size(263, 27)
        _projectName.TabIndex = 2
        _projectName.Text = "SMA Scheduler"
        ' 
        ' versionLabel
        ' 
        versionLabel.AutoSize = True
        versionLabel.ForeColor = Color.DimGray
        versionLabel.Location = New Point(291, 42)
        versionLabel.Name = "versionLabel"
        versionLabel.Size = New Size(57, 20)
        versionLabel.TabIndex = 15
        versionLabel.Text = "Version"
        ' 
        ' _versionNumber
        ' 
        _versionNumber.BorderStyle = BorderStyle.FixedSingle
        _versionNumber.Location = New Point(291, 66)
        _versionNumber.Margin = New Padding(3, 4, 3, 4)
        _versionNumber.Name = "_versionNumber"
        _versionNumber.Size = New Size(86, 27)
        _versionNumber.TabIndex = 16
        _versionNumber.Text = "1.0"
        ' 
        ' totalHoursLabel
        ' 
        totalHoursLabel.AutoSize = True
        totalHoursLabel.ForeColor = Color.DimGray
        totalHoursLabel.Location = New Point(412, 42)
        totalHoursLabel.Name = "totalHoursLabel"
        totalHoursLabel.Size = New Size(135, 20)
        totalHoursLabel.TabIndex = 9
        totalHoursLabel.Text = "Total Project Hours"
        ' 
        ' _totalProjectHours
        ' 
        _totalProjectHours.DecimalPlaces = 1
        _totalProjectHours.Location = New Point(412, 66)
        _totalProjectHours.Margin = New Padding(3, 4, 3, 4)
        _totalProjectHours.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        _totalProjectHours.Name = "_totalProjectHours"
        _totalProjectHours.Size = New Size(149, 27)
        _totalProjectHours.TabIndex = 10
        _totalProjectHours.ThousandsSeparator = True
        ' 
        ' taskCatalogLabel
        ' 
        taskCatalogLabel.AutoSize = True
        taskCatalogLabel.ForeColor = Color.DimGray
        taskCatalogLabel.Location = New Point(13, 108)
        taskCatalogLabel.Name = "taskCatalogLabel"
        taskCatalogLabel.Size = New Size(147, 20)
        taskCatalogLabel.TabIndex = 1
        taskCatalogLabel.Text = "Database Task Name"
        ' 
        ' _taskCatalogSelector
        ' 
        _taskCatalogSelector.DropDownStyle = ComboBoxStyle.DropDownList
        _taskCatalogSelector.FormattingEnabled = True
        _taskCatalogSelector.Location = New Point(13, 132)
        _taskCatalogSelector.Margin = New Padding(3, 4, 3, 4)
        _taskCatalogSelector.Name = "_taskCatalogSelector"
        _taskCatalogSelector.Size = New Size(427, 28)
        _taskCatalogSelector.TabIndex = 2
        ' 
        ' projectSizeLabel
        ' 
        projectSizeLabel.AutoSize = True
        projectSizeLabel.ForeColor = Color.DimGray
        projectSizeLabel.Location = New Point(460, 108)
        projectSizeLabel.Name = "projectSizeLabel"
        projectSizeLabel.Size = New Size(86, 20)
        projectSizeLabel.TabIndex = 3
        projectSizeLabel.Text = "Project Size"
        ' 
        ' _projectSizeSelector
        ' 
        _projectSizeSelector.DropDownStyle = ComboBoxStyle.DropDownList
        _projectSizeSelector.FormattingEnabled = True
        _projectSizeSelector.Location = New Point(460, 132)
        _projectSizeSelector.Margin = New Padding(3, 4, 3, 4)
        _projectSizeSelector.Name = "_projectSizeSelector"
        _projectSizeSelector.Size = New Size(134, 28)
        _projectSizeSelector.TabIndex = 4
        ' 
        ' _includeSaturdays
        ' 
        _includeSaturdays.AutoSize = True
        _includeSaturdays.Location = New Point(623, 132)
        _includeSaturdays.Name = "_includeSaturdays"
        _includeSaturdays.Size = New Size(89, 24)
        _includeSaturdays.TabIndex = 20
        _includeSaturdays.Text = "Saturday"
        _includeSaturdays.UseVisualStyleBackColor = True
        ' 
        ' btnSchedulePlanner
        ' 
        btnSchedulePlanner.BackColor = Color.FromArgb(CByte(32), CByte(164), CByte(112))
        btnSchedulePlanner.FlatAppearance.BorderSize = 0
        btnSchedulePlanner.FlatStyle = FlatStyle.Flat
        btnSchedulePlanner.Font = New Font("Segoe UI Semibold", 9.0F)
        btnSchedulePlanner.ForeColor = Color.White
        btnSchedulePlanner.Location = New Point(650, 58)
        btnSchedulePlanner.Name = "btnSchedulePlanner"
        btnSchedulePlanner.Size = New Size(190, 38)
        btnSchedulePlanner.TabIndex = 22
        btnSchedulePlanner.Text = "Schedule MS Planner"
        btnSchedulePlanner.UseVisualStyleBackColor = False
        ' 
        ' _summaryTitle
        ' 
        _summaryTitle.BackColor = Color.FromArgb(CByte(223), CByte(245), CByte(232))
        _summaryTitle.Font = New Font("Segoe UI Semibold", 10.0F)
        _summaryTitle.ForeColor = Color.FromArgb(CByte(37), CByte(47), CByte(63))
        _summaryTitle.Location = New Point(13, 170)
        _summaryTitle.Name = "_summaryTitle"
        _summaryTitle.Size = New Size(189, 56)
        _summaryTitle.TabIndex = 5
        _summaryTitle.Text = "0 tasks"
        _summaryTitle.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' _summaryDates
        ' 
        _summaryDates.BackColor = Color.FromArgb(CByte(255), CByte(243), CByte(205))
        _summaryDates.Font = New Font("Segoe UI Semibold", 10.0F)
        _summaryDates.ForeColor = Color.FromArgb(CByte(37), CByte(47), CByte(63))
        _summaryDates.Location = New Point(216, 170)
        _summaryDates.Name = "_summaryDates"
        _summaryDates.Size = New Size(206, 56)
        _summaryDates.TabIndex = 6
        _summaryDates.Text = "No dates"
        _summaryDates.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' _summaryProgress
        ' 
        _summaryProgress.BackColor = Color.FromArgb(CByte(225), CByte(239), CByte(255))
        _summaryProgress.Font = New Font("Segoe UI Semibold", 10.0F)
        _summaryProgress.ForeColor = Color.FromArgb(CByte(37), CByte(47), CByte(63))
        _summaryProgress.Location = New Point(442, 170)
        _summaryProgress.Name = "_summaryProgress"
        _summaryProgress.Size = New Size(171, 56)
        _summaryProgress.TabIndex = 7
        _summaryProgress.Text = "0% complete"
        _summaryProgress.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' _summaryResources
        ' 
        _summaryResources.BackColor = Color.FromArgb(CByte(248), CByte(222), CByte(234))
        _summaryResources.Font = New Font("Segoe UI Semibold", 10.0F)
        _summaryResources.ForeColor = Color.FromArgb(CByte(37), CByte(47), CByte(63))
        _summaryResources.Location = New Point(642, 170)
        _summaryResources.Name = "_summaryResources"
        _summaryResources.Size = New Size(189, 56)
        _summaryResources.TabIndex = 8
        _summaryResources.Text = "No resources"
        _summaryResources.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' resourcesNeededLabel
        ' 
        resourcesNeededLabel.AutoSize = True
        resourcesNeededLabel.ForeColor = Color.DimGray
        resourcesNeededLabel.Location = New Point(596, 56)
        resourcesNeededLabel.Name = "resourcesNeededLabel"
        resourcesNeededLabel.Size = New Size(132, 20)
        resourcesNeededLabel.TabIndex = 11
        resourcesNeededLabel.Text = "Resources Needed"
        ' 
        ' _resourcesNeeded
        ' 
        _resourcesNeeded.Location = New Point(596, 80)
        _resourcesNeeded.Margin = New Padding(3, 4, 3, 4)
        _resourcesNeeded.Maximum = New Decimal(New Integer() {500, 0, 0, 0})
        _resourcesNeeded.Name = "_resourcesNeeded"
        _resourcesNeeded.Size = New Size(137, 27)
        _resourcesNeeded.TabIndex = 12
        ' 
        ' _remainingHoursLabel
        ' 
        _remainingHoursLabel.AutoSize = True
        _remainingHoursLabel.ForeColor = Color.FromArgb(CByte(37), CByte(47), CByte(63))
        _remainingHoursLabel.Location = New Point(810, 122)
        _remainingHoursLabel.Name = "_remainingHoursLabel"
        _remainingHoursLabel.Size = New Size(118, 20)
        _remainingHoursLabel.TabIndex = 21
        _remainingHoursLabel.Text = "Remaining: 8 hrs"
        ' 
        ' contentSplit
        ' 
        contentSplit.BackColor = Color.FromArgb(CByte(224), CByte(229), CByte(236))
        contentSplit.Dock = DockStyle.Fill
        contentSplit.Location = New Point(0, 297)
        contentSplit.Margin = New Padding(3, 4, 3, 4)
        contentSplit.Name = "contentSplit"
        contentSplit.Orientation = Orientation.Horizontal
        ' 
        ' contentSplit.Panel1
        ' 
        contentSplit.Panel1.Controls.Add(mainSplit)
        contentSplit.Panel1MinSize = 430
        ' 
        ' contentSplit.Panel2
        ' 
        contentSplit.Panel2.Controls.Add(_detailsPanel)
        contentSplit.Panel2Collapsed = True
        contentSplit.Panel2MinSize = 130
        contentSplit.Size = New Size(1577, 732)
        contentSplit.SplitterDistance = 430
        contentSplit.SplitterWidth = 5
        contentSplit.TabIndex = 2
        ' 
        ' mainSplit
        ' 
        mainSplit.BackColor = Color.FromArgb(CByte(224), CByte(229), CByte(236))
        mainSplit.Dock = DockStyle.Fill
        mainSplit.Location = New Point(0, 0)
        mainSplit.Margin = New Padding(3, 4, 3, 4)
        mainSplit.Name = "mainSplit"
        ' 
        ' mainSplit.Panel1
        ' 
        mainSplit.Panel1.Controls.Add(_grid)
        mainSplit.Panel1MinSize = 620
        ' 
        ' mainSplit.Panel2
        ' 
        mainSplit.Panel2.Controls.Add(_gantt)
        mainSplit.Panel2MinSize = 320
        mainSplit.Size = New Size(1577, 732)
        mainSplit.SplitterDistance = 937
        mainSplit.SplitterWidth = 5
        mainSplit.TabIndex = 0
        ' 
        ' _grid
        ' 
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.BackgroundColor = Color.White
        _grid.BorderStyle = BorderStyle.None
        _grid.ColumnHeadersHeight = 34
        _grid.Dock = DockStyle.Fill
        _grid.EnableHeadersVisualStyles = False
        _grid.GridColor = Color.FromArgb(CByte(232), CByte(236), CByte(242))
        _grid.Location = New Point(0, 0)
        _grid.Margin = New Padding(3, 4, 3, 4)
        _grid.MultiSelect = False
        _grid.Name = "_grid"
        _grid.RowHeadersVisible = False
        _grid.RowHeadersWidth = 51
        _grid.RowTemplate.Height = 30
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.Size = New Size(937, 732)
        _grid.TabIndex = 0
        ' 
        ' _gantt
        ' 
        _gantt.AutoScroll = True
        _gantt.BackColor = Color.White
        _gantt.Dock = DockStyle.Fill
        _gantt.Location = New Point(0, 0)
        _gantt.Margin = New Padding(3, 4, 3, 4)
        _gantt.Name = "_gantt"
        _gantt.Size = New Size(635, 732)
        _gantt.TabIndex = 0
        ' 
        ' _detailsPanel
        ' 
        _detailsPanel.BackColor = Color.White
        _detailsPanel.Controls.Add(taskWorkspaceTitle)
        _detailsPanel.Dock = DockStyle.Fill
        _detailsPanel.Location = New Point(0, 0)
        _detailsPanel.Margin = New Padding(3, 4, 3, 4)
        _detailsPanel.Name = "_detailsPanel"
        _detailsPanel.Padding = New Padding(16, 19, 16, 19)
        _detailsPanel.Size = New Size(150, 46)
        _detailsPanel.TabIndex = 0
        ' 
        ' taskWorkspaceTitle
        ' 
        taskWorkspaceTitle.Dock = DockStyle.Top
        taskWorkspaceTitle.Font = New Font("Segoe UI Semibold", 10.5F)
        taskWorkspaceTitle.ForeColor = Color.FromArgb(CByte(24), CByte(31), CByte(42))
        taskWorkspaceTitle.Location = New Point(16, 19)
        taskWorkspaceTitle.Name = "taskWorkspaceTitle"
        taskWorkspaceTitle.Size = New Size(118, 37)
        taskWorkspaceTitle.TabIndex = 0
        taskWorkspaceTitle.Text = "Task allocation"
        ' 
        ' statusBar
        ' 
        statusBar.BackColor = Color.White
        statusBar.ImageScalingSize = New Size(20, 20)
        statusBar.Items.AddRange(New ToolStripItem() {_status})
        statusBar.Location = New Point(0, 1029)
        statusBar.Name = "statusBar"
        statusBar.Padding = New Padding(1, 0, 16, 0)
        statusBar.Size = New Size(1577, 26)
        statusBar.TabIndex = 3
        ' 
        ' _status
        ' 
        _status.Name = "_status"
        _status.Size = New Size(50, 20)
        _status.Text = "Ready"
        ' 
        ' SMASchedulerForm
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(244), CByte(246), CByte(249))
        ClientSize = New Size(1577, 1055)
        Controls.Add(contentSplit)
        Controls.Add(statusBar)
        Controls.Add(headerPanel)
        Controls.Add(commandBar)
        Font = New Font("Segoe UI", 9.0F)
        Margin = New Padding(3, 4, 3, 4)
        MinimumSize = New Size(1323, 891)
        Name = "SMASchedulerForm"
        Text = "SMA Scheduler"
        commandBar.ResumeLayout(False)
        commandBar.PerformLayout()
        headerPanel.ResumeLayout(False)
        headerPanel.PerformLayout()
        CType(_totalProjectHours, ComponentModel.ISupportInitialize).EndInit()
        CType(_resourcesNeeded, ComponentModel.ISupportInitialize).EndInit()
        contentSplit.Panel1.ResumeLayout(False)
        contentSplit.Panel2.ResumeLayout(False)
        CType(contentSplit, ComponentModel.ISupportInitialize).EndInit()
        contentSplit.ResumeLayout(False)
        mainSplit.Panel1.ResumeLayout(False)
        mainSplit.Panel2.ResumeLayout(False)
        CType(mainSplit, ComponentModel.ISupportInitialize).EndInit()
        mainSplit.ResumeLayout(False)
        CType(_grid, ComponentModel.ISupportInitialize).EndInit()
        _detailsPanel.ResumeLayout(False)
        statusBar.ResumeLayout(False)
        statusBar.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Private commandBar As ToolStrip
    Private btnNew As ToolStripButton
    Private btnOpen As ToolStripButton
    Private btnSave As ToolStripButton
    Private btnRefreshCapacity As ToolStripButton
    Private sepFile As ToolStripSeparator
    Private btnAddTask As ToolStripButton
    Private btnDelete As ToolStripButton
    Private btnMoveUp As ToolStripButton
    Private btnMoveDown As ToolStripButton
    Private sepTasks As ToolStripSeparator
    Private btnLink As ToolStripButton
    Private btnUnlink As ToolStripButton
    Private btnMilestone As ToolStripButton
    Private btnSchedulePlanner As Button
    Private headerPanel As Panel
    Private appTitle As Label
    Private projectLabel As Label
    Private versionLabel As Label
    Private totalHoursLabel As Label
    Private resourcesNeededLabel As Label
    Private contentSplit As SplitContainer
    Private mainSplit As SplitContainer
    Private projectSizeLabel As Label
    Private taskCatalogLabel As Label
    Private taskWorkspaceTitle As Label
    Private statusBar As StatusStrip
    Private _totalProjectHours As BlankNumericUpDown
    Private _resourcesNeeded As BlankNumericUpDown
End Class


Public Class PlannerPieChartPanel
    Inherits Panel

    Private _tasks As List(Of ScheduleTask)
    Private ReadOnly _motionTimer As System.Windows.Forms.Timer
    Private _animationProgress As Single = 1.0F
    Private _pulsePhase As Single = 0.0F
    Private _taskSignature As String = ""

    Private Shared ReadOnly SliceColors As Color() = {
        Color.FromArgb(45, 125, 221),
        Color.FromArgb(32, 164, 112),
        Color.FromArgb(245, 158, 11),
        Color.FromArgb(236, 72, 153),
        Color.FromArgb(99, 102, 241),
        Color.FromArgb(20, 184, 166),
        Color.FromArgb(239, 68, 68),
        Color.FromArgb(132, 204, 22)
    }

    Public Sub New(tasks As IEnumerable(Of ScheduleTask))
        DoubleBuffered = True
        _tasks = tasks.OrderBy(Function(task) task.TaskId).ToList()
        _taskSignature = BuildTaskSignature(_tasks)
        _motionTimer = New System.Windows.Forms.Timer With {.Interval = 35}
        AddHandler _motionTimer.Tick, AddressOf MotionTimerTick
        ResetMotion()
    End Sub

    Public Shared Function SliceColor(index As Integer) As Color
        Return SliceColors(index Mod SliceColors.Length)
    End Function

    Public Sub UpdateTasks(tasks As IEnumerable(Of ScheduleTask))
        Dim nextTasks = tasks.OrderBy(Function(task) task.TaskId).ToList()
        Dim nextSignature = BuildTaskSignature(nextTasks)
        Dim taskSetChanged = Not String.Equals(_taskSignature, nextSignature, StringComparison.Ordinal)
        _tasks = nextTasks
        _taskSignature = nextSignature
        If taskSetChanged Then
            ResetMotion()
        End If
        Invalidate()
    End Sub

    Private Shared Function BuildTaskSignature(tasks As IEnumerable(Of ScheduleTask)) As String
        Return String.Join("|", tasks.Select(Function(task) task.TaskId.ToString(CultureInfo.InvariantCulture) & ":" & task.TaskName & ":" & task.DurationDays.ToString("0.###", CultureInfo.InvariantCulture)))
    End Function

    Private Sub ResetMotion()
        _animationProgress = 0.0F
        _pulsePhase = 0.0F
        If _motionTimer IsNot Nothing AndAlso Not _motionTimer.Enabled Then
            _motionTimer.Start()
        End If
        Invalidate()
    End Sub

    Private Sub MotionTimerTick(sender As Object, e As EventArgs)
        If IsDisposed Then
            _motionTimer.Stop()
            Return
        End If

        If _animationProgress < 1.0F Then
            _animationProgress = Math.Min(1.0F, _animationProgress + 0.045F)
        End If
        _pulsePhase += 0.08F
        If _pulsePhase > CSng(Math.PI * 2) Then
            _pulsePhase -= CSng(Math.PI * 2)
        End If

        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        e.Graphics.Clear(Color.White)

        Dim totalDuration = _tasks.Sum(Function(task) Math.Max(0.01F, CSng(task.DurationDays)))
        If totalDuration <= 0 Then
            DrawEmpty(e.Graphics)
            Return
        End If

        Dim compact = ClientSize.Height < 260 OrElse ClientSize.Width < 340
        Dim titleHeight = If(compact, 0, 28)
        Dim topPadding = If(compact, 0, 36)
        Dim bottomPadding = If(compact, 0, 10)
        Dim sidePadding = If(compact, 0, 24)
        Dim drawingHeight = Math.Max(1, ClientSize.Height - topPadding - bottomPadding)
        Dim diameter = Math.Min(ClientSize.Width - (sidePadding * 2), drawingHeight)

        If diameter < 28 Then
            DrawCenteredText(e.Graphics, _tasks.Count.ToString(CultureInfo.InvariantCulture) & " tasks", New Font("Segoe UI Semibold", 9.0F), Color.FromArgb(24, 31, 42), ClientRectangle)
            Return
        End If

        Dim bounds As New Rectangle((ClientSize.Width - diameter) \ 2, topPadding + ((drawingHeight - diameter) \ 2), Math.Max(1, diameter - 2), Math.Max(1, diameter - 2))

        Dim startAngle As Single = -90.0F
        Dim remainingSweep As Single = 360.0F * EaseOutCubic(_animationProgress)
        For i = 0 To _tasks.Count - 1
            Dim sweep = CSng(Math.Max(0.01D, _tasks(i).DurationDays) / CDec(totalDuration) * 360D)
            Dim visibleSweep = Math.Min(sweep, remainingSweep)
            If visibleSweep <= 0 Then
                Exit For
            End If
            Using brush As New SolidBrush(SliceColor(i))
                e.Graphics.FillPie(brush, bounds, startAngle, visibleSweep)
            End Using
            startAngle += sweep
            remainingSweep -= visibleSweep
        Next

        Using pen As New Pen(Color.White, 2.0F)
            e.Graphics.DrawEllipse(pen, bounds)
        End Using

        Dim pulseSize = CInt(Math.Round((Math.Sin(_pulsePhase) + 1.0R) * 2.0R))
        Dim pulseBounds = Rectangle.Inflate(bounds, pulseSize, pulseSize)
        Dim pulseAlpha = CInt(32 + ((Math.Sin(_pulsePhase) + 1.0R) * 16.0R))
        Using pen As New Pen(Color.FromArgb(pulseAlpha, 45, 125, 221), 2.0F)
            e.Graphics.DrawEllipse(pen, pulseBounds)
        End Using

        Dim center = New Point(bounds.Left + bounds.Width \ 2, bounds.Top + bounds.Height \ 2)
        Dim innerDiameter = Math.Max(28, CInt(diameter * 0.55))
        Dim innerBounds As New Rectangle(center.X - innerDiameter \ 2, center.Y - innerDiameter \ 2, innerDiameter, innerDiameter)
        Using brush As New SolidBrush(Color.White)
            e.Graphics.FillEllipse(brush, innerBounds)
        End Using

        Dim title = _tasks.Count.ToString(CultureInfo.InvariantCulture)
        Dim subtitle = totalDuration.ToString("0.##", CultureInfo.InvariantCulture) & " days"
        Dim titleFontSize = CSng(Math.Max(9.0R, Math.Min(28.0R, diameter / 5.6R)))
        Dim subtitleFontSize = CSng(Math.Max(7.0R, Math.Min(12.0R, diameter / 15.5R)))
        Dim centerTitleTop = innerBounds.Top + CInt(innerBounds.Height * If(compact, 0.16R, 0.24R))
        DrawCenteredText(e.Graphics, title, New Font("Segoe UI Semibold", titleFontSize), Color.FromArgb(24, 31, 42), New Rectangle(innerBounds.Left, centerTitleTop, innerBounds.Width, Math.Max(18, CInt(innerBounds.Height * 0.28R))))
        If diameter >= 62 Then
            DrawCenteredText(e.Graphics, subtitle, New Font("Segoe UI", subtitleFontSize), Color.FromArgb(75, 85, 99), New Rectangle(innerBounds.Left, centerTitleTop + Math.Max(18, CInt(innerBounds.Height * 0.25R)), innerBounds.Width, Math.Max(16, CInt(innerBounds.Height * 0.22R))))
        End If
        If Not compact Then
            DrawCenteredText(e.Graphics, "Task duration split", New Font("Segoe UI Semibold", 12.0F), Color.FromArgb(24, 31, 42), New Rectangle(0, 4, ClientSize.Width, titleHeight))
        End If
    End Sub

    Private Shared Function EaseOutCubic(value As Single) As Single
        Dim progress = Math.Max(0.0F, Math.Min(1.0F, value))
        Dim inverse = 1.0F - progress
        Return 1.0F - (inverse * inverse * inverse)
    End Function

    Private Sub DrawEmpty(graphics As Graphics)
        DrawCenteredText(graphics, "No scheduled tasks", New Font("Segoe UI Semibold", 13.0F), Color.DimGray, ClientRectangle)
    End Sub

    Private Sub DrawCenteredText(graphics As Graphics, text As String, font As Font, color As Color, bounds As Rectangle)
        Using brush As New SolidBrush(color)
            Using format As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                graphics.DrawString(text, font, brush, bounds, format)
            End Using
        End Using
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso _motionTimer IsNot Nothing Then
            _motionTimer.Stop()
            RemoveHandler _motionTimer.Tick, AddressOf MotionTimerTick
            _motionTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

Public Class PlannerPreviewRow
    Public Property ColorName As String = ""
    Public Property TaskName As String = ""
    Public Property DurationText As String = ""
    Public Property DateRange As String = ""

    Public Shared Function FromTask(task As ScheduleTask, index As Integer) As PlannerPreviewRow
        Return New PlannerPreviewRow With {
            .ColorName = "",
            .TaskName = task.TaskName,
            .DurationText = task.DurationDays.ToString("0.##", CultureInfo.InvariantCulture) & " days",
            .DateRange = task.StartDate.ToString("dd-MMM", CultureInfo.InvariantCulture) & " - " & task.FinishDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
        }
    End Function
End Class

