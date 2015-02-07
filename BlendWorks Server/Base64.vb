Module Base64
    ' String nach Base64 codieren
    Public Function ToBase64(ByVal nBytes As Byte()) As String
        ' jetzt das Byte-Array nach Base64 codieren
        Return System.Convert.ToBase64String(nBytes)
    End Function
    ' Base64-String in lesbaren String umwandeln
    Public Function FromBase64(ByVal sText As String) As Byte()
        ' Base64-String zunächst in ByteArray konvertieren
        Dim nBytes() As Byte = System.Convert.FromBase64String(sText)

        ' ByteArray in String umwandeln
        Return nBytes
    End Function
End Module
