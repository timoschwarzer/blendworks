Module Base64
    Public Function ToBase64(ByVal nBytes As Byte()) As String
        Return System.Convert.ToBase64String(nBytes)
    End Function
    Public Function FromBase64(ByVal sText As String) As Byte()
        Dim nBytes() As Byte = System.Convert.FromBase64String(sText)
        Return nBytes
    End Function
End Module
