Imports TwinCAT
Imports TwinCAT.Ads
Imports J2i.Net.XInputWrapper
Imports System.ComponentModel

Public Class Form1


    Public WithEvents Timer_HealthCheck As New Timer ' Timer that updates the screen variables

    Public WithEvents Timer_SendDataToPLC As New Timer ' Timer that sends ADS data to the PLC.
    Public HeartBeatState As Boolean  ' Toggle helper for heartbeat.

    Public MyController As XboxController

    Declare Function SendMessage Lib "user32" Alias "SendMessageA" (ByVal hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        TabControl.SelectedTab = TabPage_Connection

        LoadSettings()

        Timer_HealthCheck.Interval = 250  ' 250ms update rate to screen.
        Timer_HealthCheck.Start()

        MyController = XboxController.RetrieveController(0)
        XboxController.StartPolling()

        If My.Settings.bAutoConnectADS Then
            ConnectToPLC()
        End If

    End Sub

    Private Sub LoadSettings()

        If Not My.Computer.Keyboard.ShiftKeyDown Then
            Me.Location = My.Settings.WindowLoc ' Restore window position from saved file.
        Else
            Me.Location = My.Computer.Screen.Bounds.Location  ' Optional factory reset if holding shift on startup
        End If

        Button_ADSDisconnect.Enabled = False

        SendMessage(ProgressBar_ADSGood.Handle, 1040, 1, 0)
        SendMessage(ProgressBar_ADSBad.Handle, 1040, 2, 0)

        TextBox_ADSConnectionStatus.Text = "Not Conntected to PLC"
        TextBox_ADSConnectionStatus2.Text = "Not Conntected to PLC"

        CheckBox_AutoConnectOnOpen.Checked = My.Settings.bAutoConnectADS
        TextBox_ADSNetID.Text = My.Settings.sPLC_NETID
        TextBox_ADSPort.Text = My.Settings.sPLC_PORT
        TextBox_TcJoyPath.Text = My.Settings.sTcJoyPath
        TextBox_ADSRate.Text = My.Settings.sADSRate
        TextBox_ADSWatchdog.Text = My.Settings.sADSWatchdog
        TextBox_ADSWatchdogDeadDuration.Text = My.Settings.sADSWatchdogDeadDuration
        TextBox_AnalogDeadzone.Text = My.Settings.sAnalogDeadZone
        TextBox_ShoulderDeadzone.Text = My.Settings.sShoulderDeadZone

        If TextBox_ADSNetID.Text = "" Then
            TextBox_ADSNetID.Text = "0.0.0.0.0.0"
        End If
        If TextBox_ADSPort.Text = "" Then
            TextBox_ADSPort.Text = "851"
        End If

        If TextBox_TcJoyPath.Text = "" Then
            TextBox_TcJoyPath.Text = "Global_Variables.TcJoy"
        End If

        If My.Settings.sADSRate = "" Then
            TextBox_ADSRate.Text = "50"
        End If

        If TextBox_ADSWatchdog.Text = "" Then
            TextBox_ADSWatchdog.Text = "100"
        End If

        If TextBox_ADSWatchdogDeadDuration.Text = "" Then
            TextBox_ADSWatchdogDeadDuration.Text = "2000"
        End If

        If TextBox_AnalogDeadzone.Text = "" Then
            TextBox_AnalogDeadzone.Text = "6000" ' counts of 32767
        End If

        If TextBox_ShoulderDeadzone.Text = "" Then
            TextBox_ShoulderDeadzone.Text = "0"  ' counts of 255
        End If

    End Sub

    ''' <summary>
    ''' System Health Check Slow Rate... Updates the gui with values from the controller, etc.  Runs slower than ADS cycle to preserve resources.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks>Ticks every 'My.Settings.iHeathCheckPulseMS_Slow' Milliseconds </remarks>
    Private Sub Timer_HealthCheck_Tick_Slow(sender As Object, e As EventArgs) Handles Timer_HealthCheck.Tick
        Try
            '       TextBox_ADSConnectionStatus.Text = IOPoller.ThreadStatus + " - " + IOPoller.PlcState + " - " + IOPoller.AmsRouterState
            '      TextBox_ADSConnectionStatus2.Text = IOPoller.ThreadStatus + " - " + IOPoller.PlcState + " - " + IOPoller.AmsRouterState
        Catch ex As Exception
            ' Do nothing on null ref.
        End Try


        If Not MyController Is Nothing Then

            If MyController.IsConnected Then
                Label_Controller0_Connection.Text = "      Controller 0 Connected     "
                Label_Controller0_Connection.BackColor = Color.LawnGreen
            Else
                Label_Controller0_Connection.Text = "  Controller 0 NOT CONNECTED!    "
                Label_Controller0_Connection.BackColor = Color.OrangeRed
            End If

            ' Update controller readout display
            If MyController.IsConnected Then

                If MyController.LeftThumbStick.X >= 0 Then
                    UpdateProgressBar(ProgressBar_LeftStickXPlus, Math.Abs(MyController.LeftThumbStick.X))
                    UpdateProgressBar(ProgressBar_LeftStickXMinus, 0)
                    Label_LeftStickXPos.Text = MyController.LeftThumbStick.X.ToString
                    Label_LeftStickXNeg.Text = ""
                Else
                    UpdateProgressBar(ProgressBar_LeftStickXPlus, 0)
                    UpdateProgressBar(ProgressBar_LeftStickXMinus, Math.Abs(MyController.LeftThumbStick.X))
                    Label_LeftStickXPos.Text = ""
                    Label_LeftStickXNeg.Text = MyController.LeftThumbStick.X.ToString
                End If
                If MyController.LeftThumbStick.Y >= 0 Then
                    UpdateProgressBar(ProgressBar_LeftStickYPlus, Math.Abs(MyController.LeftThumbStick.Y))
                    UpdateProgressBar(ProgressBar_LeftStickYMinus, 0)
                    Label_LeftStickYPos.Text = MyController.LeftThumbStick.Y.ToString
                    Label_LeftStickYNeg.Text = ""
                Else
                    UpdateProgressBar(ProgressBar_LeftStickYPlus, 0)
                    UpdateProgressBar(ProgressBar_LeftStickYMinus, Math.Abs(MyController.LeftThumbStick.Y))
                    Label_LeftStickYPos.Text = ""
                    Label_LeftStickYNeg.Text = MyController.LeftThumbStick.Y.ToString
                End If


                If MyController.RightThumbStick.X >= 0 Then
                    UpdateProgressBar(ProgressBar_RightStickXPlus, Math.Abs(MyController.RightThumbStick.X))
                    UpdateProgressBar(ProgressBar_RightStickXMinus, 0)
                    Label_RightStickXPos.Text = MyController.RightThumbStick.X.ToString
                    Label_RightStickXNeg.Text = ""
                Else
                    UpdateProgressBar(ProgressBar_RightStickXPlus, 0)
                    UpdateProgressBar(ProgressBar_RightStickXMinus, Math.Abs(MyController.RightThumbStick.X))
                    Label_RightStickXPos.Text = ""
                    Label_RightStickXNeg.Text = MyController.RightThumbStick.X.ToString
                End If

                If MyController.RightThumbStick.Y >= 0 Then
                    UpdateProgressBar(ProgressBar_RightStickYPlus, Math.Abs(MyController.RightThumbStick.Y))
                    UpdateProgressBar(ProgressBar_RightStickYMinus, 0)
                    Label_RightStickYPos.Text = MyController.RightThumbStick.Y.ToString
                    Label_RightStickYNeg.Text = ""
                Else
                    UpdateProgressBar(ProgressBar_RightStickYPlus, 0)
                    UpdateProgressBar(ProgressBar_RightStickYMinus, Math.Abs(MyController.RightThumbStick.Y))
                    Label_RightStickYPos.Text = ""
                    Label_RightStickYNeg.Text = MyController.RightThumbStick.Y.ToString
                End If


                CheckBox_AButton.Checked = MyController.IsAPressed
                CheckBox_BButton.Checked = MyController.IsBPressed
                CheckBox_XButton.Checked = MyController.IsXPressed
                CheckBox_YButton.Checked = MyController.IsYPressed


                CheckBox_DPadDown.Checked = MyController.IsDPadDownPressed
                CheckBox_DPadUp.Checked = MyController.IsDPadUpPressed
                CheckBox_DPadLeft.Checked = MyController.IsDPadLeftPressed
                CheckBox_DPadRight.Checked = MyController.IsDPadRightPressed

                CheckBox_LeftShoulderBtn.Checked = MyController.IsLeftShoulderPressed
                CheckBox_RightShoulderBtn.Checked = MyController.IsRightShoulderPressed

                CheckBox_BackBtn.Checked = MyController.IsBackPressed
                CheckBox_StartBtn.Checked = MyController.IsStartPressed

                UpdateProgressBar(ProgressBar_LeftShoulderAnalog, MyController.LeftTrigger)
                Label_LeftShoulderVal.Text = MyController.LeftTrigger.ToString
                UpdateProgressBar(ProgressBar_RightShoulderAnalog, MyController.RightTrigger)
                Label_RightShoulderVal.Text = MyController.RightTrigger.ToString

                Select Case MyController.BatteryInformationGamepad.BatteryLevel

                    Case BatteryLevel.BATTERY_LEVEL_EMPTY

                        ProgressBar_Controller0Battery.Value = 0
                        SendMessage(ProgressBar_Controller0Battery.Handle, 1040, 2, 0) ' Turn Red

                    Case BatteryLevel.BATTERY_LEVEL_LOW

                        ProgressBar_Controller0Battery.Value = 25
                        SendMessage(ProgressBar_Controller0Battery.Handle, 1040, 2, 0) ' Turn Red

                    Case BatteryLevel.BATTERY_LEVEL_MEDIUM

                        ProgressBar_Controller0Battery.Value = 50
                        SendMessage(ProgressBar_Controller0Battery.Handle, 1040, 1, 0) ' Turn Green

                    Case BatteryLevel.BATTERY_LEVEL_FULL

                        ProgressBar_Controller0Battery.Value = 100
                        SendMessage(ProgressBar_Controller0Battery.Handle, 1040, 1, 0) ' Turn Green

                    Case Else

                        Console.WriteLine("Battery: " + "Error: " + MyController.BatteryInformationGamepad.BatteryLevel.ToString)
                        ProgressBar_Controller0Battery.Value = 100
                        SendMessage(ProgressBar_Controller0Battery.Handle, 1040, 2, 0) ' Turn Red

                End Select

            End If

        End If

    End Sub

    ' Hack a faster response from dumb animated progress bars.
    Public Sub UpdateProgressBar(ByRef Bar As ProgressBar, ByVal value As Integer)
        If value = Bar.Maximum Then
            Bar.Value = value
            Bar.Value = value - 1
            Bar.Value = value
        Else
            Bar.Value = value + 1
            Bar.Value = value
        End If
    End Sub

    Private Sub Timer_SendDataToPLC_Tick(sender As Object, e As EventArgs) Handles Timer_SendDataToPLC.Tick

        HeartBeatState = Not HeartBeatState ' Invert heartbeat before send.

        Try

            'IOPoller.PlcBoolValue(TextBox_TcJoyPath.Text + ".bControllerConnected") = MyController.IsConnected

            ' Write values to object that gets sent to the PLC

            For Each Tag As DataTag In PLC_IO_Polling.BgTaskData.TagList

                Select Case Tag.TagName

                    Case TextBox_TcJoyPath.Text + ".bControllerConnected"
                        Tag.Value = MyController.IsConnected

                End Select

            Next

            ' Controller Data to the PLC. (From the PLC happens in the PLC_Polling.vb File)
            If MyController.IsConnected Then

                For Each Tag As DataTag In PLC_IO_Polling.BgTaskData.TagList

                    Select Case Tag.TagName

                        Case TextBox_TcJoyPath.Text + ".Start_Button"
                            Tag.Value = MyController.IsStartPressed

                        Case TextBox_TcJoyPath.Text + ".Back_Button"
                            Tag.Value = MyController.IsBackPressed

                        Case TextBox_TcJoyPath.Text + ".A_Button"
                            Tag.Value = MyController.IsAPressed

                        Case TextBox_TcJoyPath.Text + ".B_Button"
                            Tag.Value = MyController.IsBPressed

                        Case TextBox_TcJoyPath.Text + ".X_Button"
                            Tag.Value = MyController.IsXPressed

                        Case TextBox_TcJoyPath.Text + ".Y_Button"
                            Tag.Value = MyController.IsYPressed

                        Case TextBox_TcJoyPath.Text + ".LeftShoulder_Button"
                            Tag.Value = MyController.IsLeftShoulderPressed

                        Case TextBox_TcJoyPath.Text + ".RightShoulder_Button"
                            Tag.Value = MyController.IsRightShoulderPressed

                        Case TextBox_TcJoyPath.Text + ".LeftStick_Button"
                            Tag.Value = MyController.IsLeftStickPressed

                        Case TextBox_TcJoyPath.Text + ".RightStick_Button"
                            Tag.Value = MyController.IsRightStickPressed

                        Case TextBox_TcJoyPath.Text + ".DPad_Up_Button"
                            Tag.Value = MyController.IsDPadUpPressed

                        Case TextBox_TcJoyPath.Text + ".DPad_Left_Button"
                            Tag.Value = MyController.IsDPadLeftPressed

                        Case TextBox_TcJoyPath.Text + ".DPad_Right_Button"
                            Tag.Value = MyController.IsDPadRightPressed

                        Case TextBox_TcJoyPath.Text + ".DPad_Down_Button"
                            Tag.Value = MyController.IsDPadDownPressed

                        Case TextBox_TcJoyPath.Text + ".iLeftTrigger_Axis"
                            If Math.Abs(MyController.LeftTrigger) > CInt(TextBox_ShoulderDeadzone.Text) Then
                                Tag.Value = MyController.LeftTrigger
                            Else
                                Tag.Value = 0
                            End If

                        Case TextBox_TcJoyPath.Text + ".iRightTrigger_Axis"
                            If Math.Abs(MyController.RightTrigger) > CInt(TextBox_ShoulderDeadzone.Text) Then
                                Tag.Value = MyController.RightTrigger
                            Else
                                Tag.Value = 0
                            End If

                        Case TextBox_TcJoyPath.Text + ".iLeftStick_X_Axis"
                            If Math.Abs(MyController.LeftThumbStick.X) > CInt(TextBox_AnalogDeadzone.Text) Then
                                Tag.Value = MyController.LeftThumbStick.X
                            Else
                                Tag.Value = 0
                            End If

                        Case TextBox_TcJoyPath.Text + ".iLeftStick_Y_Axis"
                            If Math.Abs(MyController.LeftThumbStick.Y) > CInt(TextBox_AnalogDeadzone.Text) Then
                                Tag.Value = MyController.LeftThumbStick.Y
                            Else
                                Tag.Value = 0
                            End If

                        Case TextBox_TcJoyPath.Text + ".iRightStick_X_Axis"
                            If Math.Abs(MyController.LeftThumbStick.X) > CInt(TextBox_AnalogDeadzone.Text) Then
                                Tag.Value = MyController.LeftThumbStick.X
                            Else
                                Tag.Value = 0
                            End If

                        Case TextBox_TcJoyPath.Text + ".iRightStick_Y_Axis"
                            If Math.Abs(MyController.RightThumbStick.Y) > CInt(TextBox_AnalogDeadzone.Text) Then
                                Tag.Value = MyController.RightThumbStick.Y
                            Else
                                Tag.Value = 0
                            End If

                        Case TextBox_TcJoyPath.Text + ".sBatteryInfo"

                            MyController.UpdateBatteryState()

                            Select Case MyController.BatteryInformationGamepad.BatteryLevel

                                Case BatteryLevel.BATTERY_LEVEL_EMPTY

                                    Tag.Value = "0"

                                Case BatteryLevel.BATTERY_LEVEL_LOW

                                    Tag.Value = "25"

                                Case BatteryLevel.BATTERY_LEVEL_MEDIUM

                                    Tag.Value = "50"

                                Case BatteryLevel.BATTERY_LEVEL_FULL

                                    Tag.Value = "100"

                                Case Else

                                    Tag.Value = "unkn"

                            End Select

                        Case TextBox_TcJoyPath.Text + ".bHeartBeatToggle"
                            Tag.Value = HeartBeatState

                    End Select

                Next





            End If

        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try

        PLC_IO_Polling.BackgroundWorker1.RunWorkerAsync(BgTaskData)

    End Sub


#Region "Connection Tab"

    Private Sub CheckBox_AutoConnectOnOpen_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_AutoConnectOnOpen.CheckedChanged
        My.Settings.bAutoConnectADS = CheckBox_AutoConnectOnOpen.Checked
        My.Settings.Save()
    End Sub

    Private Sub TextBox_ADSNetID_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSNetID.TextChanged
        My.Settings.sPLC_NETID = TextBox_ADSNetID.Text
        My.Settings.Save()
    End Sub

    Private Sub TextBox_ADSPort_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSPort.TextChanged
        If CInt(TextBox_ADSPort.Text) < 1 Then
            TextBox_ADSPort.Text = "851"
        End If
        My.Settings.sPLC_PORT = TextBox_ADSPort.Text
        My.Settings.Save()
    End Sub

    Private Sub ConnectToPLC()

        ' Disable buttons/input while connected.

        TextBox_TcJoyFunctionBlockStatus.Enabled = False
        TextBox_ADSPort.Enabled = False
        TextBox_ADSNetID.Enabled = False
        TextBox_ADSRate.Enabled = False
        Button_ADSConnect.Enabled = False

        PLC_IO_Polling.StartWorker()

        ' Dwell to let port open
        Threading.Thread.Sleep(500)

        '   If IOPoller.CheckTagPresent(TextBox_TcJoyPath.Text + ".bHeartBeatToggle") Then
        '   TextBox_TcJoyFunctionBlockStatus.Text = "FOUND IT!"
        '   TextBox_TcJoyFunctionBlockStatus.ForeColor = Color.Black
        '   TextBox_TcJoyFunctionBlockStatus.BackColor = Color.LawnGreen
        '   Else
        '   TextBox_TcJoyFunctionBlockStatus.Text = "NOT FOUND! CHECK INSTANCE PATH."
        '   TextBox_TcJoyFunctionBlockStatus.ForeColor = Color.Black
        '   TextBox_TcJoyFunctionBlockStatus.BackColor = Color.OrangeRed
        '   TextBox_TcJoyPath.Focus()
        '   End If

        ' Send Settings to PLC FB:
        ' IOPoller.PlcIntValue(TextBox_TcJoyPath.Text + ".iADSWatchdogMS") = CInt(TextBox_ADSWatchdog.Text)
        ' IOPoller.PlcIntValue(TextBox_TcJoyPath.Text + ".iADSWatchdogDeadDurationMS") = CInt(TextBox_ADSWatchdogDeadDuration.Text)

        ' Start sending data using this timer's tick function.
        Timer_SendDataToPLC.Interval = CInt(TextBox_ADSRate.Text)
        Timer_SendDataToPLC.Start()

        Button_ADSDisconnect.Enabled = True


    End Sub

    Private Sub Button_ADSConnect_Click(sender As Object, e As EventArgs) Handles Button_ADSConnect.Click
        ConnectToPLC()
    End Sub


    Private Sub TextBox_TcJoyPath_TextChanged(sender As Object, e As EventArgs) Handles TextBox_TcJoyPath.TextChanged
        My.Settings.sTcJoyPath = TextBox_TcJoyPath.Text
        My.Settings.Save()
    End Sub

    Private Sub TextBox_ADSRate_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSRate.TextChanged
        If CInt(TextBox_ADSRate.Text) < 1 Then
            TextBox_ADSRate.Text = "1"
        End If
        My.Settings.sADSRate = TextBox_ADSRate.Text
        My.Settings.Save()
    End Sub

    Private Sub Button_ADSDisconnect_Click(sender As Object, e As EventArgs) Handles Button_ADSDisconnect.Click


        TextBox_TcJoyFunctionBlockStatus.Enabled = False
        TextBox_ADSPort.Enabled = False
        TextBox_ADSNetID.Enabled = False
        TextBox_ADSRate.Enabled = False
        Button_ADSConnect.Enabled = False

        Button_ADSConnect.Enabled = True

    End Sub

    Private Sub TextBox_ADSWatchdog_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSWatchdog.TextChanged
        If TextBox_ADSWatchdog.Text <> "" Then
            If CInt(TextBox_ADSWatchdog.Text) < 1 Then
                TextBox_ADSWatchdog.Text = "1"
            End If
            My.Settings.sADSWatchdog = TextBox_ADSWatchdog.Text
            My.Settings.Save()
            ' Send to poller
            'For Each Tag As DataTag In PLC_IO_Polling.BgTaskData.TagList
            ' If Tag.TagName = TextBox_TcJoyPath.Text + ".iADSWatchdogMS" Then
            ' Tag.Value = CInt(TextBox_ADSWatchdog.Text) ' TODO: Better validation than this.
            ' End If
            '     Next
        End If
    End Sub

    Private Sub TextBox_ADSWatchdogDeadDuration_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSWatchdogDeadDuration.TextChanged
        If TextBox_ADSWatchdogDeadDuration.Text <> "" Then
            If CInt(TextBox_ADSWatchdogDeadDuration.Text) < 1 Then
                TextBox_ADSWatchdogDeadDuration.Text = "1"
            End If
            My.Settings.sADSWatchdogDeadDuration = TextBox_ADSWatchdogDeadDuration.Text
            My.Settings.Save()
        End If
    End Sub

    Private Sub TextBox_AnalogDeadzone_TextChanged(sender As Object, e As EventArgs) Handles TextBox_AnalogDeadzone.TextChanged
        If CInt(TextBox_AnalogDeadzone.Text) < 0 Then
            TextBox_AnalogDeadzone.Text = "0"
        End If
        My.Settings.sAnalogDeadZone = TextBox_AnalogDeadzone.Text
        My.Settings.Save()
    End Sub

    Private Sub TextBox_ShoulderDeadzone_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ShoulderDeadzone.TextChanged
        If CInt(TextBox_ShoulderDeadzone.Text) < 0 Then
            TextBox_ShoulderDeadzone.Text = "0"
        End If
        My.Settings.sShoulderDeadZone = TextBox_ShoulderDeadzone.Text
        My.Settings.Save()
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Try
            PLC_IO_Polling.BackgroundWorker1.CancelAsync()  ' TODO: This doesn't close out properly
        Catch ex As Exception
            '
        End Try

        My.Settings.WindowLoc = Me.Location
        My.Settings.Save()
    End Sub


#End Region



End Class
