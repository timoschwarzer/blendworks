Imports System.Net
Imports System.Net.Sockets
Imports System.Text

Public Class BWIPBroadcastReceiver
    Public Class UdpState
        Public e As IPEndPoint
        Public u As UdpClient
    End Class

    Private _disp As System.Windows.Threading.Dispatcher = Nothing

    Public Event ServerFound(ip As IPEndPoint)

    Public messageReceived As Boolean = False

    Public Sub New(disp As System.Windows.Threading.Dispatcher)
        _disp = disp
    End Sub

    Public Sub WaitForServer()
        Dim ip As New IPEndPoint(IPAddress.Any, 15001)
        Dim udp As New UdpClient(ip)

        Dim state As New UdpState()
        state.e = ip
        state.u = udp

        udp.BeginReceive(New AsyncCallback(AddressOf Receive), state)

        Do While Not messageReceived
            System.Threading.Thread.Sleep(100)
        Loop
    End Sub
    Public Sub WaitForServerAsync()
        Task.Run( _
            Sub()
                WaitForServer()
            End Sub)
    End Sub

    Private Sub Receive(ByVal ar As IAsyncResult)
        Dim udp As UdpClient = CType((CType(ar.AsyncState, UdpState)).u, UdpClient)
        Dim ip As IPEndPoint = CType((CType(ar.AsyncState, UdpState)).e, IPEndPoint)

        Dim bytes As Byte() = udp.EndReceive(ar, ip)
        Dim message As String = Encoding.ASCII.GetString(bytes)
        If message = "blendworksserver" Then
            _disp.Invoke( _
                Sub()
                    RaiseEvent ServerFound(ip)
                    udp.Close()
                End Sub)
            messageReceived = True
        End If
    End Sub
End Class
