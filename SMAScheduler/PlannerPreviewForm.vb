Imports System.Drawing.Drawing2D
Imports System.Globalization

Public Class PlannerPreviewForm
    Inherits Form

    Private _tasks As List(Of ScheduleTask)

    Public Sub New(projectName As String, tasks As IEnumerable(Of ScheduleTask))
        _tasks = tasks.OrderBy(Function(task) task.TaskId).ToList()

        Text = "Planner Preview"
        StartPosition = FormStartPosition.CenterParent
        MinimumSize = New Size(920, 620)
        Size = New Size(1080, 720)
        Font = New Font("Segoe UI", 9.0F)
        BackColor = Color.White

        BuildLayout(If(String.IsNullOrWhiteSpace(projectName), "SMA Scheduler", projectName.Trim()))
    End Sub

    Private Sub BuildLayout(projectName As String)
        Dim header As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 130,
            .BackColor = Color.FromArgb(229, 241, 255),
            .Padding = New Padding(24, 18, 24, 16)
        }

        Dim title As New Label With {
            .Text = "Planner Preview",
            .AutoSize = True,
            .Font = New Font("Segoe UI Semibold", 18.0F),
            .ForeColor = Color.FromArgb(24, 31, 42),
            .Location = New Point(24, 18)
        }

        Dim subtitle As New Label With {
            .Text = projectName,
            .AutoSize = True,
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = Color.FromArgb(75, 85, 99),
            .Location = New Point(27, 62)
        }

        header.Controls.Add(title)
        header.Controls.Add(subtitle)
        header.Controls.Add(SummaryTile("Scheduled Tasks", _tasks.Count.ToString(CultureInfo.InvariantCulture), New Point(420, 28), Color.FromArgb(223, 245, 232)))
        header.Controls.Add(SummaryTile("Project Duration", ProjectDurationText(), New Point(610, 28), Color.FromArgb(255, 243, 205)))
        header.Controls.Add(SummaryTile("Task Duration", TotalTaskDuration().ToString("0.##", CultureInfo.InvariantCulture) & " days", New Point(810, 28), Color.FromArgb(248, 222, 234)))

        Dim content As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .Padding = New Padding(24),
            .BackColor = Color.White
        }
        content.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 54))
        content.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 46))

        Dim pie As New PlannerPieChartPanel(_tasks) With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White
        }

        Dim legend As New DataGridView With {
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoGenerateColumns = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ColumnHeadersHeight = 34,
            .Dock = DockStyle.Fill,
            .EnableHeadersVisualStyles = False,
            .GridColor = Color.FromArgb(232, 236, 242),
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        }
        legend.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 46, 66)
        legend.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        legend.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        legend.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 235, 255)
        legend.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 31, 42)
        legend.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(PlannerPreviewRow.ColorName), .HeaderText = "", .Width = 42})
        legend.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(PlannerPreviewRow.TaskName), .HeaderText = "Task", .Width = 210})
        legend.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(PlannerPreviewRow.DurationText), .HeaderText = "Duration", .Width = 92})
        legend.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(PlannerPreviewRow.DateRange), .HeaderText = "Dates", .Width = 150})
        AddHandler legend.CellFormatting, AddressOf LegendCellFormatting
        legend.DataSource = _tasks.Select(Function(task, index) PlannerPreviewRow.FromTask(task, index)).ToList()

        content.Controls.Add(pie, 0, 0)
        content.Controls.Add(legend, 1, 0)

        Controls.Add(content)
        Controls.Add(header)
    End Sub

    Private Function SummaryTile(labelText As String, valueText As String, location As Point, backColor As Color) As Panel
        Dim panel As New Panel With {
            .BackColor = backColor,
            .Location = location,
            .Size = New Size(170, 72)
        }

        panel.Controls.Add(New Label With {
            .Text = labelText,
            .AutoSize = False,
            .Location = New Point(12, 8),
            .Size = New Size(146, 22),
            .ForeColor = Color.FromArgb(75, 85, 99)
        })
        panel.Controls.Add(New Label With {
            .Text = valueText,
            .AutoSize = False,
            .Location = New Point(12, 32),
            .Size = New Size(146, 30),
            .Font = New Font("Segoe UI Semibold", 11.0F),
            .ForeColor = Color.FromArgb(24, 31, 42)
        })

        Return panel
    End Function

    Private Function ProjectDurationText() As String
        If _tasks.Count = 0 Then
            Return "0 days"
        End If

        Dim startDate = _tasks.Min(Function(task) task.StartDate)
        Dim finishDate = _tasks.Max(Function(task) task.FinishDate)
        Dim days = Math.Max(1, (finishDate.Date - startDate.Date).Days + 1)
        Return days.ToString(CultureInfo.InvariantCulture) & " days"
    End Function

    Private Function TotalTaskDuration() As Decimal
        Return _tasks.Sum(Function(task) Math.Max(0.01D, task.DurationDays))
    End Function

    Private Sub LegendCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> 0 Then
            Return
        End If

        Dim color = PlannerPieChartPanel.SliceColor(e.RowIndex)
        e.CellStyle.BackColor = color
        e.CellStyle.ForeColor = color
        e.Value = ""
        e.FormattingApplied = True
    End Sub
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
