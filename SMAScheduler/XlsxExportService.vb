Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Security
Imports System.Text
Imports System.Xml.Linq

Public Class XlsxExportService
    Public Sub ExportProjectPlan(filePath As String, projectName As String, versionNumber As String, tasks As IEnumerable(Of ScheduleTask))
        Dim rows As New List(Of List(Of Object)) From {
            New List(Of Object) From {"Project Name", projectName},
            New List(Of Object) From {"Version", versionNumber},
            New List(Of Object)(),
            New List(Of Object) From {"Task ID", "Database Task ID", "Task Name", "Assigned To", "Resource Allocations", "Resource Hours", "Assignment Date", "Start Date", "Finish Date", "Duration Days", "Percent Complete", "Predecessors", "Link Type", "Module ID"}
        }

        For Each task In tasks
            rows.Add(New List(Of Object) From {
                     task.TaskId,
                     task.DatabaseTaskId,
                     task.TaskName,
                     task.AssignedTo,
                     task.ResourceAllocations,
                     task.ResourceHours,
                     task.AssignmentDate,
                     task.StartDate,
                     task.FinishDate,
                     task.DurationDays,
                     task.PercentComplete,
                     task.Predecessors,
                     task.DependencyType,
                     task.ModuleId})
        Next

        CreateWorkbook(filePath, New Dictionary(Of String, List(Of List(Of Object))) From {{"Project Plan", rows}})
    End Sub

    Public Sub ExportResourceMonth(filePath As String, projectName As String, versionNumber As String, employeeNames As IEnumerable(Of String), tasks As IEnumerable(Of ScheduleTask), monthDate As Date, includeSaturdays As Boolean)
        Dim monthStart = New Date(monthDate.Year, monthDate.Month, 1)
        Dim daysInMonth = Date.DaysInMonth(monthStart.Year, monthStart.Month)
        Dim projectLabel = ProjectVersionLabel(projectName, versionNumber)
        Dim allocations = ReadCapacityPlanningRows(filePath)

        Dim rows As New List(Of List(Of Object)) From {
            New List(Of Object) From {"Capacity Planning"},
            New List(Of Object) From {"Month", monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture)},
            New List(Of Object) From {"Rule", "Max 8 hours per employee per day. Weekends are holidays unless Weekend Plan is enabled."},
            New List(Of Object)()
        }

        Dim header As New List(Of Object) From {"Resource / Project"}
        For day = 1 To daysInMonth
            header.Add(New Date(monthStart.Year, monthStart.Month, day))
        Next
        rows.Add(header)

        For Each employeeName In employeeNames.OrderBy(Function(name) name)
            If Not allocations.ContainsKey(employeeName) Then
                allocations(employeeName) = New Dictionary(Of String, List(Of Object))(StringComparer.OrdinalIgnoreCase)
            End If

            Dim projectRow As New List(Of Object) From {projectLabel}
            For day = 1 To daysInMonth
                Dim currentDate = New Date(monthStart.Year, monthStart.Month, day)
                If IsBlockedDate(currentDate, includeSaturdays) Then
                    projectRow.Add("HOLIDAY")
                Else
                    projectRow.Add(AllocatedHours(employeeName, currentDate, tasks, includeSaturdays))
                End If
            Next
            If projectRow.Skip(1).Any(Function(value) TypeOf value Is Decimal AndAlso CDec(value) > 0D) Then
                allocations(employeeName)(projectLabel) = projectRow
            End If

            rows.Add(ResourceHeaderRow(employeeName, daysInMonth))
            For Each savedProject In allocations(employeeName).OrderBy(Function(item) item.Key)
                rows.Add(NormalizeCapacityRow(savedProject.Value, daysInMonth))
            Next
        Next

        CreateWorkbook(filePath, New Dictionary(Of String, List(Of List(Of Object))) From {{"Capacity Planning", rows}})
    End Sub

    Private Function AllocatedHours(employeeName As String, currentDate As Date, tasks As IEnumerable(Of ScheduleTask), includeSaturdays As Boolean) As Decimal
        Return tasks.
            Where(Function(task) TaskHasEmployee(task, employeeName)).
            Sum(Function(task) AllocatedHoursForDate(task, employeeName, currentDate.Date, includeSaturdays))
    End Function

    Public Function ProjectVersionLabel(projectName As String, versionNumber As String) As String
        Dim safeProjectName = If(String.IsNullOrWhiteSpace(projectName), "Project", projectName.Trim())
        Dim safeVersion = If(String.IsNullOrWhiteSpace(versionNumber), "1.0", versionNumber.Trim())
        Return safeProjectName & "-V" & safeVersion
    End Function

    Public Function AllocatedHoursFromCapacityPlanning(filePath As String, employeeName As String, workDate As Date, Optional excludedProjectLabel As String = "") As Decimal
        If String.IsNullOrWhiteSpace(employeeName) OrElse String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
            Return 0D
        End If

        Try
            Using archive = ZipFile.OpenRead(filePath)
                Dim sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                If sheetEntry Is Nothing Then
                    Return 0D
                End If

                Dim rows = ReadWorksheetRows(sheetEntry)
                Dim headerIndex = rows.FindIndex(Function(row) row.Count > 0 AndAlso String.Equals(Convert.ToString(row(0), CultureInfo.InvariantCulture), "Resource / Project", StringComparison.OrdinalIgnoreCase))
                If headerIndex < 0 Then
                    Return 0D
                End If

                Dim dateColumn = CapacityPlanningDateColumn(rows(headerIndex), workDate.Date)
                If dateColumn < 0 Then
                    Return 0D
                End If

                Dim totalHours = 0D
                Dim currentEmployee = ""
                For i = headerIndex + 1 To rows.Count - 1
                    Dim row = rows(i)
                    If row.Count = 0 Then
                        Continue For
                    End If

                    Dim firstCell = Convert.ToString(row(0), CultureInfo.InvariantCulture).Trim()
                    If String.IsNullOrWhiteSpace(firstCell) Then
                        Continue For
                    End If

                    If IsCapacityResourceHeaderRow(row) Then
                        currentEmployee = firstCell
                    ElseIf String.Equals(currentEmployee, employeeName, StringComparison.OrdinalIgnoreCase) AndAlso
                        Not String.Equals(firstCell, excludedProjectLabel, StringComparison.OrdinalIgnoreCase) Then
                        If dateColumn < row.Count Then
                            totalHours += DecimalCellValue(row(dateColumn))
                        End If
                    End If
                Next

                Return Math.Max(0D, totalHours)
            End Using
        Catch ex As IOException
            Return 0D
        Catch ex As InvalidDataException
            Return 0D
        End Try
    End Function

    Private Function ResourceHeaderRow(employeeName As String, daysInMonth As Integer) As List(Of Object)
        Dim row As New List(Of Object) From {employeeName}
        For i = 1 To daysInMonth
            row.Add("")
        Next
        Return row
    End Function

    Private Function NormalizeCapacityRow(row As List(Of Object), daysInMonth As Integer) As List(Of Object)
        Dim normalized = If(row Is Nothing OrElse row.Count = 0, New List(Of Object) From {""}, New List(Of Object)(row))
        While normalized.Count < daysInMonth + 1
            normalized.Add("")
        End While
        If normalized.Count > daysInMonth + 1 Then
            normalized = normalized.Take(daysInMonth + 1).ToList()
        End If
        Return normalized
    End Function

    Private Function CapacityPlanningDateColumn(headerRow As List(Of Object), workDate As Date) As Integer
        For i = 1 To headerRow.Count - 1
            Dim headerDate As Date
            If TryCapacityDate(headerRow(i), headerDate) AndAlso headerDate.Date = workDate.Date Then
                Return i
            End If
        Next

        Return -1
    End Function

    Private Function TryCapacityDate(value As Object, ByRef parsedDate As Date) As Boolean
        If TypeOf value Is Date Then
            parsedDate = CDate(value).Date
            Return True
        End If

        If TypeOf value Is Decimal OrElse TypeOf value Is Double OrElse TypeOf value Is Integer Then
            parsedDate = Date.FromOADate(Convert.ToDouble(value, CultureInfo.InvariantCulture)).Date
            Return True
        End If

        Return Date.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), parsedDate)
    End Function

    Private Function IsCapacityResourceHeaderRow(row As List(Of Object)) As Boolean
        Return row.Skip(1).All(Function(value) String.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture)))
    End Function

    Private Function DecimalCellValue(value As Object) As Decimal
        If TypeOf value Is Decimal Then
            Return CDec(value)
        End If
        If TypeOf value Is Integer OrElse TypeOf value Is Double Then
            Return Convert.ToDecimal(value, CultureInfo.InvariantCulture)
        End If

        Dim parsed As Decimal
        If Decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, parsed) Then
            Return parsed
        End If

        Return 0D
    End Function

    Private Function ReadCapacityPlanningRows(filePath As String) As Dictionary(Of String, Dictionary(Of String, List(Of Object)))
        Dim result As New Dictionary(Of String, Dictionary(Of String, List(Of Object)))(StringComparer.OrdinalIgnoreCase)
        If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
            Return result
        End If

        Try
            Using archive = ZipFile.OpenRead(filePath)
                Dim sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                If sheetEntry Is Nothing Then
                    Return result
                End If

                Dim rows = ReadWorksheetRows(sheetEntry)
                Dim headerIndex = rows.FindIndex(Function(row) row.Count > 0 AndAlso String.Equals(Convert.ToString(row(0), CultureInfo.InvariantCulture), "Resource / Project", StringComparison.OrdinalIgnoreCase))
                If headerIndex < 0 Then
                    Return result
                End If

                Dim currentEmployee = ""
                For i = headerIndex + 1 To rows.Count - 1
                    Dim row = rows(i)
                    If row.Count = 0 Then
                        Continue For
                    End If

                    Dim firstCell = Convert.ToString(row(0), CultureInfo.InvariantCulture)
                    If String.IsNullOrWhiteSpace(firstCell) Then
                        Continue For
                    End If

                    If IsCapacityResourceHeaderRow(row) Then
                        currentEmployee = firstCell.Trim()
                        If Not result.ContainsKey(currentEmployee) Then
                            result(currentEmployee) = New Dictionary(Of String, List(Of Object))(StringComparer.OrdinalIgnoreCase)
                        End If
                    ElseIf Not String.IsNullOrWhiteSpace(currentEmployee) Then
                        result(currentEmployee)(firstCell.Trim()) = row
                    End If
                Next
            End Using
        Catch ex As IOException
            Return result
        Catch ex As InvalidDataException
            Return result
        End Try

        Return result
    End Function

    Private Function ReadWorksheetRows(sheetEntry As ZipArchiveEntry) As List(Of List(Of Object))
        Dim rows As New List(Of List(Of Object))()
        Dim spreadsheetNamespace = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main")
        Using stream = sheetEntry.Open()
            Dim document = XDocument.Load(stream)
            For Each rowElement In document.Descendants(spreadsheetNamespace + "row")
                Dim row As New List(Of Object)()
                For Each cellElement In rowElement.Elements(spreadsheetNamespace + "c")
                    row.Add(ReadCellValue(cellElement, spreadsheetNamespace))
                Next
                rows.Add(row)
            Next
        End Using
        Return rows
    End Function

    Private Function ReadCellValue(cellElement As XElement, spreadsheetNamespace As XNamespace) As Object
        Dim typeAttribute = cellElement.Attribute("t")
        Dim cellType = If(typeAttribute Is Nothing, "", typeAttribute.Value)
        If String.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase) Then
            Dim textElement = cellElement.Descendants(spreadsheetNamespace + "t").FirstOrDefault()
            Return If(textElement Is Nothing, "", textElement.Value)
        End If

        Dim valueElement = cellElement.Element(spreadsheetNamespace + "v")
        Dim rawValue = If(valueElement Is Nothing, "", valueElement.Value)
        Dim decimalValue As Decimal
        If Decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, decimalValue) Then
            Return decimalValue
        End If

        Return If(rawValue, "")
    End Function

    Public Function RemainingHours(employeeName As String, currentDate As Date, tasks As IEnumerable(Of ScheduleTask), includeSaturdays As Boolean, Optional ignoredTaskId As Integer = 0) As Decimal
        If String.IsNullOrWhiteSpace(employeeName) OrElse IsBlockedDate(currentDate, includeSaturdays) Then
            Return 0D
        End If

        Dim usedHours = tasks.
            Where(Function(task) task.TaskId <> ignoredTaskId AndAlso TaskHasEmployee(task, employeeName)).
            Sum(Function(task) AllocatedHoursForDate(task, employeeName, currentDate.Date, includeSaturdays))

        Return Math.Max(0D, 8D - usedHours)
    End Function

    Private Function AllocatedHoursForDate(task As ScheduleTask, employeeName As String, currentDate As Date, includeSaturdays As Boolean) As Decimal
        If task Is Nothing OrElse task.ResourceHours <= 0D OrElse currentDate < task.StartDate.Date OrElse currentDate > task.FinishDate.Date OrElse IsBlockedDate(currentDate, includeSaturdays) Then
            Return 0D
        End If

        Dim dailyAssignedResources = DailyAssignedResourceHours(task)
        If dailyAssignedResources.Count > 0 Then
            Dim dailyKey = DailyAllocationKey(employeeName, currentDate.Date)
            If dailyAssignedResources.ContainsKey(dailyKey) Then
                Return Math.Max(0D, dailyAssignedResources(dailyKey))
            End If
            Return 0D
        End If

        Dim assignedResources = AssignedResourceHours(task)
        If assignedResources.Count = 0 OrElse Not assignedResources.ContainsKey(employeeName) Then
            Return 0D
        End If

        Dim employeeTaskHours = assignedResources(employeeName)
        Dim previousWorkingDays = 0
        Dim cursor = task.StartDate.Date
        While cursor < currentDate
            If Not IsBlockedDate(cursor, includeSaturdays) Then
                previousWorkingDays += 1
            End If
            cursor = cursor.AddDays(1)
        End While

        Dim remainingForDate = employeeTaskHours - previousWorkingDays * 8D
        Return ClampDecimal(remainingForDate, 0D, 8D)
    End Function

    Private Function TaskHasEmployee(task As ScheduleTask, employeeName As String) As Boolean
        Return AssignedResourceHours(task).ContainsKey(employeeName)
    End Function

    Private Function AssignedResourceHours(task As ScheduleTask) As Dictionary(Of String, Decimal)
        Dim result As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        If task Is Nothing Then
            Return result
        End If

        If Not String.IsNullOrWhiteSpace(task.ResourceAllocations) Then
            For Each part In task.ResourceAllocations.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
                Dim pieces = part.Split({"="c}, 2, StringSplitOptions.None)
                If pieces.Length = 2 Then
                    Dim hours As Decimal
                    If Decimal.TryParse(pieces(1).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, hours) OrElse
                        Decimal.TryParse(pieces(1).Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, hours) Then
                        Dim employeeName = pieces(0).Trim()
                        If employeeName.Length > 0 Then
                            result(employeeName) = Math.Max(0D, hours)
                        End If
                    End If
                End If
            Next
        End If

        If result.Count > 0 Then
            Return result
        End If

        Dim names = AssignedResourceNames(task)
        If names.Count = 0 Then
            Return result
        End If

        Dim hoursPerResource = task.ResourceHours / names.Count
        For Each name In names
            result(name) = hoursPerResource
        Next

        Return result
    End Function

    Private Function DailyAssignedResourceHours(task As ScheduleTask) As Dictionary(Of String, Decimal)
        Dim result As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        If task Is Nothing OrElse String.IsNullOrWhiteSpace(task.DailyResourceAllocations) Then
            Return result
        End If

        For Each part In task.DailyResourceAllocations.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
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

    Private Function DailyAllocationKey(employeeName As String, workDate As Date) As String
        Return employeeName.Trim() & "|" & workDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    End Function

    Private Function AssignedResourceNames(task As ScheduleTask) As List(Of String)
        If task Is Nothing Then
            Return New List(Of String)()
        End If

        Dim resourceValue = If(String.IsNullOrWhiteSpace(task.ResourceNames), task.AssignedTo, task.ResourceNames)
        If String.IsNullOrWhiteSpace(resourceValue) Then
            Return New List(Of String)()
        End If

        Return resourceValue.Split({","c, ";"c}, StringSplitOptions.RemoveEmptyEntries).
            Select(Function(name) name.Trim()).
            Where(Function(name) name.Length > 0).
            Distinct(StringComparer.OrdinalIgnoreCase).
            ToList()
    End Function

    Private Function ClampDecimal(value As Decimal, minimum As Decimal, maximum As Decimal) As Decimal
        Return Math.Min(maximum, Math.Max(minimum, value))
    End Function

    Private Function IsBlockedDate(currentDate As Date, includeSaturdays As Boolean) As Boolean
        Return Not includeSaturdays AndAlso
            (currentDate.DayOfWeek = DayOfWeek.Saturday OrElse currentDate.DayOfWeek = DayOfWeek.Sunday)
    End Function

    Private Sub CreateWorkbook(filePath As String, sheets As Dictionary(Of String, List(Of List(Of Object))))
        If File.Exists(filePath) Then
            File.Delete(filePath)
        End If

        Using archive = ZipFile.Open(filePath, ZipArchiveMode.Create)
            AddText(archive, "[Content_Types].xml", ContentTypesXml(sheets.Count))
            AddText(archive, "_rels/.rels", RootRelsXml())
            AddText(archive, "xl/workbook.xml", WorkbookXml(sheets.Keys.ToList()))
            AddText(archive, "xl/_rels/workbook.xml.rels", WorkbookRelsXml(sheets.Count))
            AddText(archive, "xl/styles.xml", StylesXml())

            Dim index = 1
            For Each sheet In sheets
                AddText(archive, $"xl/worksheets/sheet{index}.xml", WorksheetXml(sheet.Value))
                index += 1
            Next
        End Using
    End Sub

    Private Sub AddText(archive As ZipArchive, path As String, content As String)
        Dim entry = archive.CreateEntry(path, CompressionLevel.Optimal)
        Using stream = entry.Open()
            Using writer As New StreamWriter(stream, New UTF8Encoding(False))
                writer.Write(content)
            End Using
        End Using
    End Sub

    Private Function ContentTypesXml(sheetCount As Integer) As String
        Dim builder As New StringBuilder()
        builder.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        builder.Append("<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">")
        builder.Append("<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>")
        builder.Append("<Default Extension=""xml"" ContentType=""application/xml""/>")
        builder.Append("<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>")
        builder.Append("<Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>")
        For i = 1 To sheetCount
            builder.Append($"<Override PartName=""/xl/worksheets/sheet{i}.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>")
        Next
        builder.Append("</Types>")
        Return builder.ToString()
    End Function

    Private Function RootRelsXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/></Relationships>"
    End Function

    Private Function WorkbookXml(sheetNames As List(Of String)) As String
        Dim builder As New StringBuilder()
        builder.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        builder.Append("<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheets>")
        For i = 0 To sheetNames.Count - 1
            builder.Append($"<sheet name=""{EscapeXml(SanitizeSheetName(sheetNames(i)))}"" sheetId=""{i + 1}"" r:id=""rId{i + 1}""/>")
        Next
        builder.Append("</sheets></workbook>")
        Return builder.ToString()
    End Function

    Private Function WorkbookRelsXml(sheetCount As Integer) As String
        Dim builder As New StringBuilder()
        builder.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">")
        For i = 1 To sheetCount
            builder.Append($"<Relationship Id=""rId{i}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet{i}.xml""/>")
        Next
        builder.Append($"<Relationship Id=""rId{sheetCount + 1}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>")
        builder.Append("</Relationships>")
        Return builder.ToString()
    End Function

    Private Function StylesXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""><numFmts count=""1""><numFmt numFmtId=""164"" formatCode=""dd-mmm-yyyy""/></numFmts><fonts count=""3""><font><sz val=""11""/><name val=""Calibri""/></font><font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font><font><sz val=""11""/><color rgb=""FF9C0006""/><name val=""Calibri""/></font></fonts><fills count=""4""><fill><patternFill patternType=""none""/></fill><fill><patternFill patternType=""gray125""/></fill><fill><patternFill patternType=""solid""><fgColor rgb=""FF24344D""/><bgColor indexed=""64""/></patternFill></fill><fill><patternFill patternType=""solid""><fgColor rgb=""FFFFC7CE""/><bgColor indexed=""64""/></patternFill></fill></fills><borders count=""1""><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs><cellXfs count=""4""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/><xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyNumberFormat=""1""/><xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/><xf numFmtId=""0"" fontId=""2"" fillId=""3"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/></cellXfs></styleSheet>"
    End Function

    Private Function WorksheetXml(rows As List(Of List(Of Object))) As String
        Dim builder As New StringBuilder()
        builder.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        builder.Append("<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">")
        builder.Append(ColumnWidthsXml(rows))
        builder.Append("<sheetData>")
        Dim isCapacitySheet = rows.Any(Function(row) row.Count > 0 AndAlso String.Equals(CStr(row(0)), "Resource / Project", StringComparison.OrdinalIgnoreCase))
        For rowIndex = 0 To rows.Count - 1
            Dim isHeaderRow = rows(rowIndex).Count > 0 AndAlso (String.Equals(CStr(rows(rowIndex)(0)), "Task ID", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(CStr(rows(rowIndex)(0)), "Employee", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(CStr(rows(rowIndex)(0)), "Resource / Project", StringComparison.OrdinalIgnoreCase))
            builder.Append($"<row r=""{rowIndex + 1}"">")
            For colIndex = 0 To rows(rowIndex).Count - 1
                Dim cellRef = ColumnName(colIndex + 1) & (rowIndex + 1).ToString(CultureInfo.InvariantCulture)
                Dim styleIndex = If(isHeaderRow, 2, 0)
                If isCapacitySheet AndAlso Not isHeaderRow AndAlso colIndex > 0 AndAlso DecimalCellValue(rows(rowIndex)(colIndex)) > 8D Then
                    styleIndex = 3
                End If
                builder.Append(CellXml(cellRef, rows(rowIndex)(colIndex), styleIndex))
            Next
            builder.Append("</row>")
        Next
        builder.Append("</sheetData></worksheet>")
        Return builder.ToString()
    End Function

    Private Function ColumnWidthsXml(rows As List(Of List(Of Object))) As String
        Dim maxColumns = If(rows.Count = 0, 1, rows.Max(Function(row) row.Count))
        Dim builder As New StringBuilder("<cols>")
        For columnIndex = 1 To maxColumns
            Dim width As Decimal = 15D
            If columnIndex = 1 Then
                width = 28D
            ElseIf columnIndex = 3 Then
                width = 42D
            ElseIf columnIndex = 4 Then
                width = 24D
            ElseIf columnIndex >= 6 AndAlso columnIndex <= 8 Then
                width = 16D
            End If

            builder.Append($"<col min=""{columnIndex}"" max=""{columnIndex}"" width=""{width.ToString(CultureInfo.InvariantCulture)}"" customWidth=""1""/>")
        Next
        builder.Append("</cols>")
        Return builder.ToString()
    End Function

    Private Function CellXml(cellRef As String, value As Object, styleIndex As Integer) As String
        Dim style = If(styleIndex > 0, $" s=""{styleIndex}""", "")
        If value Is Nothing Then
            Return $"<c r=""{cellRef}""{style}/>"
        End If

        If TypeOf value Is Date Then
            Dim serial = CType(value, Date).ToOADate().ToString(CultureInfo.InvariantCulture)
            Return $"<c r=""{cellRef}"" s=""1""><v>{serial}</v></c>"
        End If

        If TypeOf value Is Integer OrElse TypeOf value Is Decimal OrElse TypeOf value Is Double Then
            Return $"<c r=""{cellRef}""{style}><v>{Convert.ToString(value, CultureInfo.InvariantCulture)}</v></c>"
        End If

        Return $"<c r=""{cellRef}"" t=""inlineStr""{style}><is><t>{EscapeXml(CStr(value))}</t></is></c>"
    End Function

    Private Function ColumnName(columnNumber As Integer) As String
        Dim dividend = columnNumber
        Dim columnNameValue As String = ""
        While dividend > 0
            Dim moduloValue = (dividend - 1) Mod 26
            columnNameValue = ChrW(65 + moduloValue) & columnNameValue
            dividend = (dividend - moduloValue) \ 26
        End While
        Return columnNameValue
    End Function

    Private Function EscapeXml(value As String) As String
        Return SecurityElement.Escape(If(value, ""))
    End Function

    Private Function SanitizeSheetName(value As String) As String
        Dim invalidChars = New Char() {"["c, "]"c, ":"c, "*"c, "?"c, "/"c, "\"c}
        Dim safe = invalidChars.Aggregate(value, Function(current, ch) current.Replace(ch, "_"c))
        If safe.Length > 31 Then
            safe = safe.Substring(0, 31)
        End If
        Return If(String.IsNullOrWhiteSpace(safe), "Sheet1", safe)
    End Function
End Class
