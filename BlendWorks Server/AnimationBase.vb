Imports System.Windows.Media.Animation

Public Class AnimationBase


#Region "Variablen"
    Private _myControl As UIElement = Nothing
#End Region

#Region "Constructor"
    Public Sub New(_myCtrl As UIElement)
        _myControl = _myCtrl
    End Sub
#End Region

#Region "Functions"
    Public Sub Fade([to] As Double, Optional duration As Integer = 300, Optional EnablePowerEase As Boolean = False)
        Dim anim As New DoubleAnimation([to], New Duration(TimeSpan.FromMilliseconds(duration)))
        Dim pe As New PowerEase With {.EasingMode = EasingMode.EaseInOut, .Power = 6}

        If EnablePowerEase Then anim.EasingFunction = pe

        _myControl.BeginAnimation(UIElement.OpacityProperty, anim)
    End Sub
    Public Sub Fade([from] As Double, [to] As Double, Optional duration As Integer = 300, Optional EnablePowerEase As Boolean = False)
        Dim anim As New DoubleAnimation([from], [to], New Duration(TimeSpan.FromMilliseconds(duration)))
        _myControl.BeginAnimation(UIElement.OpacityProperty, anim)
    End Sub
    Public Sub TranslateTransformAnimation(fromX As Double, fromY As Double, toX As Double, toY As Double, Optional duration As Integer = 1000, Optional EnablePowerEase As Boolean = True)
        Dim pe As New PowerEase With {.EasingMode = EasingMode.EaseInOut, .Power = 6}

        Dim animX As New DoubleAnimation(toX, New Duration(TimeSpan.FromMilliseconds(duration)))
        If EnablePowerEase Then animX.EasingFunction = pe

        Dim animY As New DoubleAnimation(toY, New Duration(TimeSpan.FromMilliseconds(duration)))
        If EnablePowerEase Then animY.EasingFunction = pe

        Dim newTranslateTransform As New TranslateTransform(fromX, fromY)
        _myControl.RenderTransform = newTranslateTransform

        newTranslateTransform.BeginAnimation(TranslateTransform.XProperty, animX)
        newTranslateTransform.BeginAnimation(TranslateTransform.YProperty, animY)
    End Sub

    Public Sub PropertyDoubleAnimation(dp As DependencyProperty, [to] As Double, Optional duration As Integer = 1000)
        Dim anim As New DoubleAnimation([to], New Duration(TimeSpan.FromMilliseconds(duration)))
        Dim pe As New PowerEase With {.EasingMode = EasingMode.EaseInOut, .Power = 10}
        anim.EasingFunction = pe
        _myControl.BeginAnimation(dp, anim)
    End Sub
#End Region


End Class
