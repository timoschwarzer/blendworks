Module CMDReader
    Public Function GetCMDOutput(path As String, Optional args As String = "") As String
        Dim proc As New Process
        proc.StartInfo.FileName = path
        proc.StartInfo.Arguments = args
        proc.StartInfo.CreateNoWindow = True
        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden

        proc.StartInfo.UseShellExecute = False
        proc.StartInfo.RedirectStandardOutput = True
        proc.StartInfo.RedirectStandardInput = True
        proc.StartInfo.RedirectStandardError = True

        proc.Start()
        proc.WaitForExit()

        Dim output As String = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd()

        Return output
    End Function
End Module
