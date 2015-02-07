Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Windows
Imports System.Windows.Threading

Public Class BWServer
    Private server As TcpListener
    Private client As New TcpClient
    Private ipendpoint As IPEndPoint = New IPEndPoint(IPAddress.Any, EXCHANGE_SERVER_PORT) ' eingestellt ist port 8000. dieser muss ggf. freigegeben sein!
    Private list As New List(Of Connection)
    Public Const NewLineStr As String = "<-nl>"
    Private _doStrop As Boolean = False
    Private IsStarted As Boolean = False
    Private _disp As Dispatcher

    Public Event LogToConsole(s As String)
    Public Event ServerStarted()
    Public Event ServerStopped()
    Public Event ActionPendingChanged(ispending As Boolean)
    Public Event MessageReceived(msg As String, func As String, from As String)
    Public Event ClientConnected(con As Connection)
    Public Event ClientDisconnected(nick As String)

    Public Sub New(disp As Dispatcher)
        _disp = disp
    End Sub

    Public Sub Start()
        RaiseEventActionPendingChanged(True)
        Task.Run(AddressOf StartupServer)
    End Sub

    Public Sub [Stop]()
        _doStrop = True
        Log("Stopping server...")
        RaiseEventActionPendingChanged(True)
    End Sub


    Private Sub StartupServer()
        Log("Starting server...")

        server = New TcpListener(ipendpoint)
        server.Start()
        IsStarted = True

        StartIPBroadcasting()

        Log("Server started successfully!")

        IsStarted = True
        RaiseEventServerStarted()
        RaiseEventActionPendingChanged(False)

        While True ' wir warten auf eine neue verbindung...

            Do Until server.Pending
                System.Threading.Thread.Sleep(500)
                If _doStrop Then
                    _doStrop = False
                    IsStarted = False

                    SendToAllClients("Server stopped", "disconnect")

                    server.Stop()

                    list.Clear()

                    RaiseEventServerStopped()
                    RaiseEventActionPendingChanged(False)

                    Log("Server stopped!")
                    Exit Sub
                End If
            Loop

            client = server.AcceptTcpClient

            Dim ipend As Net.IPEndPoint = CType(client.Client.RemoteEndPoint, Net.IPEndPoint)

            Dim c As New Connection ' und erstellen für die neue verbindung eine neue connection...
            c.stream = client.GetStream
            c.streamr = New StreamReader(c.stream)
            c.streamw = New StreamWriter(c.stream)
            c.endPoint = ipend
            c.status = BWStatus.Idle
            c.paused = False
            c.doSafeDisconnect = False

            c.nick = c.streamr.ReadLine ' falls das mit dem nick nicht gewünscht, auch diese zeile entfernen.

            Dim nicknameAlreadyExists As Boolean = False
            For Each Con As Connection In list
                If Con.nick = c.nick Then
                    nicknameAlreadyExists = True
                    Exit For
                End If
            Next

            If Not nicknameAlreadyExists Then
                list.Add(c) ' und fügen sie der liste der clients hinzu.
                Log(c.nick & " is connected.")

                RaiseEventClientConnected(c)
                ' falls alle anderen das auch lesen sollen können, an alle clients weiterleiten.

                Dim t As New System.Threading.Thread( _
                    Sub()
                        ListenToConnection(c)
                    End Sub)
                t.IsBackground = True
                t.Priority = System.Threading.ThreadPriority.AboveNormal
                t.Start()

            Else

                c.streamw.WriteLine(".~disconnect~that nickname does already exist!")
                c.streamw.Flush()
                c.stream.Close()
            End If
        End While
    End Sub
    Public Sub ListenToConnection(ByVal con As Connection)
        Do
            Try
                Dim tmp As String = con.streamr.ReadLine ' warten, bis etwas empfangen wird...

                If tmp.Split("~"c).Count = 3 Then

                    Dim [to] As New List(Of String)
                    [to].AddRange(tmp.Split("~"c)(0).Split({","}, StringSplitOptions.RemoveEmptyEntries))

                    Dim msg As String = tmp.Split("~"c)(2)
                    Dim func As String = tmp.Split("~"c)(1)

                    If [to].Contains(".") Then
                        RaiseEventMessageReceived(msg, func, con.nick)
                    End If

                    'outcoming:   to,to,to~func~msg OR .servercmd
                    'incoming:    from~func~msg
                    For Each c As Connection In list ' an alle clients weitersenden.
                        Try
                            If ([to].Count = 0 OrElse [to].Contains(c.nick)) And Not c.nick = con.nick Then
                                c.streamw.WriteLine(con.nick + "~" + func + "~" + msg)
                                c.streamw.Flush()
                                'Log(con.nick + " > " + c.nick + ": " + func + "(" + msg + ")")
                            End If
                        Catch
                        End Try
                    Next

                Else

                    Log("invalid format: '" + tmp + "'")
                End If

            Catch ex As Exception ' die aktuelle überwachte verbindung hat sich wohl verabschiedet.

                RaiseEventClientDisconnected(con.nick)
                list.Remove(con)
                Log("Connection with " + con.nick + " has ended")
                Exit Do
            End Try
        Loop
    End Sub

    Private Sub BroadcastIP()
        Dim client As New UdpClient()
        Dim ip As New IPEndPoint(IPAddress.Broadcast, 15001)

        Dim bytes As Byte() = Encoding.ASCII.GetBytes("blendworksserver")
        client.Send(bytes, bytes.Length, ip)
        client.Close()
    End Sub
    Private Sub StartIPBroadcasting()
        Task.Run( _
            Sub()
                Do
                    If Not IsStarted Then
                        Exit Do
                    End If

                    Try
                        BroadcastIP()
                        'Log("IP Boradcasted to network")
                    Catch ex As Exception
                    End Try

                    System.Threading.Thread.Sleep(5000)
                Loop
            End Sub)
    End Sub

    Public Sub Log(str As String)
        RaiseEventLogToConsole("[" + Now.ToLongTimeString + "] " + str)
    End Sub
    Public Sub SendToClient(msg As String, func As String, con As Connection)
        con.streamw.WriteLine(".~" + func + "~" + msg)
        con.streamw.Flush()
    End Sub
    Public Sub SendToAllClients(msg As String, func As String, Optional except As String() = Nothing)
        For Each c As Connection In list ' an alle clients weitersenden.
            If except Is Nothing OrElse except.Contains(c.nick) Then
                Try
                    c.streamw.WriteLine(".~" + func + "~" + msg)
                    c.streamw.Flush()
                    If Not func = "filedata" Then Log("Server" + " > " + c.nick + ": " + func + "(" + msg + ")")
                Catch
                End Try
            End If
        Next
    End Sub

    Public Function GetConnectionByNick(nick As String) As Connection
        Dim reVal As New Connection
        For Each con As Connection In list
            If con.nick = nick Then
                reVal = con
                Exit For
            End If
        Next
        Return reVal
    End Function
    Public Function IsNickConnected(nick As String) As Boolean
        Dim reVal As Boolean = False
        For Each con As Connection In list
            If con.nick = nick Then
                reVal = True
                Exit For
            End If
        Next
        Return reVal
    End Function

    Public ReadOnly Property Clients As List(Of Connection)
        Get
            Return list
        End Get
    End Property



#Region "Event Raiser"
    Private Sub RaiseEventActionPendingChanged(b As Boolean)
        Try
            _disp.Invoke(Sub() RaiseEvent ActionPendingChanged(b))
        Catch
        End Try
    End Sub
    Private Sub RaiseEventServerStarted()
        _disp.Invoke(Sub() RaiseEvent ServerStarted())
    End Sub
    Private Sub RaiseEventServerStopped()
        Try
            _disp.Invoke(Sub() RaiseEvent ServerStopped())
        Catch
        End Try
    End Sub
    Private Sub RaiseEventLogToConsole(s As String)
        _disp.Invoke(Sub() RaiseEvent LogToConsole(s))
    End Sub
    Private Sub RaiseEventMessageReceived(msg As String, func As String, from As String)
        _disp.Invoke(Sub() RaiseEvent MessageReceived(msg, func, from))
    End Sub
    Private Sub RaiseEventClientConnected(con As Connection)
        _disp.Invoke(Sub() RaiseEvent ClientConnected(con))
    End Sub
    Private Sub RaiseEventClientDisconnected(nick As String)
        Try
            _disp.Invoke(Sub() RaiseEvent ClientDisconnected(nick))
        Catch
        End Try
    End Sub
#End Region

#Region "Enums"
    Public Enum BWStatus As Integer
        Idle = 0
        Fetching = 1
        Rendering = 2
    End Enum
#End Region
End Class


Public Class Connection
    Public stream As NetworkStream
    Public streamw As StreamWriter
    Public streamr As StreamReader
    Public endPoint As IPEndPoint
    Public status As BWServer.BWStatus
    Public paused As Boolean
    Public doSafeDisconnect As Boolean
    Public nick As String ' natürlich optional, aber für die identifikation des clients empfehlenswert.
End Class
