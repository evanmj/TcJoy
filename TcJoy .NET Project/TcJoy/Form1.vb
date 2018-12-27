Imports TwinCAT
Imports TwinCAT.Ads
Imports J2i.Net.XInputWrapper
Imports System.ComponentModel
Imports System

'
' TcJoy, by Evan Jensen of Jensen Mecharonics, LLC. 2018 
'
' See the github page for more information: https://github.com/evanmj/TcJoy
'
' TODO: Any todos in code
' TODO: Fix help tab, add help text/btns on plc settings
' TODO: Add donate link and possibly 'nag' screen
' TODO: Make howto video
' 
'
' MIT License

' Copyright(c) 2018 Evan Jensen of Jensen Mecharonics, LLC.

' Permission Is hereby granted, free Of charge, to any person obtaining a copy
' of this software And associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, And/Or sell
' copies of the Software, And to permit persons to whom the Software Is
' furnished to do so, subject to the following conditions:

' The above copyright notice And this permission notice shall be included In all
' copies Or substantial portions of the Software.

' THE SOFTWARE Is PROVIDED "AS IS", WITHOUT WARRANTY Of ANY KIND, EXPRESS Or
' IMPLIED, INCLUDING BUT Not LIMITED To THE WARRANTIES Of MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE And NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS Or COPYRIGHT HOLDERS BE LIABLE For ANY CLAIM, DAMAGES Or OTHER
' LIABILITY, WHETHER In AN ACTION Of CONTRACT, TORT Or OTHERWISE, ARISING FROM,
' OUT OF Or IN CONNECTION WITH THE SOFTWARE Or THE USE Or OTHER DEALINGS IN THE
' SOFTWARE.
'

Public Class Form1

    Public BgTaskData As TaskData ' Data for background task that runs actual ADS comms.

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

        BgTaskData = New TaskData(0, 0, 0, "", 851)

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
            If Not BgTaskData Is Nothing Then
                If BgTaskData.ADS_Connection Is Nothing Then
                    TextBox_ADSConnectionStatus.Text = "Not Connected Yet."
                    TextBox_ADSConnectionStatus2.Text = TextBox_ADSConnectionStatus.Text
                ElseIf BgTaskData.ADS_Connection.AdsClient.IsConnected Then
                    TextBox_ADSConnectionStatus.Text = "Connected."
                    TextBox_ADSConnectionStatus2.Text = TextBox_ADSConnectionStatus.Text
                Else
                    TextBox_ADSConnectionStatus.Text = "Not Connected."
                    TextBox_ADSConnectionStatus2.Text = TextBox_ADSConnectionStatus.Text
                End If
            End If


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

                ' Color thumbstick sliders
                If Math.Abs(MyController.LeftThumbStick.X) < CInt(TextBox_AnalogDeadzone.Text) Then
                    SendMessage(ProgressBar_LeftStickXPlus.Handle, 1040, 2, 0) ' Turn Red
                    SendMessage(ProgressBar_LeftStickXMinus.Handle, 1040, 2, 0) ' Turn Red
                Else
                    SendMessage(ProgressBar_LeftStickXPlus.Handle, 1040, 1, 0) ' Turn Green
                    SendMessage(ProgressBar_LeftStickXMinus.Handle, 1040, 1, 0) ' Turn Green
                End If
                If Math.Abs(MyController.LeftThumbStick.Y) < CInt(TextBox_AnalogDeadzone.Text) Then
                    SendMessage(ProgressBar_LeftStickYPlus.Handle, 1040, 2, 0) ' Turn Red
                    SendMessage(ProgressBar_LeftStickYMinus.Handle, 1040, 2, 0) ' Turn Red
                Else
                    SendMessage(ProgressBar_LeftStickYPlus.Handle, 1040, 1, 0) ' Turn Green
                    SendMessage(ProgressBar_LeftStickYMinus.Handle, 1040, 1, 0) ' Turn Green
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

                ' Color thumbstick sliders
                If Math.Abs(MyController.RightThumbStick.X) < CInt(TextBox_AnalogDeadzone.Text) Then
                    SendMessage(ProgressBar_RightStickXPlus.Handle, 1040, 2, 0) ' Turn Red
                    SendMessage(ProgressBar_RightStickXMinus.Handle, 1040, 2, 0) ' Turn Red
                Else
                    SendMessage(ProgressBar_RightStickXPlus.Handle, 1040, 1, 0) ' Turn Green
                    SendMessage(ProgressBar_RightStickXMinus.Handle, 1040, 1, 0) ' Turn Green
                End If
                If Math.Abs(MyController.RightThumbStick.Y) < CInt(TextBox_AnalogDeadzone.Text) Then
                    SendMessage(ProgressBar_RightStickYPlus.Handle, 1040, 2, 0) ' Turn Red
                    SendMessage(ProgressBar_RightStickYMinus.Handle, 1040, 2, 0) ' Turn Red
                Else
                    SendMessage(ProgressBar_RightStickYPlus.Handle, 1040, 1, 0) ' Turn Green
                    SendMessage(ProgressBar_RightStickYMinus.Handle, 1040, 1, 0) ' Turn Green
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

                ' Color shoulder sliders
                If MyController.LeftTrigger < CInt(TextBox_ShoulderDeadzone.Text) Then
                    SendMessage(ProgressBar_LeftShoulderAnalog.Handle, 1040, 2, 0) ' Turn Red
                Else
                    SendMessage(ProgressBar_LeftShoulderAnalog.Handle, 1040, 1, 0) ' Turn Green
                End If
                If MyController.RightTrigger < CInt(TextBox_ShoulderDeadzone.Text) Then
                    SendMessage(ProgressBar_RightShoulderAnalog.Handle, 1040, 2, 0) ' Turn Red
                Else
                    SendMessage(ProgressBar_RightShoulderAnalog.Handle, 1040, 1, 0) ' Turn Green
                End If

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

        ' Do Reads at slow speed, we don't need really any of this for actual functionality, but it is nice for status.
        If Not BgTaskData Is Nothing Then
            If Not BgTaskData.ADS_Connection Is Nothing Then
                If BgTaskData.ADS_Connection.AdsClient.IsConnected Then

                    Dim symbol As ITcAdsSymbol
                    Dim debug = False
                    For Each tag As Form1.DataTag In BgTaskData.TagList

                        If tag.IsRead Then

                            Try
                                ' Get data type and other information from PLC
                                symbol = BgTaskData.ADS_Connection.AdsClient.ReadSymbolInfo(tag.TagName)

                                tag.Value = BgTaskData.ADS_Connection.AdsClient.ReadSymbol(symbol)
                                If debug Then Console.WriteLine(tag.TagName + " value = " + tag.Value.ToString)
                                BgTaskData.IsConnected = BgTaskData.ADS_Connection.AdsClient.IsConnected

                            Catch ex As Exception
                                Console.WriteLine("Could not get symbol or write tag: " + tag.TagName + " ex: " + ex.Message)
                                BgTaskData.IsConnected = False
                                BackgroundWorker1.CancelAsync()
                            End Try

                        End If

                        ' Do stuff with our new found tag reads... 
                        Select Case tag.TagName

                            Case TextBox_TcJoyPath.Text + ".bIsActive"
                                If tag.Value Then
                                    Label_JoyActiveFromPLC.BackColor = Color.LawnGreen
                                    Label_JoyActiveFromPLC.Text = "Latency Status"
                                Else
                                    If MyController.IsConnected Then
                                        Label_JoyActiveFromPLC.BackColor = Color.OrangeRed
                                        Label_JoyActiveFromPLC.Text = "Latency Status"
                                    Else
                                        Label_JoyActiveFromPLC.BackColor = Color.OrangeRed
                                        Label_JoyActiveFromPLC.Text = "No Controller!"
                                    End If

                                End If

                            Case TextBox_TcJoyPath.Text + ".iUpdateRateMS"

                                ' Fill out latency status bar graphs with achieved rate.
                                If tag.Value > 0 Then

                                    TextBox_ADSDataRateAchieved.Text = tag.Value.ToString + " ms"

                                    Dim val = (Math.Abs(tag.Value - BgTaskData.UpdateRateMS) / BgTaskData.ADSWatchdogMs) * 100
                                    If val < 0 Then val = 0
                                    If val > 100 Then val = 100

                                    If tag.Value <= BgTaskData.UpdateRateMS Then
                                        ProgressBar_ADSGood.Value = val
                                        ProgressBar_ADSBad.Value = 0
                                    Else
                                        ProgressBar_ADSGood.Value = 0
                                        ProgressBar_ADSBad.Value = val
                                    End If
                                Else
                                    ProgressBar_ADSGood.Value = 0
                                    ProgressBar_ADSBad.Value = 0
                                    TextBox_ADSDataRateAchieved.Text = ""
                                End If

                        End Select
                    Next
                End If
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
            ' Controller Data to the PLC. (From the PLC happens in the PLC_Polling.vb File)
            For Each Tag As DataTag In BgTaskData.TagList

                Select Case Tag.TagName

                    Case TextBox_TcJoyPath.Text + ".bControllerConnected"
                        Tag.Value = MyController.IsConnected

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

                    Case TextBox_TcJoyPath.Text + ".iADSWatchdogMS"
                        Tag.Value = BgTaskData.ADSWatchdogMs

                    Case TextBox_TcJoyPath.Text + ".iADSWatchdogDeadDurationMS"
                        Tag.Value = BgTaskData.ADSWatchdogDeadDurationMS

                End Select

            Next

        Catch ex As Exception
            MessageBox.Show(ex.Message + " Disconnecting from PLC.")
            DisconnectFromPLC()
        End Try

        ' Fire off dude that goes and applies the changes to the code.
        PLC_IO_Polling.StartWorker()

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
        If TextBox_ADSPort.Text <> "" Then
            Dim value As Integer
            Try
                value = Int32.Parse(TextBox_ADSPort.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the port you put in, it must be an integer.  Defaulting to 851 for TC3 Runtime 1")
                TextBox_ADSPort.Text = "851"
                value = 851
            End Try
            If value < 1 Then
                TextBox_ADSPort.Text = "851"
            End If
            My.Settings.sPLC_PORT = TextBox_ADSPort.Text
            My.Settings.Save()
        End If

    End Sub

    Private Sub ConnectToPLC()

        ' Disable buttons/input while connected.

        If BgTaskData.ADS_Connection Is Nothing Then

            ' Validate inputs
            Dim rate As Integer
            Try
                rate = Int32.Parse(TextBox_ADSRate.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the rate you put in, it must be an integer.  Defaulting to 100ms")
                TextBox_ADSPort.Text = "100"
                rate = 100
            End Try

            Dim port As Integer
            Try
                port = Int32.Parse(TextBox_ADSPort.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the port you put in, it must be an integer.  Defaulting to 851 for TC3 Runtime 1")
                TextBox_ADSPort.Text = "851"
                port = 851
            End Try

            Dim ADSWatchdog As Integer
            Try
                ADSWatchdog = Int32.Parse(TextBox_ADSWatchdog.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the ADS Watchdog you put in, it must be an integer.  Defaulting to 200ms")
                TextBox_ADSWatchdog.Text = "200"
                ADSWatchdog = 200
            End Try

            Dim ADSWatchdogDeadDuration As Integer
            Try
                ADSWatchdogDeadDuration = Int32.Parse(TextBox_ADSWatchdogDeadDuration.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the ADS Watchdog Dead Duration you put in, it must be an integer.  Defaulting to 2000ms")
                TextBox_ADSWatchdogDeadDuration.Text = "2000"
                ADSWatchdogDeadDuration = 2000
            End Try

            BgTaskData = New Form1.TaskData(rate, ADSWatchdog, ADSWatchdogDeadDuration,
                                  TextBox_ADSNetID.Text, port)

            Try
                BgTaskData.ADS_Connection = New Ads_Link(BgTaskData.NetID, BgTaskData.Port) ' Connect to local twincat. 
            Catch Ex As System.Exception
                Console.WriteLine("StartPoller():" + Ex.Message)
            End Try

            ' Dwell to let port open
            Threading.Thread.Sleep(500)

            ' See if we can find the FB on the PLC.
            Dim testtag As ITcAdsSymbol
            Try
                testtag = BgTaskData.ADS_Connection.AdsClient.ReadSymbolInfo(TextBox_TcJoyPath.Text + ".bHeartBeatToggle")
            Catch ex As Exception
                Console.WriteLine("Exception looking for fb instance.")
                testtag = Nothing
            End Try

            If testtag Is Nothing Then
                TextBox_TcJoyFunctionBlockStatus.Text = "NOT FOUND! CHECK INSTANCE PATH."
                TextBox_TcJoyFunctionBlockStatus.ForeColor = Color.Black
                TextBox_TcJoyFunctionBlockStatus.BackColor = Color.OrangeRed
                TextBox_TcJoyPath.Focus()

                ' Kill ads object.
                BgTaskData.ADS_Connection.AdsClient.Dispose()
                BgTaskData.ADS_Connection = Nothing
            Else
                TextBox_TcJoyFunctionBlockStatus.Text = "FOUND IT!"
                TextBox_TcJoyFunctionBlockStatus.ForeColor = Color.Black
                TextBox_TcJoyFunctionBlockStatus.BackColor = Color.LawnGreen

                TextBox_TcJoyFunctionBlockStatus.Enabled = False
                TextBox_ADSPort.Enabled = False
                TextBox_ADSNetID.Enabled = False
                TextBox_ADSRate.Enabled = False
                Button_ADSConnect.Enabled = False
                Button_ADSDisconnect.Enabled = True

                ' Build IO List
                AddVariablesToList(BgTaskData)

                ' Start sending data using this timer's tick function.
                Timer_SendDataToPLC.Interval = CInt(TextBox_ADSRate.Text)
                Timer_SendDataToPLC.Start()

                Button_ADSDisconnect.Enabled = True


            End If

        End If

    End Sub

    Private Sub Button_ADSConnect_Click(sender As Object, e As EventArgs) Handles Button_ADSConnect.Click
        ConnectToPLC()
    End Sub

    Private Sub TextBox_TcJoyPath_TextChanged(sender As Object, e As EventArgs) Handles TextBox_TcJoyPath.TextChanged
        My.Settings.sTcJoyPath = TextBox_TcJoyPath.Text
        My.Settings.Save()
    End Sub

    Private Sub TextBox_ADSRate_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSRate.TextChanged
        If TextBox_ADSRate.Text <> "" Then
            Dim value As Integer
            Try
                value = Int32.Parse(TextBox_ADSRate.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the value you put in, it must be an integer. Setting a default value.")
                TextBox_ADSRate.Text = "100"
                value = 100
            End Try
            If value < 1 Then
                TextBox_ADSRate.Text = "1"
            End If
            My.Settings.sADSRate = TextBox_ADSRate.Text
            My.Settings.Save()
        End If

    End Sub

    Private Sub DisconnectFromPLC()
        TextBox_TcJoyFunctionBlockStatus.Enabled = False
        TextBox_ADSPort.Enabled = False
        TextBox_ADSNetID.Enabled = False
        TextBox_ADSRate.Enabled = False
        Button_ADSDisconnect.Enabled = False
        Button_ADSConnect.Enabled = True
        Label_JoyActiveFromPLC.BackColor = Color.LightGray
        Label_JoyActiveFromPLC.Text = "Latency Status"
        ProgressBar_ADSBad.Value = 0
        ProgressBar_ADSGood.Value = 0

        Timer_SendDataToPLC.Stop()
        Threading.Thread.Sleep(100)

        ' Kill ads object.
        If Not BgTaskData.ADS_Connection Is Nothing Then
            BgTaskData.ADS_Connection.AdsClient.Dispose()
            BgTaskData.ADS_Connection = Nothing
        End If
    End Sub

    Private Sub Button_ADSDisconnect_Click(sender As Object, e As EventArgs) Handles Button_ADSDisconnect.Click
        DisconnectFromPLC()
    End Sub

    Private Sub TextBox_ADSWatchdog_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSWatchdog.TextChanged
        If TextBox_ADSWatchdog.Text <> "" Then
            Dim value As Integer
            Try
                value = Int32.Parse(TextBox_ADSWatchdog.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the value you put in, it must be an integer. Setting a default value.")
                TextBox_ADSWatchdog.Text = "200"
                value = 200
            End Try
            If value < 1 Then
                TextBox_ADSWatchdog.Text = "1"
            End If
            ' set in object so plc will get it.
            If Not BgTaskData Is Nothing Then
                BgTaskData.ADSWatchdogMs = value
            End If
            My.Settings.sADSWatchdog = TextBox_ADSWatchdog.Text
            My.Settings.Save()
        End If
    End Sub

    Private Sub TextBox_ADSWatchdogDeadDuration_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ADSWatchdogDeadDuration.TextChanged
        If TextBox_ADSWatchdogDeadDuration.Text <> "" Then
            Dim value As Integer
            Try
                value = Int32.Parse(TextBox_ADSWatchdogDeadDuration.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the value you put in, it must be an integer. Setting a default value.")
                TextBox_ADSWatchdogDeadDuration.Text = "2000"
                value = 2000
            End Try
            If value < 1 Then
                TextBox_ADSWatchdogDeadDuration.Text = "1"
            End If
            ' set in object so plc will get it.
            If Not BgTaskData Is Nothing Then
                BgTaskData.ADSWatchdogDeadDurationMS = value
            End If
            My.Settings.sADSWatchdogDeadDuration = TextBox_ADSWatchdogDeadDuration.Text
            My.Settings.Save()
        End If
    End Sub

    Private Sub TextBox_AnalogDeadzone_TextChanged(sender As Object, e As EventArgs) Handles TextBox_AnalogDeadzone.TextChanged
        If TextBox_AnalogDeadzone.Text <> "" Then
            Dim value As Integer
            Try
                value = Int32.Parse(TextBox_AnalogDeadzone.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the value you put in, it must be an integer. Setting a default value.")
                TextBox_AnalogDeadzone.Text = "6000"
                value = 6000
            End Try
            If value < 0 Then
                TextBox_AnalogDeadzone.Text = "6000"
            End If
            My.Settings.sAnalogDeadZone = TextBox_AnalogDeadzone.Text
            My.Settings.Save()
        End If

    End Sub

    Private Sub TextBox_ShoulderDeadzone_TextChanged(sender As Object, e As EventArgs) Handles TextBox_ShoulderDeadzone.TextChanged
        If TextBox_ShoulderDeadzone.Text <> "" Then
            Dim value As Integer
            Try
                value = Int32.Parse(TextBox_ShoulderDeadzone.Text)
            Catch ex As Exception
                MessageBox.Show("Didn't understand the value you put in, it must be an integer. Setting a default value.")
                TextBox_ShoulderDeadzone.Text = "0"
                value = 0
            End Try
            If value < 0 Then
                TextBox_ShoulderDeadzone.Text = "0"
            End If
            My.Settings.sShoulderDeadZone = TextBox_ShoulderDeadzone.Text
            My.Settings.Save()
        End If

    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.WindowLoc = Me.Location
        My.Settings.Save()
        Application.Exit()
        End
    End Sub


#End Region

    Public Class TaskData

        Private _TagList As List(Of DataTag)
        Private _UpdateRateMS As Integer
        Private _ADSWatchdogMs As Integer
        Private _ADSWatchdogDeadDurationMS As Integer
        Private _NetID As String
        Private _Port As Integer
        Private _IsConnected As Boolean
        Private _ADS_Connection As Ads_Link


        Public Sub New(UpdateRateMs As Integer, ADSWatchdogMs As Integer, ADSWatchdogDeadDurationMS As Integer, NetID As String, Port As Integer)
            _TagList = New List(Of DataTag)
            _UpdateRateMS = UpdateRateMs
            _ADSWatchdogMs = ADSWatchdogMs
            _ADSWatchdogDeadDurationMS = ADSWatchdogDeadDurationMS
            _NetID = NetID
            _Port = Port
        End Sub

        Property TagList As List(Of DataTag)
            Get
                Return _TagList
            End Get
            Set(value As List(Of DataTag))
                _TagList = value
            End Set
        End Property

        Property UpdateRateMS As Integer
            Get
                Return _UpdateRateMS
            End Get
            Set(value As Integer)
                _UpdateRateMS = value
            End Set
        End Property

        Property ADSWatchdogMs As Integer
            Get
                Return _ADSWatchdogMs
            End Get
            Set(value As Integer)
                _ADSWatchdogMs = value
            End Set
        End Property

        Property ADSWatchdogDeadDurationMS As Integer
            Get
                Return _ADSWatchdogDeadDurationMS
            End Get
            Set(value As Integer)
                _ADSWatchdogDeadDurationMS = value
            End Set
        End Property

        Property NetID As String
            Get
                Return _NetID
            End Get
            Set(value As String)
                _NetID = value
            End Set
        End Property

        Property Port As Integer
            Get
                Return _Port
            End Get
            Set(value As Integer)
                _Port = value
            End Set
        End Property

        Property IsConnected As Boolean
            Get
                Return _IsConnected
            End Get
            Set(value As Boolean)
                _IsConnected = value
            End Set
        End Property

        Property ADS_Connection As Ads_Link
            Get
                Return _ADS_Connection
            End Get
            Set(value As Ads_Link)
                _ADS_Connection = value
            End Set
        End Property

    End Class

    ''' <summary>
    ''' Data Tag Object that we read/write to or from twincat via ADS
    ''' </summary>
    Public Class DataTag

        Private _TagName As String
        Private _TagType As Object
        Private _IsRead As Boolean
        Private _IsWritten As Boolean
        Private _value As Object

        Public Sub New(TagName As String, TagType As Object, IsRead As Boolean, IsWritten As Boolean)
            _TagName = TagName
            _TagType = TagType
            _IsRead = IsRead
            _IsWritten = IsWritten
            _value = New Object
            _value = 0 ' Set default value of this object to '0'
        End Sub

        Property TagName As String
            Get
                Return _TagName
            End Get
            Set(value As String)
                _TagName = value
            End Set
        End Property

        Property TagType As Object
            Get
                Return _TagType
            End Get
            Set(value As Object)
                _TagType = value
            End Set
        End Property

        Property IsRead As Boolean
            Get
                Return _IsRead
            End Get
            Set(value As Boolean)
                _IsRead = value
            End Set
        End Property

        Property IsWritten As Boolean
            Get
                Return _IsWritten
            End Get
            Set(value As Boolean)
                _IsWritten = value
            End Set
        End Property

        Property Value As Object
            Get
                Return _value
            End Get
            Set(value As Object)
                _value = value
            End Set
        End Property

    End Class


End Class
