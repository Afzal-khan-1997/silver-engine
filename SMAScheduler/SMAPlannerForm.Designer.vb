<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class SMAPlannerForm
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
        headerPanel = New Panel()
        titleLabel = New Label()
        promptLabel = New Label()
        btnNewProject = New Button()
        btnRefreshList = New Button()
        searchLabel = New Label()
        _liveProjectSearchBox = New TextBox()
        selectorLabel = New Label()
        _liveProjectSelector = New ComboBox()
        btnScheduleProject = New Button()
        _liveProjectSizeLabel = New Label()
        gridPanel = New Panel()
        _grid = New DataGridView()
        listTitle = New Label()
        _status = New Label()
        projectColumn = New DataGridViewTextBoxColumn()
        versionColumn = New DataGridViewTextBoxColumn()
        sizeColumn = New DataGridViewTextBoxColumn()
        tasksColumn = New DataGridViewTextBoxColumn()
        hoursColumn = New DataGridViewTextBoxColumn()
        startColumn = New DataGridViewTextBoxColumn()
        finishColumn = New DataGridViewTextBoxColumn()
        updatedColumn = New DataGridViewTextBoxColumn()
        headerPanel.SuspendLayout()
        gridPanel.SuspendLayout()
        CType(_grid, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' headerPanel
        ' 
        headerPanel.BackColor = Color.FromArgb(CByte(229), CByte(241), CByte(255))
        headerPanel.Controls.Add(titleLabel)
        headerPanel.Controls.Add(promptLabel)
        headerPanel.Controls.Add(btnNewProject)
        headerPanel.Controls.Add(btnRefreshList)
        headerPanel.Controls.Add(searchLabel)
        headerPanel.Controls.Add(_liveProjectSearchBox)
        headerPanel.Controls.Add(selectorLabel)
        headerPanel.Controls.Add(_liveProjectSelector)
        headerPanel.Controls.Add(btnScheduleProject)
        headerPanel.Controls.Add(_liveProjectSizeLabel)
        headerPanel.Dock = DockStyle.Top
        headerPanel.Location = New Point(0, 0)
        headerPanel.Name = "headerPanel"
        headerPanel.Padding = New Padding(24, 18, 24, 18)
        headerPanel.Size = New Size(1180, 210)
        headerPanel.TabIndex = 0
        ' 
        ' titleLabel
        ' 
        titleLabel.AutoSize = True
        titleLabel.Font = New Font("Segoe UI Semibold", 20.0F)
        titleLabel.ForeColor = Color.FromArgb(CByte(24), CByte(31), CByte(42))
        titleLabel.Location = New Point(24, 18)
        titleLabel.Name = "titleLabel"
        titleLabel.Size = New Size(185, 46)
        titleLabel.TabIndex = 0
        titleLabel.Text = "SMA Planner"
        ' 
        ' promptLabel
        ' 
        promptLabel.AutoSize = True
        promptLabel.Font = New Font("Segoe UI Semibold", 11.0F)
        promptLabel.ForeColor = Color.FromArgb(CByte(37), CByte(47), CByte(63))
        promptLabel.Location = New Point(26, 76)
        promptLabel.Name = "promptLabel"
        promptLabel.Size = New Size(340, 25)
        promptLabel.TabIndex = 1
        promptLabel.Text = "Do you want to plan for a new project?"
        ' 
        ' btnNewProject
        ' 
        btnNewProject.BackColor = Color.FromArgb(CByte(45), CByte(125), CByte(221))
        btnNewProject.FlatAppearance.BorderSize = 0
        btnNewProject.FlatStyle = FlatStyle.Flat
        btnNewProject.ForeColor = Color.White
        btnNewProject.Location = New Point(390, 70)
        btnNewProject.Name = "btnNewProject"
        btnNewProject.Size = New Size(140, 34)
        btnNewProject.TabIndex = 2
        btnNewProject.Text = "New Project"
        btnNewProject.UseVisualStyleBackColor = False
        ' 
        ' btnRefreshList
        ' 
        btnRefreshList.BackColor = Color.FromArgb(CByte(35), CByte(46), CByte(66))
        btnRefreshList.FlatAppearance.BorderSize = 0
        btnRefreshList.FlatStyle = FlatStyle.Flat
        btnRefreshList.ForeColor = Color.White
        btnRefreshList.Location = New Point(546, 70)
        btnRefreshList.Name = "btnRefreshList"
        btnRefreshList.Size = New Size(120, 34)
        btnRefreshList.TabIndex = 3
        btnRefreshList.Text = "Refresh List"
        btnRefreshList.UseVisualStyleBackColor = False
        ' 
        ' searchLabel
        ' 
        searchLabel.AutoSize = True
        searchLabel.ForeColor = Color.FromArgb(CByte(75), CByte(85), CByte(99))
        searchLabel.Location = New Point(26, 120)
        searchLabel.Name = "searchLabel"
        searchLabel.Size = New Size(97, 20)
        searchLabel.TabIndex = 4
        searchLabel.Text = "Find template"
        ' 
        ' _liveProjectSearchBox
        ' 
        _liveProjectSearchBox.Location = New Point(26, 144)
        _liveProjectSearchBox.Name = "_liveProjectSearchBox"
        _liveProjectSearchBox.PlaceholderText = "Search by template or size"
        _liveProjectSearchBox.Size = New Size(290, 27)
        _liveProjectSearchBox.TabIndex = 5
        ' 
        ' selectorLabel
        ' 
        selectorLabel.AutoSize = True
        selectorLabel.ForeColor = Color.FromArgb(CByte(75), CByte(85), CByte(99))
        selectorLabel.Location = New Point(336, 120)
        selectorLabel.Name = "selectorLabel"
        selectorLabel.Size = New Size(69, 20)
        selectorLabel.TabIndex = 6
        selectorLabel.Text = "Template"
        ' 
        ' _liveProjectSelector
        ' 
        _liveProjectSelector.DropDownStyle = ComboBoxStyle.DropDownList
        _liveProjectSelector.FormattingEnabled = True
        _liveProjectSelector.Location = New Point(336, 144)
        _liveProjectSelector.Name = "_liveProjectSelector"
        _liveProjectSelector.Size = New Size(360, 28)
        _liveProjectSelector.TabIndex = 7
        ' 
        ' btnScheduleProject
        ' 
        btnScheduleProject.BackColor = Color.FromArgb(CByte(32), CByte(164), CByte(112))
        btnScheduleProject.FlatAppearance.BorderSize = 0
        btnScheduleProject.FlatStyle = FlatStyle.Flat
        btnScheduleProject.ForeColor = Color.White
        btnScheduleProject.Location = New Point(716, 140)
        btnScheduleProject.Name = "btnScheduleProject"
        btnScheduleProject.Size = New Size(150, 34)
        btnScheduleProject.TabIndex = 8
        btnScheduleProject.Text = "Schedule Project"
        btnScheduleProject.UseVisualStyleBackColor = False
        ' 
        ' _liveProjectSizeLabel
        ' 
        _liveProjectSizeLabel.Font = New Font("Segoe UI Semibold", 9.0F)
        _liveProjectSizeLabel.ForeColor = Color.FromArgb(CByte(24), CByte(31), CByte(42))
        _liveProjectSizeLabel.Location = New Point(868, 140)
        _liveProjectSizeLabel.Name = "_liveProjectSizeLabel"
        _liveProjectSizeLabel.Size = New Size(260, 34)
        _liveProjectSizeLabel.TabIndex = 9
        _liveProjectSizeLabel.Text = "Template size:"
        _liveProjectSizeLabel.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' gridPanel
        ' 
        gridPanel.BackColor = Color.White
        gridPanel.Controls.Add(_grid)
        gridPanel.Controls.Add(listTitle)
        gridPanel.Controls.Add(_status)
        gridPanel.Dock = DockStyle.Fill
        gridPanel.Location = New Point(0, 210)
        gridPanel.Name = "gridPanel"
        gridPanel.Padding = New Padding(24, 22, 24, 16)
        gridPanel.Size = New Size(1180, 550)
        gridPanel.TabIndex = 1
        ' 
        ' _grid
        ' 
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.AutoGenerateColumns = False
        _grid.BackgroundColor = Color.White
        _grid.BorderStyle = BorderStyle.None
        _grid.ColumnHeadersHeight = 34
        _grid.Columns.AddRange(New DataGridViewColumn() {projectColumn, versionColumn, sizeColumn, tasksColumn, hoursColumn, startColumn, finishColumn, updatedColumn})
        _grid.Dock = DockStyle.Fill
        _grid.EnableHeadersVisualStyles = False
        _grid.GridColor = Color.FromArgb(CByte(232), CByte(236), CByte(242))
        _grid.Location = New Point(24, 56)
        _grid.MultiSelect = False
        _grid.Name = "_grid"
        _grid.ReadOnly = True
        _grid.RowHeadersVisible = False
        _grid.RowHeadersWidth = 51
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.Size = New Size(1132, 450)
        _grid.TabIndex = 1
        ' 
        ' listTitle
        ' 
        listTitle.Dock = DockStyle.Top
        listTitle.Font = New Font("Segoe UI Semibold", 12.0F)
        listTitle.ForeColor = Color.FromArgb(CByte(24), CByte(31), CByte(42))
        listTitle.Location = New Point(24, 22)
        listTitle.Name = "listTitle"
        listTitle.Size = New Size(1132, 34)
        listTitle.TabIndex = 0
        listTitle.Text = "Recent Scheduled Projects"
        ' 
        ' _status
        ' 
        _status.Dock = DockStyle.Bottom
        _status.ForeColor = Color.DimGray
        _status.Location = New Point(24, 506)
        _status.Name = "_status"
        _status.Size = New Size(1132, 28)
        _status.TabIndex = 2
        ' 
        ' projectColumn
        ' 
        projectColumn.DataPropertyName = "ProjectName"
        projectColumn.HeaderText = "Project"
        projectColumn.MinimumWidth = 6
        projectColumn.Name = "projectColumn"
        projectColumn.ReadOnly = True
        projectColumn.Width = 260
        ' 
        ' versionColumn
        ' 
        versionColumn.DataPropertyName = "VersionNumber"
        versionColumn.HeaderText = "Version"
        versionColumn.MinimumWidth = 6
        versionColumn.Name = "versionColumn"
        versionColumn.ReadOnly = True
        versionColumn.Width = 90
        ' 
        ' sizeColumn
        ' 
        sizeColumn.DataPropertyName = "ProjectSize"
        sizeColumn.HeaderText = "Size"
        sizeColumn.MinimumWidth = 6
        sizeColumn.Name = "sizeColumn"
        sizeColumn.ReadOnly = True
        sizeColumn.Width = 110
        ' 
        ' tasksColumn
        ' 
        tasksColumn.DataPropertyName = "TaskCount"
        tasksColumn.HeaderText = "Tasks"
        tasksColumn.MinimumWidth = 6
        tasksColumn.Name = "tasksColumn"
        tasksColumn.ReadOnly = True
        tasksColumn.Width = 80
        ' 
        ' hoursColumn
        ' 
        hoursColumn.DataPropertyName = "ResourceHours"
        hoursColumn.HeaderText = "Hours"
        hoursColumn.MinimumWidth = 6
        hoursColumn.Name = "hoursColumn"
        hoursColumn.ReadOnly = True
        hoursColumn.Width = 90
        ' 
        ' startColumn
        ' 
        startColumn.DataPropertyName = "StartDateText"
        startColumn.HeaderText = "Start"
        startColumn.MinimumWidth = 6
        startColumn.Name = "startColumn"
        startColumn.ReadOnly = True
        startColumn.Width = 120
        ' 
        ' finishColumn
        ' 
        finishColumn.DataPropertyName = "FinishDateText"
        finishColumn.HeaderText = "Finish"
        finishColumn.MinimumWidth = 6
        finishColumn.Name = "finishColumn"
        finishColumn.ReadOnly = True
        finishColumn.Width = 120
        ' 
        ' updatedColumn
        ' 
        updatedColumn.DataPropertyName = "UpdatedOn"
        updatedColumn.HeaderText = "Updated"
        updatedColumn.MinimumWidth = 6
        updatedColumn.Name = "updatedColumn"
        updatedColumn.ReadOnly = True
        updatedColumn.Width = 160
        ' 
        ' SMAPlannerForm
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(244), CByte(246), CByte(249))
        ClientSize = New Size(1180, 760)
        Controls.Add(gridPanel)
        Controls.Add(headerPanel)
        Font = New Font("Segoe UI", 9.0F)
        MinimumSize = New Size(980, 640)
        Name = "SMAPlannerForm"
        StartPosition = FormStartPosition.CenterScreen
        Text = "SMA Planner"
        headerPanel.ResumeLayout(False)
        headerPanel.PerformLayout()
        gridPanel.ResumeLayout(False)
        CType(_grid, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Private headerPanel As Panel
    Private titleLabel As Label
    Private promptLabel As Label
    Private btnNewProject As Button
    Private btnRefreshList As Button
    Private searchLabel As Label
    Private _liveProjectSearchBox As TextBox
    Private selectorLabel As Label
    Private _liveProjectSelector As ComboBox
    Private btnScheduleProject As Button
    Private _liveProjectSizeLabel As Label
    Private gridPanel As Panel
    Private _grid As DataGridView
    Private listTitle As Label
    Private _status As Label
    Private projectColumn As DataGridViewTextBoxColumn
    Private versionColumn As DataGridViewTextBoxColumn
    Private sizeColumn As DataGridViewTextBoxColumn
    Private tasksColumn As DataGridViewTextBoxColumn
    Private hoursColumn As DataGridViewTextBoxColumn
    Private startColumn As DataGridViewTextBoxColumn
    Private finishColumn As DataGridViewTextBoxColumn
    Private updatedColumn As DataGridViewTextBoxColumn
End Class
