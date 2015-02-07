Module Extensions
    Public Function GetFileSize(ByVal mySize As Single) As String
        Try
            Select Case mySize
                Case 0 To 1023
                    Return mySize & " B"
                Case 1024 To 1048575
                    Return Format(mySize / 1024, "###0") & " KB"
                Case 1048576 To 1043741824
                    Return Format(mySize / 1024 ^ 2, "###0.0") & " MB"
                Case Is > 1043741824
                    Return Format(mySize / 1024 ^ 3, "###0.0") & " GB"
            End Select

            Return "0 B"

        Catch ex As Exception
            Return "0 B"
        End Try
    End Function
End Module
