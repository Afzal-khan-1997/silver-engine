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
        sepPlanner = New ToolStripSeparator()
        btnPlanner = New ToolStripButton()
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
        ' sepPlanner
        ' 
        sepPlanner.Name = "sepPlanner"
        sepPlanner.Size = New Size(6, 27)
        ' 
        ' btnPlanner
        ' 
        btnPlanner.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnPlanner.ForeColor = Color.White
        btnPlanner.Name = "btnPlanner"
        btnPlanner.Size = New Size(117, 24)
        btnPlanner.Text = "Planner Preview"
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

    Friend WithEvents commandBar As ToolStrip
    Friend WithEvents btnNew As ToolStripButton
    Friend WithEvents btnOpen As ToolStripButton
    Friend WithEvents btnSave As ToolStripButton
    Friend WithEvents btnRefreshCapacity As ToolStripButton
    Friend WithEvents sepFile As ToolStripSeparator
    Friend WithEvents btnAddTask As ToolStripButton
    Friend WithEvents btnDelete As ToolStripButton
    Friend WithEvents btnMoveUp As ToolStripButton
    Friend WithEvents btnMoveDown As ToolStripButton
    Friend WithEvents sepTasks As ToolStripSeparator
    Friend WithEvents btnLink As ToolStripButton
    Friend WithEvents btnUnlink As ToolStripButton
    Friend WithEvents btnMilestone As ToolStripButton
    Friend WithEvents sepPlanner As ToolStripSeparator
    Friend WithEvents btnPlanner As ToolStripButton
    Friend WithEvents btnSchedulePlanner As Button
    Friend WithEvents headerPanel As Panel
    Friend WithEvents appTitle As Label
    Friend WithEvents projectLabel As Label
    Friend WithEvents versionLabel As Label
    Friend WithEvents totalHoursLabel As Label
    Friend WithEvents resourcesNeededLabel As Label
    Friend WithEvents contentSplit As SplitContainer
    Friend WithEvents mainSplit As SplitContainer
    Friend WithEvents projectSizeLabel As Label
    Friend WithEvents taskCatalogLabel As Label
    Friend WithEvents taskWorkspaceTitle As Label
    Friend WithEvents statusBar As StatusStrip
    Private WithEvents _totalProjectHours As BlankNumericUpDown
    Private WithEvents _resourcesNeeded As BlankNumericUpDown
End Class
