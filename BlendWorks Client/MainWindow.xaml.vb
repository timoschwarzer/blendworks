Imports System.Net
Imports Microsoft.Win32

Class MainWindow
    Public WithEvents myClient As New BWClient(Dispatcher)
    Public myWebServer As WebServerTwo

#Region "Variables"
    Private currentFileName As String = ""
#End Region

#Region "Constructor"
    Public Sub New()
        InitializeComponent()

        BlenderExecutableTextBox.Text = My.Settings.BlenderExecutable
    End Sub
#End Region

#Region "Event Handler"
    Private Sub AboutLabel_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles AboutLabel.MouseLeftButtonUp
        Dim newAboutWindow As New AboutWindow
        newAboutWindow.Owner = Me
        newAboutWindow.ShowDialog()
    End Sub

    Private Sub myClient_Connected(ip As IPEndPoint) Handles myClient.Connected
        myWebServer = New WebServerTwo(WEB_SERVER_PATH)

        Log("Connected to " + ip.Address.ToString)
        Title = "BlendWorks - Connected to " + ip.Address.ToString

        ConnectButton.IsEnabled = False
        DisconnectButton.IsEnabled = True

        Dim connectAnimBase As New AnimationBase(ConnectButton)
        Dim disconnectAnimBase As New AnimationBase(DisconnectButton)

        connectAnimBase.Fade(0, 1000, True)
        connectAnimBase.TranslateTransformAnimation(0, 0, ConnectButton.Width, 0, 1000)

        disconnectAnimBase.Fade(1, 1000, True)
        disconnectAnimBase.TranslateTransformAnimation(-DisconnectButton.Width, 0, 0, 0, 1000)

        SetStatus(0)
    End Sub
    Private Sub myClient_Disconnected() Handles myClient.Disconnected
        myWebServer.Dispose()

        Log("Disconnected.")
        Title = "BlendWorks"

        ConnectButton.IsEnabled = True
        DisconnectButton.IsEnabled = False

        Dim connectAnimBase As New AnimationBase(ConnectButton)
        Dim disconnectAnimBase As New AnimationBase(DisconnectButton)

        connectAnimBase.Fade(1, 1000, True)
        connectAnimBase.TranslateTransformAnimation(ConnectButton.Width, 0, 0, 0, 1000)

        disconnectAnimBase.Fade(0, 1000, True)
        disconnectAnimBase.TranslateTransformAnimation(0, 0, -DisconnectButton.Width, 0, 1000)

        SetStatus(-1)
    End Sub
    Private Sub myClient_MessageReceived(msg As String, func As String, from As String) Handles myClient.MessageReceived
        If from = "." Then

            Select Case func

                Case "disconnect"
                    myClient.Disconnect()
                    Log("Server: " + msg)

                Case "renderfile"
                    Dim args As String() = msg.Split("%"c)
                    Dim filename As String = args(0)
                    Dim frame As Integer = Integer.Parse(args(1))
                    Dim jobID As Integer = Integer.Parse(args(2))
                    Dim fileUrl As String = "http://" + myClient.ConnectionEP.Address.ToString + ":" + SERVER_WEB_SERVER_PORT.ToString + "/dl/" + filename

                    BlenderLogTextBlock.Text = ""

                    Task.Run( _
                        Sub()
                            If Not IO.File.Exists(FILES_PATH + filename) Then
                                SetStatus(1)
                                Log("Downloading file from server...")
                                EnsureFilesDirectoriy()
                                My.Computer.Network.DownloadFile(fileUrl, FILES_PATH + filename, "", "", True, 20000, True)
                                Log("File downloaded successfully")
                                SetStatus(0)
                            End If

                            RenderFile(filename, frame, jobID)
                        End Sub)

            End Select

        Else


        End If
    End Sub

    Private Sub ConnectButton_Click(sender As Object, e As RoutedEventArgs) Handles ConnectButton.Click
        If Not IO.File.Exists(My.Settings.BlenderExecutable) Then
            MessageBox.Show("Can't connect cause of the following problems:" + vbNewLine + _
                            vbNewLine + _
                            "The Blender executable cannot be found. Please select a new one.", "", MessageBoxButton.OK, MessageBoxImage.Error)
            Exit Sub
        End If

        ConnectButton.IsEnabled = False

        If ExternalIPCheckBox.IsChecked Then
            Dim IPInput As String = InputBox("Server IP:")
            If Not IPInput = "" AndAlso IPAddress.TryParse(IPInput, Nothing) Then
                myClient.Connect(New IPEndPoint(IPAddress.Parse(IPInput), EXCHANGE_SERVER_PORT), My.Computer.Name)
            End If
        Else
            Log("Searching for server...")

            Dim newBroadcastReceiver As New BWIPBroadcastReceiver(Dispatcher)
            With newBroadcastReceiver
                AddHandler .ServerFound, _
                    Sub(ip As IPEndPoint)
                        Log("Found server at " + ip.Address.ToString)
                        Log("Connecting to " + ip.Address.ToString + "...")
                        myClient.Connect(New IPEndPoint(ip.Address, EXCHANGE_SERVER_PORT), My.Computer.Name)
                    End Sub
            End With

            newBroadcastReceiver.WaitForServerAsync()
        End If
    End Sub
    Private Sub DisconnectButton_Click(sender As Object, e As RoutedEventArgs) Handles DisconnectButton.Click
        If myClient.IsConnected Then
            myClient.Disconnect()
        End If
    End Sub
    Private Sub RootWindow_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles RootWindow.Closing
        If myClient.IsConnected Then
            myClient.Disconnect()
        End If
    End Sub
    Private Sub RootWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles RootWindow.Loaded
        If Not My.Settings.upgraded Then
            Try
                My.Settings.Upgrade()
                My.Settings.upgraded = True
                My.Settings.Save()
            Catch
            End Try
        End If

        SetStatus(-1)
    End Sub
    Private Sub ShowWebViewButton_Click(sender As Object, e As RoutedEventArgs) Handles ShowWebViewButton.Click
        Try
            Process.Start("http://" + myClient.ConnectionEP.Address.ToString + ":" + SERVER_WEB_SERVER_PORT.ToString)
        Catch ex As Exception
            MessageBox.Show("You are not connected to a server.", "", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub
    Private Sub ChangeBlenderExecutableButton_Click(sender As Object, e As RoutedEventArgs) Handles ChangeBlenderExecutableButton.Click
        Dim OFD As New OpenFileDialog
        OFD.Filter = "Blender Executables|blender.exe;blender-app.exe"

        If IO.File.Exists(My.Settings.BlenderExecutable) Then OFD.InitialDirectory = IO.Path.GetDirectoryName(My.Settings.BlenderExecutable)

        If OFD.ShowDialog = True Then
            My.Settings.BlenderExecutable = OFD.FileName
            My.Settings.Save()
            BlenderExecutableTextBox.Text = My.Settings.BlenderExecutable
        End If
    End Sub
#End Region

#Region "Functions"
    Public Sub Log(str As String)
        Try
            Dispatcher.Invoke( _
                Sub()
                    With LogTextBlock
                        If Not .Text = "" Then .Text += vbNewLine
                        .Text += "[" + Now.ToLongTimeString + "] " + str.Replace(vbNewLine, vbNewLine + "[" + Now.ToLongTimeString + "] ")
                    End With
                    LogScrollViewer.ScrollToBottom()
                End Sub)
        Catch
        End Try
    End Sub
    Public Sub BlenderLog(str As String)
        Try
            Dispatcher.Invoke( _
                Sub()
                    With BlenderLogTextBlock
                        .Text = "[" + Now.ToLongTimeString + "] " + str.Replace(vbNewLine, vbNewLine + "[" + Now.ToLongTimeString + "] ")
                    End With
                End Sub)
        Catch
        End Try
    End Sub
    Public Sub EnsureDownloadDirectory()
        If Not IO.Directory.Exists(WEB_SERVER_PATH + "dl\") Then
            IO.Directory.CreateDirectory(WEB_SERVER_PATH + "dl\")
        End If
    End Sub
    Public Sub EnsureFilesDirectoriy()
        If Not IO.Directory.Exists(FILES_PATH) Then
            IO.Directory.CreateDirectory(FILES_PATH)
        End If
    End Sub
    Public Sub RenderFile(file As String, frame As Integer, jobID As Integer)
        SetStatus(2)

        Dim fileMD5 As String = IO.Path.GetFileNameWithoutExtension(FILES_PATH + file)
        Dim fileID As Guid = Guid.NewGuid

        Log("Rendering frame " + frame.ToString + "...")
        Dim pStartInfo As New ProcessStartInfo(My.Settings.BlenderExecutable, "-b """ + FILES_PATH + file + """ -o //output/" + fileID.ToString + "_########" + " -F PNG -x 1 -f " + frame.ToString)
        pStartInfo.CreateNoWindow = True
        pStartInfo.RedirectStandardOutput = True
        pStartInfo.RedirectStandardError = True
        pStartInfo.UseShellExecute = False
        pStartInfo.WindowStyle = ProcessWindowStyle.Hidden
        Dim p As New Process
        p.StartInfo = pStartInfo

        p.EnableRaisingEvents = True
        p.Start()
        p.BeginOutputReadLine()

        AddHandler p.OutputDataReceived, _
            Sub(sender As Object, e As DataReceivedEventArgs)
                If Not String.IsNullOrEmpty(e.Data) Then BlenderLog(e.Data)
            End Sub

        AddHandler p.Exited, _
            Sub()
                Log("Rendering finished.")
                Log("Sending file...")

                Dim outputFile As String = FILES_PATH + "output\" + fileID.ToString + "_" + frame.ToString("D8") + ".png"
                EnsureDownloadDirectory()

                Try
                    IO.File.Move(outputFile, WEB_SERVER_PATH + "dl\" + fileID.ToString + ".png")
                Catch
                    Log("Move error on frame " + frame.ToString)
                End Try


                SetStatus(0)

                System.Threading.Thread.Sleep(250)

                myClient.Send("renderfinished", fileID.ToString + ".png" + "%" + fileMD5 + "%" + frame.ToString + "%" + jobID.ToString)
            End Sub
    End Sub
    Public Sub SetStatus(s As Integer)
        Dim sendStatusToServer As Boolean = True

        Try
            Dispatcher.Invoke( _
                      Sub()
                          With StatusLabel
                              Select Case s
                                  Case -1
                                      .Content = "Not Connected"
                                      sendStatusToServer = False

                                  Case 0
                                      .Content = "Idle"

                                  Case 1
                                      .Content = "Fetching..."

                                  Case 2
                                      .Content = "Rendering..."

                              End Select
                          End With
                      End Sub)
        Catch
        End Try


        If myClient.IsConnected And sendStatusToServer Then myClient.Send("status", s.ToString)
    End Sub
#End Region

End Class
