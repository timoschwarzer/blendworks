Module Public_Variables
    Public Const MAX_CONNECTIONS As Integer = 50
    Public Const EXCHANGE_SERVER_PORT As Integer = 9912
    Public Const SERVER_WEB_SERVER_PORT As Integer = 9913
    Public Const CLIENT_WEB_SERVER_PORT As Integer = 9914
    Public WEB_SERVER_PATH As String = My.Computer.FileSystem.SpecialDirectories.CurrentUserApplicationData + "\webserver\"
    Public RENDER_OUTPUT_DIR As String = My.Computer.FileSystem.SpecialDirectories.CurrentUserApplicationData + "\webserver\output\"
End Module
