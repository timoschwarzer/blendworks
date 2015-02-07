Imports System.Net.Sockets
Imports System.Net
Imports System.Threading
Imports System.Text
Imports System.Collections.Generic
Imports System.IO
Imports System
Imports System.Web

Public Class WebServer
    Public running As Boolean = False
    ' Is it running?
    Private timeout As Integer = 8
    ' Time limit for data transfers.
    Private charEncoder As Encoding = Encoding.UTF8
    ' To encode string
    Private serverSocket As Socket
    ' Our server socket
    Private contentPath As String
    ' Root path of our contents
    ' Content types that are supported by our server
    ' You can add more...
    ' To see other types: http://www.webmaster-toolkit.com/mime-types.shtml
    '{ "extension", "content type" }
    Private extensions As New Dictionary(Of String, String)() From { _
        {"htm", "text/html"}, _
        {"png", "image/png"}, _
        {"blend", "application/blend"}, _
        {"ico", "image/x-icon"}, _
        {"gif", "image/gif"}, _
        {"js", "text/javascript"} _
    }

    Public Function start(ipAddress As IPAddress, port As Integer, maxNOfCon As Integer, contentPath As String) As Boolean
        If running Then
            Return False
        End If
        ' If it is already running, exit.
        Try
            ' A tcp/ip socket (ipv4)
            serverSocket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            serverSocket.Bind(New IPEndPoint(ipAddress, port))
            serverSocket.Listen(maxNOfCon)
            serverSocket.ReceiveTimeout = timeout
            serverSocket.SendTimeout = timeout
            running = True

            If Not IO.Directory.Exists(contentPath) Then IO.Directory.CreateDirectory(contentPath)
            Me.contentPath = contentPath
        Catch
            Return False
        End Try

        ' Our thread that will listen connection requests and create new threads to handle them.
        Dim requestListenerT As New Thread( _
            Sub()
                While running
                    Dim clientSocket As Socket
                    Try
                        clientSocket = serverSocket.Accept()
                        ' Create new thread to handle the request and continue to listen the socket.
                        Dim requestHandler As New Thread( _
                            Sub()
                                    clientSocket.ReceiveTimeout = timeout
                                    clientSocket.SendTimeout = timeout
                                    Try
                                        handleTheRequest(clientSocket)
                                    Catch
                                        Try
                                            clientSocket.Close()
                                        Catch
                                        End Try
                                    End Try

                                End Sub)
                        requestHandler.Start()
                    Catch
                    End Try
                End While

            End Sub)
        requestListenerT.Start()

        Return True
    End Function

    Public Sub [stop]()
        If running Then
            running = False
            Try
                serverSocket.Close()
            Catch
            End Try
            serverSocket = Nothing
        End If
    End Sub

    Private Sub handleTheRequest(clientSocket As Socket)
        Dim buffer As Byte() = New Byte(CInt(1024 ^ 2)) {}
        ' 10 kb, just in case
        Dim receivedBCount As Integer = clientSocket.Receive(buffer)
        ' Receive the request
        Dim strReceived As String = charEncoder.GetString(buffer, 0, receivedBCount)

        ' Parse the method of the request
        Dim httpMethod As String = strReceived.Substring(0, strReceived.IndexOf(" "))

        Dim start As Integer = strReceived.IndexOf(httpMethod) + httpMethod.Length + 1
        Dim length As Integer = strReceived.LastIndexOf("HTTP") - start - 1
        Dim requestedUrl As String = strReceived.Substring(start, length)

        Dim requestedFile As String
        If httpMethod.Equals("GET") OrElse httpMethod.Equals("POST") Then
            requestedFile = requestedUrl.Split("?"c)(0)
        Else
            ' You can implement other methods...
            notImplemented(clientSocket)
            Return
        End If

        requestedFile = requestedFile.Replace("/", "\").Replace("\..", "")
        ' Not to go back
        start = requestedFile.LastIndexOf("."c) + 1
        If start > 0 Then
            length = requestedFile.Length - start
            Dim extension As String = requestedFile.Substring(start, length)
            If extensions.ContainsKey(extension) Then
                ' Do we support this extension?
                If File.Exists(contentPath & requestedFile) Then
                    ' If yes check existence
                    ' Everything is OK, send requested file with correct content type:
                    sendOkResponse(clientSocket, File.ReadAllBytes(contentPath & requestedFile), extensions(extension))
                Else
                    notFound(clientSocket)
                End If
                ' We don't support this extension. We are assuming that it doesn't exist.
            End If
        Else
            ' If file is not specified try to send index.htm or index.html
            ' You can add more (for example "default.html")
            If requestedFile.Substring(length - 1, 1) <> "\" Then
                requestedFile += "\"
            End If
            If File.Exists(contentPath & requestedFile & "index.htm") Then
                sendOkResponse(clientSocket, File.ReadAllBytes(contentPath & requestedFile & "\index.htm"), "text/html")
            ElseIf File.Exists(contentPath & requestedFile & "index.html") Then
                sendOkResponse(clientSocket, File.ReadAllBytes(contentPath & requestedFile & "\index.html"), "text/html")
            Else
                notFound(clientSocket)
            End If
        End If
    End Sub

    Private Sub notImplemented(clientSocket As Socket)
        sendResponse(clientSocket, "<html><head><meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8""></head><body><h2>tiweb WebServer</h2><div>501 - Method Not Implemented</div></body></html>", "501 Not Implemented", "text/html")
    End Sub

    Private Sub notFound(clientSocket As Socket)
        sendResponse(clientSocket, "<html><head><meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8""></head><body><h2>tiweb WebServer</h2><div>404 - Not Found</div></body></html>", "404 Not Found", "text/html")
    End Sub

    Private Sub sendOkResponse(clientSocket As Socket, bContent As Byte(), contentType As String)
        sendResponse(clientSocket, bContent, "200 OK", contentType)
    End Sub

    ' For strings
    Private Sub sendResponse(clientSocket As Socket, strContent As String, responseCode As String, contentType As String)
        Dim bContent As Byte() = charEncoder.GetBytes(strContent)
        sendResponse(clientSocket, bContent, responseCode, contentType)
    End Sub

    ' For byte arrays
    Private Sub sendResponse(clientSocket As Socket, bContent As Byte(), responseCode As String, contentType As String)
        Try
            Dim bHeader As Byte() = charEncoder.GetBytes("HTTP/1.1 " & responseCode & vbCr & vbLf & "Server: tiweb_WebServer" & vbCr & vbLf & "Content-Length: " & bContent.Length.ToString() & vbCr & vbLf & "Connection: close" & vbCr & vbLf & "Content-Type: " & contentType & vbCr & vbLf & vbCr & vbLf)
            clientSocket.Send(bHeader)
            clientSocket.Send(bContent)
            clientSocket.Close()
        Catch
        End Try
    End Sub
End Class


Public Class WebServerTwo
    Implements IDisposable

    Public rootPath As String
    Private Const bufferSize As Integer = 1024 * 512
    '512KB
    Private ReadOnly http As HttpListener
    Public Sub New(ByVal rootPath As String)
        http = New HttpListener()
        Task.Run( _
            Sub()
                Me.rootPath = rootPath
                http.Prefixes.Add("http://+:" + SERVER_WEB_SERVER_PORT.ToString + "/")
                http.Start()
                http.BeginGetContext(AddressOf requestWait, Nothing)
            End Sub)
    End Sub
    Public Sub Dispose() Implements System.IDisposable.Dispose
        http.[Stop]()
        http.Close()
    End Sub
    Private Sub requestWait(ByVal ar As IAsyncResult)
        If Not http.IsListening Then
            Return
        End If
        Dim c = http.EndGetContext(ar)
        http.BeginGetContext(AddressOf requestWait, Nothing)
        Dim url = tuneUrl(c.Request.RawUrl)
        Dim fullPath = If(String.IsNullOrEmpty(url), rootPath, Path.Combine(rootPath, url.Split("?"c)(0)))
        Dim indexFile As String = Path.Combine(fullPath, "index.htm")
        If Directory.Exists(fullPath) Then
            If File.Exists(indexFile) Then
                returnFile(c, indexFile)
            Else
                returnDirContents(c, fullPath)
            End If
        ElseIf File.Exists(fullPath) Then
            returnFile(c, fullPath)
        ElseIf File.Exists(fullPath.Replace(".get_png", ".png")) Then
            returnFile(c, fullPath)
        Else
            return404(c)
        End If
    End Sub
    Private Sub returnDirContents(ByVal context As HttpListenerContext, ByVal dirPath As String)
        Try

        Catch ex As Exception

        End Try
        context.Response.ContentType = "text/html"
        context.Response.ContentEncoding = Encoding.UTF8
        Using sw = New StreamWriter(context.Response.OutputStream)
            Dim dirs = Directory.GetDirectories(dirPath)
            For Each d As String In dirs
                Dim link = d.Replace(rootPath, "").Replace("\"c, "/"c)
                sw.WriteLine("-> <a href=""/?l=" + link + """> " + Path.GetFileName(d) + "</a><br/>")
            Next
            Dim files = Directory.GetFiles(dirPath)
            For Each f As String In files
                Dim link = f.Replace(rootPath, "").Replace("\"c, "/"c).Replace(".png", ".get_png")
                If Path.GetExtension(f) = ".png" Then
                    sw.WriteLine("<a href=""javascript:refreshFunc('" + link + "')""> " + Path.GetFileName(f).Replace(".png", "") + "</a> <br/> ")
                Else
                    sw.WriteLine("<a href=""" + link + """> " + Path.GetFileName(f) + " </a> <br/> ")
                End If

            Next
        End Using
        context.Response.OutputStream.Close()
    End Sub
    Private Sub returnFile(ByVal context As HttpListenerContext, ByVal filePath As String)
        Try
            If filePath.EndsWith(".get_png") Then
                context.Response.ContentType = "text/html"
                context.Response.ContentEncoding = Encoding.UTF8
                Using sw = New StreamWriter(context.Response.OutputStream)

                    Dim currentLink = filePath.Replace(rootPath, "").Replace("\"c, "/"c).Replace(".get_png", ".png")
                    Dim nextFile As String = ""
                    Dim prevFile As String = ""

                    Dim nextFileIsNextFile As Boolean = False

                    Dim nextFileSubmitted As Boolean = False
                    Dim prevFileSubmitted As Boolean = False

                    Dim writeString As String = "<h4>" + Path.GetFileName(filePath.Replace(".get_png", ".png")) + "</h4><img style=""max-width: 90vw; max-height: 90vh;"" src=""" + currentLink + """><br>"

                    For Each File As String In Directory.GetFiles(Path.GetDirectoryName(filePath))
                        If Path.GetExtension(File) = ".png" Then



                            If nextFileIsNextFile And Not nextFileSubmitted Then
                                nextFileSubmitted = True
                                Dim link = File.Replace(rootPath, "").Replace("\"c, "/"c).Replace(".png", ".get_png")
                                writeString += "&nbsp;<a href=""javascript:refreshFunc('" + link + "')"">Next</a>"
                            End If

                            If File = filePath.Replace(".get_png", ".png") Then
                                nextFileIsNextFile = True

                                If Not prevFileSubmitted Then
                                    prevFileSubmitted = True
                                    Dim link = prevFile.Replace(rootPath, "").Replace("\"c, "/"c).Replace(".png", ".get_png")
                                    writeString += "&nbsp;<a href=""javascript:refreshFunc('" + link + "')"">Prev</a>"
                                End If
                            End If

                            prevFile = File
                        End If
                    Next

                    sw.WriteLine(writeString)
                End Using
                context.Response.OutputStream.Close()
            Else
                context.Response.ContentType = getcontentType(Path.GetExtension(filePath))
                Dim buffer = New Byte(bufferSize - 1) {}
                Using fs = File.OpenRead(filePath)
                    context.Response.ContentLength64 = fs.Length
                    Dim read As Integer
                    While (InlineAssignHelper(read, fs.Read(buffer, 0, buffer.Length))) > 0
                        context.Response.OutputStream.Write(buffer, 0, read)
                    End While
                End Using
                context.Response.OutputStream.Close()
            End If
        Catch ex As Exception
            return400(context)
        End Try
    End Sub
    Private Shared Sub return404(ByVal context As HttpListenerContext)
        context.Response.StatusCode = 404
        context.Response.Close()
    End Sub
    Private Shared Sub return400(ByVal context As HttpListenerContext)
        context.Response.StatusCode = 400
        context.Response.Close()
    End Sub
    Private Shared Function tuneUrl(ByVal url As String) As String
        url = url.Replace("/"c, "\"c)
        url = HttpUtility.UrlDecode(url, Encoding.UTF8)
        url = url.Substring(1)
        Return url
    End Function
    Private Shared Function getcontentType(ByVal extension As String) As String
        Select Case extension
            Case ".avi"
                Return "video/x-msvideo"
            Case ".css"
                Return "text/css"
            Case ".doc"
                Return "application/msword"
            Case ".gif"
                Return "image/gif"
            Case ".htm", ".html"
                Return "text/html"
            Case ".jpg", ".jpeg"
                Return "image/jpeg"
            Case ".js"
                Return "application/x-javascript"
            Case ".mp3"
                Return "audio/mpeg"
            Case ".png"
                Return "image/png"
            Case ".pdf"
                Return "application/pdf"
            Case ".ppt"
                Return "application/vnd.ms-powerpoint"
            Case ".zip"
                Return "application/zip"
            Case ".txt"
                Return "text/plain"
            Case ".blend"
                Return "application/blend"
            Case Else
                Return "application/octet-stream"
        End Select
    End Function
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
        target = value
        Return value
    End Function
End Class