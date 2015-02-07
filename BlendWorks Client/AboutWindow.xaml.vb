Imports System.Windows.Media.Animation

Public Class AboutWindow

#Region "Variables"
    Private _smileyShown As Boolean = False
#End Region

#Region "Event Handler"
    Private Sub DonateButton_Click(sender As Object, e As RoutedEventArgs) Handles DonateButton.Click
        Dim s_anim As New DoubleAnimation(1, New Duration(TimeSpan.FromMilliseconds(400)))
        s_anim.EasingFunction = New ElasticEase With {.Oscillations = 1}

        SmileyLabelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, s_anim)
        SmileyLabelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, s_anim)

        Task.Run( _
            Sub()
                If Not _smileyShown Then System.Threading.Thread.Sleep(1000)
                System.Diagnostics.Process.Start("http://setagon.com/donate")
                _smileyShown = True
            End Sub)
    End Sub
    Private Sub AboutWindowRoot_Loaded(sender As Object, e As RoutedEventArgs) Handles AboutWindowRoot.Loaded
        RenderedFramesLabel.Content = String.Format("I've rendered {0} frames so far! Thank you for using BlendWorks!", My.Settings.rendered_frames.ToString)
    End Sub
#End Region

End Class
