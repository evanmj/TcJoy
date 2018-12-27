Imports TwinCAT
Imports TwinCAT.Ads
Imports System.ComponentModel

Module PLC_IO_Polling

    Dim debug = False
    Public WithEvents BackgroundWorker1 As BackgroundWorker

    ''' <summary>
    ''' Start off the background worker, which will configure itself and then run.
    ''' </summary>
    Public Sub StartWorker()

        ' Start worker with relevant data
        BackgroundWorker1 = New BackgroundWorker()
        BackgroundWorker1.WorkerSupportsCancellation = True
        BackgroundWorker1.RunWorkerAsync(Form1.BgTaskData)
        If debug Then Console.WriteLine("BG Worker Started, netID is " + Form1.BgTaskData.NetID)

    End Sub

    ''' <summary>
    ''' Start up the IO poller for the Twincat ADS data
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object,
                                         ByVal e As System.ComponentModel.DoWorkEventArgs) _
                                         Handles BackgroundWorker1.DoWork

        'Dim Stopwatch As Stopwatch = Stopwatch.StartNew()
        Dim iState As Integer = 0
        Dim symbol As ITcAdsSymbol ' working symbol. 
        Dim debug = False

        ' Do writes
        For Each tag As Form1.DataTag In e.Argument.TagList
            If tag.IsWritten Then

                Try
                    If debug Then Console.WriteLine("Writing Tag: " + tag.TagName + " to value: " + tag.Value.ToString)
                    ' Get data type and other information from PLC
                    symbol = e.Argument.ADS_Connection.AdsClient.ReadSymbolInfo(tag.TagName)
                    ' Write value to PLC
                    e.Argument.ADS_Connection.AdsClient.WriteSymbol(symbol, tag.Value)
                    e.Argument.IsConnected = e.Argument.ADS_Connection.AdsClient.IsConnected

                Catch ex As Exception
                    Console.WriteLine("Could not get symbol or write tag: " + tag.TagName + " ex: " + ex.Message)
                    e.Argument.IsConnected = False
                    BackgroundWorker1.CancelAsync()
                End Try

            End If
        Next

    End Sub

    Public Sub AddVariablesToList(ByRef BgTaskData As Form1.TaskData)

        Dim fbPath As String
        fbPath = Form1.TextBox_TcJoyPath.Text

        ' Note:  Data types not used, but leaving it here in case it is useful later.

        ' Add tags to our data object, ones that we write to TwinCAT (VB TO PLC)

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".bHeartBeatToggle", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iADSWatchdogMS", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iADSWatchdogDeadDurationMS", AdsDatatypeId.ADST_INT32, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".bControllerConnected", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".Start_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".Back_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".A_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".B_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".X_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".Y_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".LeftShoulder_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".RightShoulder_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".LeftStick_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".RightStick_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".DPad_Up_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".DPad_Left_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".DPad_Right_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".DPad_Down_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iLeftStick_X_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iLeftStick_Y_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iRightStick_X_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iRightStick_Y_Axis", AdsDatatypeId.ADST_INT32, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iLeftTrigger_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iRightTrigger_Axis", AdsDatatypeId.ADST_INT32, False, True))

        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".sBatteryInfo", AdsDatatypeId.ADST_STRING, False, True))

        ' Add tags to our data object, ones that read from TwinCAT From PLC To VB
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".bIsActive", AdsDatatypeId.ADST_BIT, True, False))
        BgTaskData.TagList.Add(New Form1.DataTag(fbPath + ".iUpdateRateMS", AdsDatatypeId.ADST_BIT, True, False))

    End Sub




End Module
