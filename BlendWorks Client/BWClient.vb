Imports System.IO
Imports System.Net.Sockets
Imports System.Net
Imports System.Threading

Public Class BWClient
    Private stream As NetworkStream = Nothing
    Private streamw As StreamWriter = Nothing
    Private streamr As StreamReader = Nothing
    Private client As New TcpClient
    Private t As Thread
    Private nick As String = ""
    Private _connected As Boolean = False
    Private _disp As System.Windows.Threading.Dispatcher = Nothing
    Private _sendList As New List(Of String)

    Public ConnectionEP As IPEndPoint = Nothing
    Public Event MessageReceived(msg As String, func As String, from As String)
    Public Event ConnectionFailed()
    Public Event Disconnected()
    Public Event Connected(ep As IPEndPoint)
    Public Const NewLineStr As String = "<-nl>"

    Public Sub New(disp As System.Windows.Threading.Dispatcher)
        _disp = disp
    End Sub

    Public ReadOnly Property Nickname As String
        Get
            Return nick
        End Get
    End Property
    Public ReadOnly Property IsConnected As Boolean
        Get
            Return _connected
        End Get
    End Property

    Public Function Connect(ByVal IPEndPoint As IPEndPoint, nickname As String) As Boolean
        Try

            nick = nickname.Replace("~", "_")

            client.Connect(IPEndPoint)

            If client.Connected Then
                stream = client.GetStream
                streamw = New StreamWriter(stream)
                streamr = New StreamReader(stream)

                streamw.WriteLine(nick)
                streamw.Flush()

                t = New Thread(AddressOf Listen)
                t.Start()

                _connected = True

                _disp.Invoke( _
                    Sub()
                        RaiseEvent Connected(IPEndPoint)
                    End Sub)

                StartSendTask()

                ConnectionEP = IPEndPoint

                Return True

            Else
                _connected = False
                Return False
            End If
        Catch ex As Exception
            _connected = False
            Return False
        End Try
    End Function
    Public Sub Disconnect()
        If IsConnected Then

            Send("disconnect", "Disconnected by user")

            client.Close()

            _connected = False
            stream = Nothing
            streamw = Nothing
            streamr = Nothing
            client = New TcpClient

            ConnectionEP = Nothing

            _disp.Invoke( _
                Sub()
                    RaiseEvent Disconnected()
                End Sub)
        End If
    End Sub

    Public Sub StartSendTask()
        Task.Run( _
            Sub()
                Try
                    Do While _connected
                        If _sendList.Count > 0 Then
                            For i As Integer = 0 To _sendList.Count - 1
                                streamw.WriteLine(_sendList(i))
                                streamw.Flush()
                            Next
                            _sendList.Clear()
                        End If

                        System.Threading.Thread.Sleep(10)
                    Loop
                Catch ex As Exception
                    Debug.Print(ex.Message)
                End Try
            End Sub)
    End Sub
    Public Function ServerCommand(cmd As String) As Boolean
        Try
            If Not cmd.Contains("~") Then
                _sendList.Add("." + cmd.Replace(vbNewLine, NewLineStr))
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function
    Public Function Send(func As String, msg As String) As Boolean
        Try
            _sendList.Add(".~" + func.Replace("~", "_") + "~" + msg.Replace("~", "_").Replace(vbNewLine, NewLineStr))
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function
    Public Function Send(func As String, msg As String, ParamArray [to] As String()) As Boolean
        Try
            Dim toCombined As String = ""

            For Each receiver As String In [to]
                If Not toCombined = "" Then toCombined += ","
                toCombined += receiver.Replace("~", "_")
            Next

            _sendList.Add(toCombined + "~" + func.Replace("~", "_") + "~" + msg.Replace("~", "_").Replace(vbNewLine, NewLineStr))
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub Listen()
        While client.Connected And IsConnected
            Try
                Dim received As String = streamr.ReadLine
                If received.Split("~"c).Count = 3 Then

                    '   from~func~msg


                    _disp.Invoke( _
                      Sub()
                          RaiseEvent MessageReceived(received.Split("~"c)(2).Replace(NewLineStr, vbNewLine), received.Split("~"c)(1), received.Split("~"c)(0))
                      End Sub)
                End If
            Catch
                Try
                    _disp.Invoke( _
                                        Sub()
                                            RaiseEvent ConnectionFailed()
                                        End Sub)
                Catch ex As Exception
                End Try
            End Try
        End While
    End Sub
End Class
