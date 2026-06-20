Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Text.Json
Imports System.Xml.Linq

Public Class ProjectLibraryService
    Public ReadOnly Property ProjectFolder As String
        Get
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMA Scheduler", "Projects")
        End Get
    End Property

    Public Function ListProjects() As List(Of ProjectLibraryItem)
        Directory.CreateDirectory(ProjectFolder)
        Dim projects As New Dictionary(Of String, ProjectLibraryItem)(StringComparer.OrdinalIgnoreCase)

        For Each filePath In CandidateProjectFiles()
            Try
                Dim snapshot = LoadSnapshot(filePath)
                If snapshot Is Nothing Then
                    Continue For
                End If

                Dim item = ProjectLibraryItem.FromSnapshot(filePath, snapshot)
                Dim key = ProjectListKey(item)
                If Not projects.ContainsKey(key) OrElse IsPreferredProjectListItem(item, projects(key)) Then
                    projects(key) = item
                End If
            Catch ex As IOException
            Catch ex As JsonException
            End Try
        Next

        Return projects.Values.OrderByDescending(Function(project) project.UpdatedOn).ToList()
    End Function

    Private Function ProjectListKey(item As ProjectLibraryItem) As String
        Return If(item.ProjectName, "").Trim().ToLowerInvariant() & "|v" & If(item.VersionNumber, "").Trim().ToLowerInvariant()
    End Function

    Private Function IsPreferredProjectListItem(candidate As ProjectLibraryItem, current As ProjectLibraryItem) As Boolean
        Dim candidatePriority = ProjectFilePriority(candidate.FilePath)
        Dim currentPriority = ProjectFilePriority(current.FilePath)

        If candidatePriority <> currentPriority Then
            Return candidatePriority < currentPriority
        End If

        Return candidate.UpdatedOn > current.UpdatedOn
    End Function

    Private Function ProjectFilePriority(filePath As String) As Integer
        Dim extension = Path.GetExtension(filePath)
        If String.Equals(extension, ".smaschedule", StringComparison.OrdinalIgnoreCase) Then
            Return 0
        End If
        If String.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) Then
            Return 1
        End If
        If String.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) Then
            Return 2
        End If
        Return 3
    End Function

    Public Function LoadSnapshot(filePath As String) As ProjectSnapshot
        If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
            Return Nothing
        End If

        Dim extension = Path.GetExtension(filePath)
        If String.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) Then
            Return LoadProjectPlanWorkbook(filePath)
        End If

        Return JsonSerializer.Deserialize(Of ProjectSnapshot)(File.ReadAllText(filePath))
    End Function

    Public Function SaveSnapshot(snapshot As ProjectSnapshot) As String
        Directory.CreateDirectory(ProjectFolder)
        snapshot.UpdatedOn = Date.Now
        Dim filePath = Path.Combine(ProjectFolder, MakeSafeFileName(snapshot.ProjectName & "_V" & snapshot.VersionNumber) & ".smaschedule")
        Dim options As New JsonSerializerOptions With {.WriteIndented = True}
        File.WriteAllText(filePath, JsonSerializer.Serialize(snapshot, options))
        Return filePath
    End Function

    Private Function CandidateProjectFiles() As IEnumerable(Of String)
        Dim roots = New List(Of String) From {
            ProjectFolder,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMA Scheduler"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        }

        Dim files As New List(Of String)()
        For Each root In roots.Where(Function(path) Not String.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase)
            If Not Directory.Exists(root) Then
                Continue For
            End If

            Dim optionValue = If(root.EndsWith(Path.Combine("SMA Scheduler", "Projects"), StringComparison.OrdinalIgnoreCase) OrElse root.EndsWith("SMA Scheduler", StringComparison.OrdinalIgnoreCase),
                                 SearchOption.AllDirectories,
                                 SearchOption.TopDirectoryOnly)

            files.AddRange(SafeEnumerateFiles(root, "*.smaschedule", optionValue))
            files.AddRange(SafeEnumerateFiles(root, "*.json", optionValue))
            files.AddRange(SafeEnumerateFiles(root, "*.xlsx", optionValue).
                Where(Function(fileName) Not Path.GetFileName(fileName).StartsWith("Capacity Planning_", StringComparison.OrdinalIgnoreCase)))
        Next

        Return files.Distinct(StringComparer.OrdinalIgnoreCase)
    End Function

    Private Function SafeEnumerateFiles(folder As String, pattern As String, optionValue As SearchOption) As IEnumerable(Of String)
        Try
            Return Directory.EnumerateFiles(folder, pattern, optionValue).ToList()
        Catch ex As IOException
            Return Enumerable.Empty(Of String)()
        Catch ex As UnauthorizedAccessException
            Return Enumerable.Empty(Of String)()
        End Try
    End Function

    Private Function LoadProjectPlanWorkbook(filePath As String) As ProjectSnapshot
        Try
            Using archive = ZipFile.OpenRead(filePath)
                Dim sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                If sheetEntry Is Nothing Then
                    Return Nothing
                End If

                Dim rows = ReadWorksheetRows(sheetEntry)
                If rows.Count < 4 OrElse rows(0).Count < 2 OrElse Not String.Equals(Convert.ToString(rows(0)(0), CultureInfo.InvariantCulture), "Project Name", StringComparison.OrdinalIgnoreCase) Then
                    Return Nothing
                End If

                Dim snapshot As New ProjectSnapshot With {
                    .ProjectName = Convert.ToString(rows(0)(1), CultureInfo.InvariantCulture),
                    .VersionNumber = If(rows(1).Count > 1, Convert.ToString(rows(1)(1), CultureInfo.InvariantCulture), "1.0"),
                    .ProjectSize = "Small",
                    .UpdatedOn = File.GetLastWriteTime(filePath)
                }

                For rowIndex = 4 To rows.Count - 1
                    Dim row = rows(rowIndex)
                    If row.Count < 13 Then
                        Continue For
                    End If

                    snapshot.Tasks.Add(New ScheduleTask With {
                        .TaskId = CInt(DecimalCellValue(row(0))),
                        .DatabaseTaskId = CInt(DecimalCellValue(row(1))),
                        .TaskName = Convert.ToString(row(2), CultureInfo.InvariantCulture),
                        .AssignedTo = Convert.ToString(row(3), CultureInfo.InvariantCulture),
                        .ResourceNames = Convert.ToString(row(3), CultureInfo.InvariantCulture),
                        .ResourceAllocations = Convert.ToString(row(4), CultureInfo.InvariantCulture),
                        .ResourceHours = DecimalCellValue(row(5)),
                        .AssignmentDate = DateCellValue(row(6)),
                        .StartDate = DateCellValue(row(7)),
                        .FinishDate = DateCellValue(row(8)),
                        .DurationDays = DecimalCellValue(row(9)),
                        .PercentComplete = CInt(DecimalCellValue(row(10))),
                        .Predecessors = Convert.ToString(row(11), CultureInfo.InvariantCulture),
                        .ModuleId = CInt(DecimalCellValue(row(12)))
                    })
                Next

                Return snapshot
            End Using
        Catch ex As IOException
            Return Nothing
        Catch ex As InvalidDataException
            Return Nothing
        Catch ex As Exception
            Return Nothing
        End Try
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
        If typeAttribute IsNot Nothing AndAlso String.Equals(typeAttribute.Value, "inlineStr", StringComparison.OrdinalIgnoreCase) Then
            Dim textElement = cellElement.Descendants(spreadsheetNamespace + "t").FirstOrDefault()
            Return If(textElement Is Nothing, "", textElement.Value)
        End If

        Dim valueElement = cellElement.Element(spreadsheetNamespace + "v")
        Dim rawValue = If(valueElement Is Nothing, "", valueElement.Value)
        Dim decimalValue As Decimal
        If Decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, decimalValue) Then
            Return decimalValue
        End If

        Return rawValue
    End Function

    Private Function DecimalCellValue(value As Object) As Decimal
        If TypeOf value Is Decimal Then
            Return CDec(value)
        End If

        Dim parsed As Decimal
        If Decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, parsed) Then
            Return parsed
        End If

        Return 0D
    End Function

    Private Function DateCellValue(value As Object) As Date
        If TypeOf value Is Decimal Then
            Return Date.FromOADate(CDbl(value))
        End If

        Dim parsed As Date
        If Date.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), parsed) Then
            Return parsed.Date
        End If

        Return Date.Today
    End Function

    Private Function MakeSafeFileName(value As String) As String
        Dim safeValue = If(String.IsNullOrWhiteSpace(value), "SMA Scheduler", value)
        For Each character In Path.GetInvalidFileNameChars()
            safeValue = safeValue.Replace(character, "_"c)
        Next
        Return safeValue
    End Function
End Class

Public Class ProjectLibraryItem
    Public Property ProjectName As String = ""
    Public Property VersionNumber As String = ""
    Public Property ProjectSize As String = ""
    Public Property TaskCount As Integer
    Public Property ResourceHours As Decimal
    Public Property StartDateText As String = ""
    Public Property FinishDateText As String = ""
    Public Property UpdatedOn As Date
    Public Property FilePath As String = ""

    Public Shared Function FromSnapshot(filePath As String, snapshot As ProjectSnapshot) As ProjectLibraryItem
        Dim item As New ProjectLibraryItem With {
            .ProjectName = snapshot.ProjectName,
            .VersionNumber = snapshot.VersionNumber,
            .ProjectSize = snapshot.ProjectSize,
            .TaskCount = If(snapshot.Tasks Is Nothing, 0, snapshot.Tasks.Count),
            .ResourceHours = If(snapshot.Tasks Is Nothing, 0D, snapshot.Tasks.Sum(Function(task) task.ResourceHours)),
            .UpdatedOn = If(snapshot.UpdatedOn = Date.MinValue, File.GetLastWriteTime(filePath), snapshot.UpdatedOn),
            .FilePath = filePath
        }

        If snapshot.Tasks IsNot Nothing AndAlso snapshot.Tasks.Count > 0 Then
            item.StartDateText = snapshot.Tasks.Min(Function(task) task.StartDate).ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
            item.FinishDateText = snapshot.Tasks.Max(Function(task) task.FinishDate).ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
        End If

        Return item
    End Function
End Class
