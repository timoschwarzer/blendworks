Public Class Job
    Public fileMD5 As String
    Public startFrame As Integer
    Public endFrame As Integer
    Public framesRendered As List(Of Integer)
    Public framesProcessing As List(Of Integer)
    Public ID As Integer
    Public Paused As Boolean

    Public ReadOnly Property FrameCount As Integer
        Get
            Return endFrame - startFrame + 1
        End Get
    End Property
    Public ReadOnly Property ProcessedAllFrames As Boolean
        Get
            Return endFrame - startFrame + 1 = framesRendered.Count + framesProcessing.Count
        End Get
    End Property
End Class
