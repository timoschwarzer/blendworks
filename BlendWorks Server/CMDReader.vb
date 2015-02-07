Module CMDReader
        ''' <summary>
        ''' Gibt das Output einer Konsolenanwendung aus
        ''' </summary>
        ''' <param name="path">Der Pfad zur Anwendung</param>
        ''' <param name="args">Mögliche Argumente</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetCMDOutput(path As String, Optional args As String = "") As String
            Dim proc As New Process
            proc.StartInfo.FileName = path
            proc.StartInfo.Arguments = args
            proc.StartInfo.CreateNoWindow = True
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden

            ' Umleiten der Ausgabe:
            proc.StartInfo.UseShellExecute = False
            proc.StartInfo.RedirectStandardOutput = True
            proc.StartInfo.RedirectStandardInput = True
            proc.StartInfo.RedirectStandardError = True

            ' Prozess starten:
            proc.Start()
            proc.WaitForExit()

            ' Ausgabe einlesen:
            Dim output As String = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd()

            Return output
        End Function
End Module
