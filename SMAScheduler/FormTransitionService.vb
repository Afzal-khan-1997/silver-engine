Public NotInheritable Class FormTransitionService
    Private Sub New()
    End Sub

    Public Shared Function ShowDialogWithMotion(owner As Form, child As Form) As DialogResult
        If owner Is Nothing OrElse child Is Nothing Then
            Return DialogResult.Cancel
        End If

        Dim ownerOriginalOpacity = owner.Opacity
        FadeForm(owner, 0.88R, 8)

        child.StartPosition = FormStartPosition.Manual
        Dim targetLocation = CenterOverOwner(owner, child)
        child.Location = New Point(targetLocation.X + 46, targetLocation.Y)
        child.Opacity = 0R

        AddHandler child.Shown, Sub()
                                    SlideFadeIn(child, targetLocation)
                                End Sub

        Dim result = child.ShowDialog(owner)
        FadeForm(owner, ownerOriginalOpacity, 8)
        Return result
    End Function

    Private Shared Function CenterOverOwner(owner As Form, child As Form) As Point
        Dim workingArea = Screen.FromControl(owner).WorkingArea
        Dim ownerBounds = owner.Bounds
        Dim x = ownerBounds.Left + ((ownerBounds.Width - child.Width) \ 2)
        Dim y = ownerBounds.Top + ((ownerBounds.Height - child.Height) \ 2)

        x = Math.Max(workingArea.Left, Math.Min(workingArea.Right - child.Width, x))
        y = Math.Max(workingArea.Top, Math.Min(workingArea.Bottom - child.Height, y))
        Return New Point(x, y)
    End Function

    Private Shared Sub SlideFadeIn(form As Form, targetLocation As Point)
        Dim startLocation = form.Location
        Dim frame = 0
        Dim totalFrames = 16
        Dim timer As New Timer With {.Interval = 14}

        AddHandler timer.Tick, Sub()
                                   If form.IsDisposed Then
                                       timer.Stop()
                                       timer.Dispose()
                                       Return
                                   End If

                                   frame += 1
                                   Dim progress = EaseOutCubic(frame / CDbl(totalFrames))
                                   form.Opacity = Math.Min(1R, progress)
                                   form.Location = New Point(
                                       CInt(startLocation.X + ((targetLocation.X - startLocation.X) * progress)),
                                       CInt(startLocation.Y + ((targetLocation.Y - startLocation.Y) * progress)))

                                   If frame >= totalFrames Then
                                       form.Opacity = 1R
                                       form.Location = targetLocation
                                       timer.Stop()
                                       timer.Dispose()
                                   End If
                               End Sub

        timer.Start()
    End Sub

    Private Shared Sub FadeForm(form As Form, targetOpacity As Double, frames As Integer)
        If form Is Nothing OrElse form.IsDisposed Then
            Return
        End If

        Dim startOpacity = form.Opacity
        For frame = 1 To Math.Max(1, frames)
            Dim progress = EaseOutCubic(frame / CDbl(Math.Max(1, frames)))
            form.Opacity = startOpacity + ((targetOpacity - startOpacity) * progress)
            form.Refresh()
            Application.DoEvents()
            Threading.Thread.Sleep(10)
        Next
        form.Opacity = targetOpacity
    End Sub

    Private Shared Function EaseOutCubic(value As Double) As Double
        Dim progress = Math.Max(0R, Math.Min(1R, value))
        Dim inverse = 1R - progress
        Return 1R - (inverse * inverse * inverse)
    End Function
End Class
