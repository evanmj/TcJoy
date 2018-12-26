Imports TwinCAT
Imports TwinCAT.Ads
Imports System.ComponentModel

Module PLC_IO_Polling

    Public BgTaskData As TaskData ' Data for background task that runs actual ADS comms.

    Dim aWDTimeout As Boolean
    Dim aWDTimer As Timer = New Timer

    ' Public WithEvents IsAliveTmr As Timer = New Timer

    Public WithEvents BackgroundWorker1 As BackgroundWorker

    Dim ADS_Connection As Ads_Link

    Dim fbPath As String

    ''' <summary>
    ''' Start off the background worker, which will configure itself and then run.
    ''' </summary>
    Public Sub StartWorker()

        ' Validate inputs
        Dim rate As Integer
        Try
            rate = Int32.Parse(Form1.TextBox_ADSRate.Text)
        Catch ex As Exception
            MessageBox.Show("Didn't understand the rate you put in, it must be an integer.  Defaulting to 100ms")
            Form1.TextBox_ADSPort.Text = "100"
            rate = 100
        End Try

        Dim port As Integer
        Try
            port = Int32.Parse(Form1.TextBox_ADSPort.Text)
        Catch ex As Exception
            MessageBox.Show("Didn't understand the port you put in, it must be an integer.  Defaulting to 851 for TC3 Runtime 1")
            Form1.TextBox_ADSPort.Text = "851"
            port = 851
        End Try

        Dim ADSWatchdog As Integer
        Try
            ADSWatchdog = Int32.Parse(Form1.TextBox_ADSWatchdog.Text)
        Catch ex As Exception
            MessageBox.Show("Didn't understand the ADS Watchdog you put in, it must be an integer.  Defaulting to 200ms")
            Form1.TextBox_ADSWatchdog.Text = "200"
            ADSWatchdog = 200
        End Try

        Dim ADSWatchdogDeadDuration As Integer
        Try
            ADSWatchdogDeadDuration = Int32.Parse(Form1.TextBox_ADSWatchdogDeadDuration.Text)
        Catch ex As Exception
            MessageBox.Show("Didn't understand the ADS Watchdog Dead Duration you put in, it must be an integer.  Defaulting to 2000ms")
            Form1.TextBox_ADSWatchdogDeadDuration.Text = "2000"
            ADSWatchdogDeadDuration = 2000
        End Try

        ' Build IO list

        BgTaskData = New TaskData(rate, ADSWatchdog, ADSWatchdogDeadDuration,
                                  Form1.TextBox_ADSNetID.Text,
                                  port)

        ' Build IO List
        AddVariablesToList(BgTaskData)

        ' Start worker with relevant data
        BackgroundWorker1 = New BackgroundWorker()
        BackgroundWorker1.WorkerSupportsCancellation = True
        BackgroundWorker1.RunWorkerAsync(BgTaskData)

    End Sub

    ''' <summary>
    ''' Start up the IO poller for the Twincat ADS data
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object,
                                         ByVal e As System.ComponentModel.DoWorkEventArgs) _
                                         Handles BackgroundWorker1.DoWork


        Dim Stopwatch As Stopwatch = Stopwatch.StartNew()
        Dim iState As Integer = 0
        Dim symbol As ITcAdsSymbol ' working symbol. 

        Console.WriteLine("BG Worker Started.")
        Console.WriteLine("Starting IO Polller with settings: " + e.Argument.NetID + ":" + e.Argument.Port.ToString + " with rate: " + e.Argument.UpdateRateMS.ToString)

        If ADS_Connection Is Nothing Then

            Try
                ADS_Connection = New Ads_Link(e.Argument.NetID, e.Argument.Port) ' Connect to local twincat. (todo)
            Catch Ex As System.Exception
                Console.WriteLine("StartPoller():" + Ex.Message)
            End Try

        End If

        While Not BackgroundWorker1.CancellationPending

            Select Case iState

                Case 0 ' Start polling

                    ' Do writes
                    For Each tag As DataTag In e.Argument.TagList
                        If tag.IsWritten Then

                            Console.WriteLine("Writing Tag: " + tag.TagName)
                            Try
                                ' Get data type and other information from PLC
                                symbol = ADS_Connection.AdsClient.ReadSymbolInfo(tag.TagName)
                                ' Write value to PLC
                                ADS_Connection.AdsClient.WriteSymbol(symbol, tag.Value)
                                e.Argument.IsConnected = ADS_Connection.AdsClient.IsConnected

                            Catch ex As Exception
                                Console.WriteLine("Could not get symbol or write tag: " + tag.TagName + " ex: " + ex.Message)
                                e.Argument.IsConnected = False
                                BackgroundWorker1.CancelAsync()
                            End Try

                        End If
                    Next

                    ' Do Reads
                    For Each tag As DataTag In e.Argument.TagList
                        If tag.IsRead Then

                            Console.WriteLine("Reading Tag: " + tag.TagName)
                            Try
                                ' Get data type and other information from PLC
                                symbol = ADS_Connection.AdsClient.ReadSymbolInfo(tag.TagName)

                                tag.Value = ADS_Connection.AdsClient.ReadSymbol(symbol)
                                Console.WriteLine(tag.TagName + " value = " + tag.Value.ToString)
                                e.Argument.IsConnected = ADS_Connection.AdsClient.IsConnected

                            Catch ex As Exception
                                Console.WriteLine("Could not get symbol or write tag: " + tag.TagName + " ex: " + ex.Message)
                                e.Argument.IsConnected = False
                                BackgroundWorker1.CancelAsync()
                            End Try

                        End If
                    Next

                    iState = 20

                Case 20 ' Start timer

                    Stopwatch.Restart()
                    Console.WriteLine("Wait...")
                    iState = 30

                Case 30 ' Wait

                    If Stopwatch.ElapsedMilliseconds > 1000 Then

                        Console.WriteLine("Go again!")
                        iState = 0
                    End If

            End Select

            ' Give processor a break
            Threading.Thread.Sleep(1)

        End While
        e.Cancel = True
        Return

    End Sub

    Public Sub AddVariablesToList(ByRef BgTaskData As TaskData)

        fbPath = Form1.TextBox_TcJoyPath.Text

        ' Note:  Data types not used, but leaving it here in case it is useful later.

        ' Add tags to our data object, ones that we write to TwinCAT (VB TO PLC)

        BgTaskData.TagList.Add(New DataTag(fbPath + ".bHeartBeatToggle", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".iADSWatchdogMS", AdsDatatypeId.ADST_INT32, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".bControllerConnected", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".Start_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".Back_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".A_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".B_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".X_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".Y_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".LeftShoulder_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".RightShoulder_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".LeftStick_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".RightStick_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".DPad_Up_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".DPad_Left_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".DPad_Right_Button", AdsDatatypeId.ADST_BIT, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".DPad_Down_Button", AdsDatatypeId.ADST_BIT, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".iLeftStick_X_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".iLeftStick_Y_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".iRightStick_X_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".iRightStick_Y_Axis", AdsDatatypeId.ADST_INT32, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".iLeftTrigger_Axis", AdsDatatypeId.ADST_INT32, False, True))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".iRightTrigger_Axis", AdsDatatypeId.ADST_INT32, False, True))

        BgTaskData.TagList.Add(New DataTag(fbPath + ".sBatteryInfo", AdsDatatypeId.ADST_STRING, False, True))

        ' Add tags to our data object, ones that read from TwinCAT From PLC To VB
        BgTaskData.TagList.Add(New DataTag(fbPath + ".iUpdateRateMS", AdsDatatypeId.ADST_INT32, True, False))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".bIsActive", AdsDatatypeId.ADST_BIT, True, False))
        BgTaskData.TagList.Add(New DataTag(fbPath + ".iADSWatchdogDeadDurationMS", AdsDatatypeId.ADST_INT32, True, False))

    End Sub

    Public Class TaskData

        Private _TagList As List(Of DataTag)
        Private _UpdateRateMS As Integer
        Private _ADSWatchdogMs As Integer
        Private _ADSWatchdogDeadDurationMS As Integer
        Private _NetID As String
        Private _Port As Integer
        Private _IsConnected As Boolean


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


End Module
