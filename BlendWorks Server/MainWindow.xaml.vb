Imports Microsoft.Win32
Imports System.Net
Imports System.Windows.Threading

Public Class MainWindow
    Public WithEvents myServer As New BWServer(Me.Dispatcher)
    Public myWebServer As WebServerTwo
    Public WithEvents JobTimer As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(2)}

    Private _lastID As Integer = 0

    Public JobList As New Dictionary(Of Integer, Job)



#Region "Event Handler"
    Private Sub myServer_ActionPendingChanged(isPending As Boolean) Handles myServer.ActionPendingChanged
        MainGrid.IsEnabled = Not isPending
    End Sub
    Private Sub myServer_ServerStarted() Handles myServer.ServerStarted
        StartServerButton.IsEnabled = False
        StopServerButton.IsEnabled = True

        Dim startAnimBase As New AnimationBase(StartServerButton)
        Dim stopAnimBase As New AnimationBase(StopServerButton)

        startAnimBase.Fade(0, 1000, True)
        startAnimBase.TranslateTransformAnimation(0, 0, StartServerButton.Width, 0, 1000)

        stopAnimBase.Fade(1, 1000, True)
        stopAnimBase.TranslateTransformAnimation(-StopServerButton.Width, 0, 0, 0, 1000)

        JobTimer.Start()
    End Sub
    Private Sub myServer_ServerStopped() Handles myServer.ServerStopped
        StartServerButton.IsEnabled = True
        StopServerButton.IsEnabled = False

        Dim startAnimBase As New AnimationBase(StartServerButton)
        Dim stopAnimBase As New AnimationBase(StopServerButton)

        startAnimBase.Fade(1, 1000, True)
        startAnimBase.TranslateTransformAnimation(StartServerButton.Width, 0, 0, 0, 1000)

        stopAnimBase.Fade(0, 1000, True)
        stopAnimBase.TranslateTransformAnimation(0, 0, -StopServerButton.Width, 0, 1000)

        JobTimer.Stop()

        UpdateClientList()
    End Sub
    Private Sub myServer_ClientConnected(con As Connection) Handles myServer.ClientConnected
        UpdateClientList()
    End Sub
    Private Sub myServer_ClientDisconnected(nick As String) Handles myServer.ClientDisconnected
        UpdateClientList()
    End Sub
    Private Sub myServer_LogToConsole(s As String) Handles myServer.LogToConsole
        With LogTextBlock
            If Not .Text = "" Then .Text += vbNewLine
            .Text += s
        End With
        LogScrollViewer.ScrollToBottom()
    End Sub
    Private Sub myServer_MessageReceived(msg As String, func As String, from As String) Handles myServer.MessageReceived
        Select Case func

            Case "renderfinished"
                Task.Run( _
                    Sub()
                        Dim args As String() = msg.Split("%"c)
                        Dim fileUrl As String = "http://" + myServer.GetConnectionByNick(from).endPoint.Address.ToString + ":" + CLIENT_WEB_SERVER_PORT.ToString + "/dl/" + args(0)
                        Dim frame As Integer = Integer.Parse(args(2))
                        Dim jobID As Integer = Integer.Parse(args(3))

                        Try
                            EnsureOutputDirectory(args(1))
                            My.Computer.Network.DownloadFile(fileUrl, RENDER_OUTPUT_DIR + args(1) + "\" + args(1) + "_" + args(2) + ".png", "", "", False, 20000, True)
                            With JobList(jobID)
                                .framesProcessing.Remove(frame)
                                If Not .framesRendered.Contains(frame) Then .framesRendered.Add(frame)
                            End With
                            Log("Saved frame " + frame.ToString)
                            Dispatcher.Invoke(Sub() UpdateJobList())
                        Catch ex As Exception
                            Log("Error on frame " + frame.ToString)
                            If JobList.ContainsKey(jobID) Then
                                With JobList(jobID)
                                    .framesProcessing.Remove(frame)
                                End With
                            End If
                        End Try
                    End Sub)

            Case "status"
                Dim newStatus As BWServer.BWStatus = CType(Integer.Parse(msg), BWServer.BWStatus)
                myServer.GetConnectionByNick(from).status = newStatus
                UpdateClientList()

            Case "disconnect"
                UpdateClientList()

        End Select
    End Sub

    Private Sub JobTimer_Tick() Handles JobTimer.Tick
        For Each Job As Job In JobList.Values
            If Not Job.ProcessedAllFrames Then
                For Each client As Connection In myServer.Clients
                    If client.status = BWServer.BWStatus.Idle And Not client.paused Then
                        For Frame As Integer = Job.startFrame To Job.endFrame
                            If Not Job.framesProcessing.Contains(Frame) And _
                               Not Job.framesRendered.Contains(Frame) And _
                               Not Job.Paused Then

                                Job.framesProcessing.Add(Frame)
                                client.status = BWServer.BWStatus.Rendering
                                myServer.SendToClient(Job.fileMD5 + ".blend" + "%" + Frame.ToString + "%" + Job.ID.ToString, "renderfile", client)

                                Log(client.nick + " is rendering frame " + Frame.ToString + " on job " + Job.ID.ToString)

                                UpdateJobList()

                                Exit For
                            End If
                        Next

                    ElseIf client.status = BWServer.BWStatus.Idle And client.paused And client.doSafeDisconnect Then
                        myServer.SendToClient("You have been dosconnected", "disconnect", client)

                    End If
                Next
            End If
        Next

        For Each client As Connection In myServer.Clients
            If client.status = BWServer.BWStatus.Idle And client.paused And client.doSafeDisconnect Then
                myServer.SendToClient("You have been dosconnected", "disconnect", client)
            End If
        Next


        UpdateClientList()

        PublishHTML()
    End Sub

    Private Sub StartServerButton_Click(sender As Object, e As RoutedEventArgs) Handles StartServerButton.Click
        myServer.Start()
        myWebServer = New WebServerTwo(WEB_SERVER_PATH)
    End Sub
    Private Sub StopServerButton_Click(sender As Object, e As RoutedEventArgs) Handles StopServerButton.Click
        myServer.Stop()
        myWebServer.Dispose()
    End Sub
    Private Sub SendFileButton_Click(sender As Object, e As RoutedEventArgs) Handles SendFileButton.Click
        Dim ofd As New OpenFileDialog With {.Filter = "Blender Files|*.blend"}

        With ofd
            If .ShowDialog = True Then
                Dim fileMD5 As String = MD5FileHash(.FileName)
                Dim newFileName As String = fileMD5 + ".blend"

                EnsureDownloadDirectory()
                IO.File.Copy(.FileName, WEB_SERVER_PATH + "dl\" + newFileName, True)

                Dim newFileInfo(2) As String
                newFileInfo = BlenderFileInfo(WEB_SERVER_PATH + "dl\" + newFileName)

                Dim newJob As New Job
                With newJob
                    .fileMD5 = fileMD5
                    .startFrame = CInt(newFileInfo(0))
                    .endFrame = CInt(newFileInfo(1))
                    .framesRendered = New List(Of Integer)
                    .framesProcessing = New List(Of Integer)
                    .ID = GetNextID()
                    .Paused = False

                    Log("Frames to render: " + .startFrame.ToString + "-" + .endFrame.ToString + " (" + (.endFrame - .startFrame + 1).ToString + ")")

                    JobList.Add(.ID, newJob)
                End With

                UpdateJobList()
            End If
        End With
    End Sub
    Private Sub RootWindow_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles RootWindow.Closing
        myServer.Stop()
    End Sub

    Private Sub JobPauseMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles JobPauseMenuItem.Click
        If JobListView.SelectedItem IsNot Nothing Then
            Dim jobID As Integer = Integer.Parse(CType(JobListView.SelectedItem, JobListViewItem).JobID)
            JobList(jobID).Paused = Not JobList(jobID).Paused

            UpdateJobList()
        End If
    End Sub
    Private Sub JobOpenOutputMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles JobOpenOutputMenuItem.Click
        If JobListView.SelectedItem IsNot Nothing Then
            Dim jobID As Integer = Integer.Parse(CType(JobListView.SelectedItem, JobListViewItem).JobID)
            Process.Start("explorer", RENDER_OUTPUT_DIR + JobList(jobID).fileMD5)
        End If
    End Sub
    Private Sub JobListView_ContextMenuOpening(sender As Object, e As ContextMenuEventArgs) Handles JobListView.ContextMenuOpening
        If JobListView.SelectedItem IsNot Nothing Then
            Dim jobID As Integer = Integer.Parse(CType(JobListView.SelectedItem, JobListViewItem).JobID)
            If JobList(jobID).Paused = True Then
                JobPauseMenuItem.Header = "Resume"
            Else
                JobPauseMenuItem.Header = "Pause"
            End If

            If IO.Directory.Exists(RENDER_OUTPUT_DIR + JobList(jobID).fileMD5) Then
                JobOpenOutputMenuItem.IsEnabled = True
            Else
                JobOpenOutputMenuItem.IsEnabled = False
            End If

        Else
            e.Handled = True
        End If
    End Sub

    Private Sub ClientListView_ContextMenuOpening(sender As Object, e As ContextMenuEventArgs) Handles ClientListView.ContextMenuOpening
        If ClientListView.SelectedItem IsNot Nothing Then
            Dim selectedClient As Connection = myServer.GetConnectionByNick(CType(ClientListView.SelectedItem, ClientListViewItem).ClientName)
            If selectedClient.paused = True Then
                ClientPauseMenuItem.Header = "Resume"
            Else
                ClientPauseMenuItem.Header = "Pause"
            End If

            If selectedClient.doSafeDisconnect Then
                ClientSafeDisconnectMenuItem.IsEnabled = False
                ClientPauseMenuItem.IsEnabled = False
                ClientSafeDisconnectMenuItem.Header = "Safe disconnecting..."
            Else
                ClientSafeDisconnectMenuItem.IsEnabled = True
                ClientPauseMenuItem.IsEnabled = True
                ClientSafeDisconnectMenuItem.Header = "Safe disconnect"
            End If

        Else
            e.Handled = True
        End If
    End Sub
    Private Sub ClientPauseMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ClientPauseMenuItem.Click
        If ClientListView.SelectedItem IsNot Nothing Then
            Dim selectedClient As Connection = myServer.GetConnectionByNick(CType(ClientListView.SelectedItem, ClientListViewItem).ClientName)
            selectedClient.paused = Not selectedClient.paused
            UpdateClientList()
        End If
    End Sub
    Private Sub ClientSafeDisconnectMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ClientSafeDisconnectMenuItem.Click
        If ClientListView.SelectedItem IsNot Nothing Then
            Dim selectedClient As Connection = myServer.GetConnectionByNick(CType(ClientListView.SelectedItem, ClientListViewItem).ClientName)
            selectedClient.doSafeDisconnect = True
            selectedClient.paused = True
            UpdateClientList()
        End If
    End Sub

    Private Sub ShowWebViewButton_Click(sender As Object, e As RoutedEventArgs) Handles ShowWebViewButton.Click
        Process.Start("http://127.0.0.1:" + SERVER_WEB_SERVER_PORT.ToString)
    End Sub
#End Region

#Region "Funktionen"
    Public Sub Log(s As String)
        Dispatcher.Invoke( _
            Sub()
                With LogTextBlock
                    If Not .Text = "" Then .Text += vbNewLine
                    .Text += "[" + Now.ToLongTimeString + "] " + s
                End With
                LogScrollViewer.ScrollToBottom()
            End Sub)
    End Sub
    Public Sub EnsureDownloadDirectory()
        If Not IO.Directory.Exists(WEB_SERVER_PATH + "dl\") Then
            IO.Directory.CreateDirectory(WEB_SERVER_PATH + "dl\")
        End If
    End Sub
    Public Sub EnsureOutputDirectory(file As String)
        If Not IO.Directory.Exists(RENDER_OUTPUT_DIR + file) Then
            IO.Directory.CreateDirectory(RENDER_OUTPUT_DIR + file)
        End If
    End Sub
    Public Sub EnsureJQueryScriptFile()
        If Not IO.File.Exists(WEB_SERVER_PATH + "jquery.js") Then
            IO.File.WriteAllText(WEB_SERVER_PATH + "jquery.js", My.Resources.jquery_script)
        End If
    End Sub
    Public Sub EnsureGraphicsFiles()
        If Not IO.File.Exists(WEB_SERVER_PATH + "load.gif") Then
            IO.File.WriteAllBytes(WEB_SERVER_PATH + "load.gif", My.Resources.ajax_loader)
        End If
        If Not IO.File.Exists(WEB_SERVER_PATH + "logo.png") Then
            IO.File.WriteAllBytes(WEB_SERVER_PATH + "logo.png", My.Resources.logo600)
        End If
        If Not IO.File.Exists(WEB_SERVER_PATH + "icon.ico") Then
            IO.File.WriteAllBytes(WEB_SERVER_PATH + "icon.ico", My.Resources.icon)
        End If
    End Sub
    Function BlenderFileInfo(blendfile As String) As String()
        Dim reVal As String()

        reVal = GetCMDOutput(IO.Path.GetFullPath("python/py.exe"), _
                                    """" + IO.Path.GetFullPath("scripts/sceneinfo.py ") + """ " + _
                                     """" + blendfile + """").Split("|"c)

        Return reVal
    End Function
    Function GetNextID() As Integer
        Dim reval As Integer = _lastID
        _lastID += 1
        Return reval
    End Function

    Private Sub UpdateJobList()
        Dim selectedIndex As Integer = JobListView.SelectedIndex
        Dim newJobItemsList As New List(Of JobListViewItem)

        For Each Job As Job In JobList.Values
            newJobItemsList.Add(New JobListViewItem With {.JobID = Job.ID.ToString, _
                                                          .JobMD5 = Job.fileMD5, _
                                                          .JobDoneProgress = Job.framesRendered.Count.ToString + " / " + Job.FrameCount.ToString, _
                                                          .JobInProgress = Job.framesProcessing.Count.ToString, _
                                                          .JobStatus = If(Job.Paused, "Paused", If(Job.framesRendered.Count = Job.FrameCount, "Done", "Active"))})
        Next

        JobListView.ItemsSource = newJobItemsList
        If Not newJobItemsList.Count - 1 > selectedIndex Then
            JobListView.SelectedIndex = selectedIndex
        End If
    End Sub
    Private Sub UpdateClientList()
        Dim selectedIndex As Integer = ClientListView.SelectedIndex
        Dim newClientItemsList As New List(Of ClientListViewItem)

        For Each client As Connection In myServer.Clients
            newClientItemsList.Add(New ClientListViewItem With {.ClientName = client.nick, _
                                                                .ClientIP = client.endPoint.Address.ToString, _
                                                                .ClientStatus = client.status.ToString, _
                                                                .ClientState = If(client.paused, If(client.doSafeDisconnect, "Disconnecting...", "Paused"), "Active")})
        Next

        ClientListView.ItemsSource = newClientItemsList

        If Not newClientItemsList.Count - 1 > selectedIndex Then
            ClientListView.SelectedIndex = selectedIndex
        End If
    End Sub

    Private Sub PublishHTML()
        Dim newContentElement As XElement = <center></center>


        Dim newClientItemsList As New List(Of ClientListViewItem)
        Dim newJobItemsList As New List(Of JobListViewItem)

        For Each client As Connection In myServer.Clients
            newClientItemsList.Add(New ClientListViewItem With _
                {.ClientName = client.nick, _
                 .ClientIP = client.endPoint.Address.ToString, _
                 .ClientStatus = client.status.ToString, _
                 .ClientState = If(client.paused, If(client.doSafeDisconnect, "Disconnecting...", "Paused"), "Active")})
        Next
        For Each Job As Job In JobList.Values
            newJobItemsList.Add(New JobListViewItem With _
                {.JobID = Job.ID.ToString, _
                 .JobMD5 = Job.fileMD5, _
                 .JobDoneProgress = Job.framesRendered.Count.ToString + " / " + Job.FrameCount.ToString, _
                 .JobInProgress = Job.framesProcessing.Count.ToString, _
                 .JobStatus = If(Job.Paused, "Paused", If(Job.framesRendered.Count = Job.FrameCount, "Done", "Active"))})
        Next


        With newContentElement
            .Add(<table>
                     <tr>
                         <th>Client Name</th>
                         <th>IP</th>
                         <th>Status</th>
                         <th>State</th>
                     </tr>
                 </table>)
            .Add(<br/>)
            .Add(<br/>)
            .Add(<table>
                     <tr>
                         <th>Job ID</th>
                         <th>Frames done</th>
                         <th>Frames processing</th>
                         <th>Status</th>
                     </tr>
                 </table>)


            With .<table>(0)
                For Each ClientItem As ClientListViewItem In newClientItemsList
                    Dim newTRElement As XElement = <tr></tr>

                    newTRElement.Add(New XElement("td", ClientItem.ClientName))
                    newTRElement.Add(New XElement("td", ClientItem.ClientIP))
                    newTRElement.Add(New XElement("td", ClientItem.ClientStatus))
                    newTRElement.Add(New XElement("td", ClientItem.ClientState))

                    .Add(newTRElement)
                Next
            End With

            With .<table>(1)
                For Each JobItem As JobListViewItem In newJobItemsList
                    Dim newTRElement As XElement = <tr></tr>

                    Dim aElement As New XElement("a", JobItem.JobID)
                    aElement.Add(New XAttribute("href", "/?l=output/" + JobItem.JobMD5))

                    newTRElement.Add(New XElement("td", aElement))
                    newTRElement.Add(New XElement("td", JobItem.JobDoneProgress))
                    newTRElement.Add(New XElement("td", JobItem.JobInProgress))
                    newTRElement.Add(New XElement("td", JobItem.JobStatus))

                    .Add(newTRElement)
                Next
            End With
        End With


        Dim newContentDocument As New XDocument
        newContentDocument.Add(newContentElement)

        Try
            EnsureGraphicsFiles()
            EnsureJQueryScriptFile()
            IO.File.WriteAllText(WEB_SERVER_PATH + "index.htm", My.Resources.loadscript)
        Catch
        End Try

        Try
            newContentDocument.Save(WEB_SERVER_PATH + "content.htm")
        Catch
        End Try
    End Sub
#End Region

End Class
