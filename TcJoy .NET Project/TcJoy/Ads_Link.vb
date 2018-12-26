Imports TwinCAT
Imports TwinCAT.Ads
Imports System.Windows.Forms
Imports System.Runtime.InteropServices

''' <summary>
''' ADS wrapper for attaching various ADS data types to .NET controls.
''' Jensen Mechatronics, LLC 2018
''' </summary>
''' <remarks></remarks>
Public Class Ads_Link

    Public WithEvents AdsClient As New TcAdsClient
    Public Symbols As TwinCAT.TypeSystem.ReadOnlySymbolCollection
    Dim dataStream As AdsStream = New AdsStream()
    Dim binReader As AdsBinaryReader
    Public binWriter As AdsBinaryWriter

    Private PendingAttachments As New List(Of AdsLinkVariable)
    Private VarAttachments As New List(Of AdsLinkVariable)

    Public Sub New(ByVal netID As String, ByVal port As Integer)
        Dim targetState As Ads.StateInfo

        binReader = New AdsBinaryReader(dataStream)
        binWriter = New AdsBinaryWriter(dataStream)
        'AdsClient = New TcAdsClient()
        AddHandler AdsClient.AdsNotification, New AdsNotificationEventHandler(AddressOf AdsNotificationCallback)
        '   AddHandler AdsClient.isConnected, AddressOf ConnectionChange

        AdsClient.Synchronize = True
        Try
            AdsClient.Connect(netID, port)
            targetState = AdsClient.ReadState()
            Console.WriteLine(String.Format("Target device state is {0}", targetState.AdsState))
        Catch ex As Ads.AdsErrorException
            Console.WriteLine(String.Format("Connection to PLC failed. {1}, Error code 0x{0:X}", ex.ErrorCode, ex.Message))
        End Try

        'Dim loader = AdsClient.CreateSymbolLoader(New SymbolLoaderSettings(SymbolsLoadMode.Flat, Ads.ValueAccess.ValueAccessMode.Symbolic, TwinCAT.Ads.ValueAccess.ValueAccessMode.Default))
        'Symbols = loader.Symbols()
        'For Each sym In Symbols
        '    Console.WriteLine(sym.ToString())
        'Next
        ' AdsClient.AddDeviceNotification("RAW_IO.hs_HeartbeatEcho", dataStream, 0, 2, AdsTransMode.OnChange, 1, 2, Main.AdsConnectionStatus)
    End Sub

    Private Sub AdsConnectionChangeCallback() Handles AdsClient.ConnectionStateChanged
        Console.WriteLine(String.Format("ADS Connection state has changed to {0}", AdsClient.IsConnected))
        Console.WriteLine(String.Format("Router state is: {0}", AdsClient.RouterState))
        Console.WriteLine(String.Format("Target IsLocal? {0}", AdsClient.IsLocal))
        If AdsClient.IsConnected And PendingAttachments.Count > 0 Then
            Console.WriteLine(String.Format("Will attempt to connect {0} pending AdsVariables", PendingAttachments.Count))
            For Each var As AdsLinkVariable In PendingAttachments
                Console.WriteLine(var.tagName)
                Attach(var)
            Next
        End If
        If Not AdsClient.IsConnected Then
            MessageBox.Show("Connection with PLC Lost! Restart this HMI when the PLC is back online.")
            Application.Exit()
            End
        End If
    End Sub

    Private Sub SysmbolTableChanged() Handles AdsClient.AdsSymbolVersionChanged
        Exit Sub
        Dim syminfo As Ads.ITcAdsSymbol

        Console.WriteLine("Symbol table has changed. Need to re-connect all variables.")
        For Each var As AdsLinkVariable In VarAttachments
            Console.WriteLine(String.Format("Re-attaching to {0}", var.tagName))
            Try
                AdsClient.DeleteDeviceNotification(var.handle)
            Catch ex As TwinCAT.Ads.AdsErrorException
                If ex.ErrorCode <> &H752 Then
                    'code 0x75 is "handle not found" which implies that we don't have an active notification.
                    Console.WriteLine(String.Format("Error: {0}", ex.Message))
                End If
            End Try
            'NOTE: if data size changes (because it's a structure?) we will be in trouble.
            Try
                syminfo = AdsClient.ReadSymbolInfo(var.tagName)
                var.handle = AdsClient.AddDeviceNotification(var.tagName, dataStream, 0, var.AdsDataSize, AdsTransMode.OnChange, 10, 0, var)
            Catch ex As TwinCAT.Ads.AdsErrorException
                Console.WriteLine(String.Format("Error: {0}", ex.Message))
            End Try
        Next
    End Sub

    Private Sub AdsNotificationCallback(sender As Object, e As TwinCAT.Ads.AdsNotificationEventArgs)
        Dim var As AdsLinkVariable = e.UserData
        e.DataStream.Position = e.Offset
        If var.UI_Element.GetType().BaseType() = GetType(MulticastDelegate) Then
            Dim tmp As MulticastDelegate = var.UI_Element
            Dim params = tmp.Method.GetParameters()
            If params(0).ParameterType() Is GetType(Boolean) Then
                tmp.DynamicInvoke(binReader.ReadBoolean())

            ElseIf params(0).ParameterType() Is GetType(Double) Then
                tmp.DynamicInvoke(binReader.ReadDouble())
            ElseIf params(0).ParameterType() Is GetType(Integer) Then
                If var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT32 Then
                    tmp.DynamicInvoke(binReader.ReadInt32())
                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT16 Then
                    tmp.DynamicInvoke(binReader.ReadInt16())
                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_UINT16 Then
                    tmp.DynamicInvoke(binReader.ReadUInt16())
                End If
            Else
                tmp.DynamicInvoke(binReader.ReadInt16().ToString())
            End If

            'Main.Invoke(tmp, binReader.ReadInt16().ToString())
        ElseIf var.UI_Element.GetType() = GetType(CheckBox) Then
            Dim chkbox As CheckBox = e.UserData
            ''Console.WriteLine("CheckBox Changed FROM plc: " + e.UserData.Text + " = " + binReader.ReadBoolean().ToString())
            'Dim CurrentHandler = chkbox.get
            var.UI_Element.Checked = binReader.ReadBoolean()
        ElseIf var.UI_Element.GetType() = GetType(RadioButton) Then
            'Do nothing for the button click, checked handler will be a MulticastDelegate
        ElseIf var.UI_Element.GetType().GetProperty("Text") IsNot Nothing Then

            ' Only read if we are not focused, or else it will conflict with the user typing in data.
            If Not var.UI_Element.Focused Then

                If var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT32 Then
                    var.UI_Element.Text = binReader.ReadInt32().ToString()
                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT16 Then
                    var.UI_Element.Text = binReader.ReadInt16().ToString()
                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_REAL32 Then
                    'var.UI_Element.Text = binReader.ReadSingle().ToString("#0.0000")
                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_REAL64 Then
                    'var.UI_Element.Text = binReader.ReadDouble().ToString("#0.0000")
                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_STRING Then
                    var.UI_Element.Text = binReader.ReadPlcAnsiString(var.SymbolInfo.BitSize)
                Else
                    var.UI_Element.Text = binReader.ReadInt16().ToString()
                End If

            End If

        ElseIf var.UI_Element.GetType() = GetType(Boolean) Then
            var.UI_Element = binReader.ReadBoolean()
        End If

    End Sub

    Public Sub Attach(variable As AdsLinkVariable)
        Attach(variable.tagName, variable.action, variable.dotNetDataType)
    End Sub

    Public Sub Attach(ByVal Variable As String, ByRef Obj As Object, Optional ByVal dataType As Type = Nothing)
        Dim var As AdsLinkVariable

        If Variable = "" Then
            Console.WriteLine("Skipping attempt to attach to an un-named variable.")
            Exit Sub
        End If

        If Obj Is Nothing Then
            Console.WriteLine(String.Format("Not attaching to PLC variable {0} because no action is defined.", Variable))
            Exit Sub
        End If

        Dim h As Integer
        Dim syminfo As Ads.ITcAdsSymbol5

        Try
            syminfo = AdsClient.ReadSymbolInfo(Variable)

            If syminfo Is Nothing Then
                Console.WriteLine(String.Format("Unable to get symbol info for {0}. Is name spelled correctly?", Variable))
            Else

                var = New AdsLinkVariable(Variable, Obj, h, dataType)
                var.AdsDataSize = syminfo.Size
                var.SymbolInfo = syminfo
                var.UI_Element = Obj

                ' https://infosys.beckhoff.com/english.php?content=../content/1033/tcadsamsspec/html/tcadsamsspec_adscmd_adddevicenotification.htm&id=
                ' EMJ: Changed 10ms here to 250 for Cycle Time.  1/4 sec updates will be easier on our processor than 10ms.
                h = AdsClient.AddDeviceNotification(Variable, dataStream, 0, syminfo.Size, AdsTransMode.OnChange, 250, 0, var)
                ' Console.WriteLine("Attached PLC variable {0}, size = {1} bytes, handle = {2}", Variable, syminfo.Size, h)

                VarAttachments.Add(var)
            End If
        Catch ex As Ads.AdsErrorException
            'Something went wrong. Assume target is offline, add variable to pending list.
            Console.WriteLine(String.Format("Error while linking to PLC var {0}. Will try again later.", Variable))
            Dim AdsVar As New AdsLinkVariable(Variable, Obj, 0, dataType)
            PendingAttachments.Add(AdsVar)
            Exit Sub
        End Try

        If Obj.GetType() = GetType(CheckBox) Then
            'dataSize = 2
            Dim tmp As CheckBox = Obj
            'Only user updates to checkbox will fire event to PLC
            'Programmatic changes to check state will NOT fire off an event.
            AddHandler tmp.Click, Sub(sender As Object, e As EventArgs)
                                      Console.WriteLine("CheckBox Changed by user ")
                                      AdsClient.WriteSymbol(Variable, tmp.Checked, False)
                                  End Sub
            AddHandler tmp.EnabledChanged, Sub(sender As Object, e As EventArgs)
                                               Console.WriteLine("CheckBox disabled/enabled")
                                               AdsClient.WriteSymbol(Variable, False, False)
                                           End Sub
        ElseIf Obj.GetType() = GetType(TextBox) Then
            Dim tmp As TextBox = Obj

            AddHandler tmp.KeyPress, Sub(sender As Object, e As KeyPressEventArgs)

                                         ' e.Handled = True
                                         Dim keyVal As Integer = AscW(e.KeyChar)
                                         Dim type1 As Boolean = e.KeyChar = vbBack
                                         Dim negative As Boolean = keyVal = Keys.OemMinus Or keyVal = Keys.Subtract Or e.KeyChar = "-"
                                         Dim dec As Boolean = e.KeyChar = "."
                                         Select Case syminfo.DataTypeId()
                                             Case AdsDatatypeId.ADST_INT32, AdsDatatypeId.ADST_INT16
                                                 If Not (Char.IsDigit(e.KeyChar) Or type1 Or negative) Then
                                                     e.Handled = True
                                                 End If

                                             Case AdsDatatypeId.ADST_UINT16, AdsDatatypeId.ADST_UINT32, AdsDatatypeId.ADST_UINT64
                                                 If Not (Char.IsDigit(e.KeyChar) Or type1) Then
                                                     e.Handled = True
                                                 End If
                                             Case AdsDatatypeId.ADST_REAL32, AdsDatatypeId.ADST_REAL64
                                                 If Not (Char.IsDigit(e.KeyChar) Or type1 Or negative Or dec) Then
                                                     e.Handled = True
                                                 End If

                                         End Select

                                         If Not e.Handled Then

                                         End If
                                     End Sub

            AddHandler tmp.KeyUp, Sub()
                                      Try

                                          If var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT32 Then
                                              AdsClient.WriteSymbol(Variable, Convert.ToInt32(tmp.Text), False)
                                          ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT16 Then
                                              AdsClient.WriteSymbol(Variable, Convert.ToInt16(tmp.Text), False)
                                          ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_UINT32 Then
                                              AdsClient.WriteSymbol(Variable, Convert.ToUInt32(tmp.Text), False)
                                          ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_UINT16 Then
                                              AdsClient.WriteSymbol(Variable, Convert.ToUInt16(tmp.Text), False)
                                          ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_REAL32 Then
                                              AdsClient.WriteSymbol(Variable, Convert.ToSingle(tmp.Text), False)
                                          ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_REAL64 Then
                                              AdsClient.WriteSymbol(Variable, Convert.ToDouble(tmp.Text), False)
                                          ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_STRING Then
                                              ' var.UI_Element.Text = binReader.ReadPlcAnsiString(var.SymbolInfo.BitSize)
                                          Else
                                              'var.UI_Element.Text = binReader.ReadInt16().ToString()
                                              AdsClient.WriteSymbol(Variable, Convert.ToInt16(tmp.Text), False)

                                          End If

                                      Catch ex As Exception
                                          Console.WriteLine(ex.Message)
                                      End Try
                                  End Sub

            AddHandler tmp.TextChanged, Sub()
                                            Try

                                                If var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT32 Then
                                                    AdsClient.WriteSymbol(Variable, Convert.ToInt32(tmp.Text), False)
                                                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_INT16 Then
                                                    AdsClient.WriteSymbol(Variable, Convert.ToInt16(tmp.Text), False)
                                                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_UINT32 Then
                                                    AdsClient.WriteSymbol(Variable, Convert.ToUInt32(tmp.Text), False)
                                                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_UINT16 Then
                                                    AdsClient.WriteSymbol(Variable, Convert.ToUInt16(tmp.Text), False)
                                                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_REAL32 Then
                                                    AdsClient.WriteSymbol(Variable, Convert.ToSingle(tmp.Text), False)
                                                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_REAL64 Then
                                                    AdsClient.WriteSymbol(Variable, Convert.ToDouble(tmp.Text), False)
                                                ElseIf var.SymbolInfo.DataTypeId() = AdsDatatypeId.ADST_STRING Then
                                                    ' var.UI_Element.Text = binReader.ReadPlcAnsiString(var.SymbolInfo.BitSize)
                                                Else
                                                    'var.UI_Element.Text = binReader.ReadInt16().ToString()
                                                    AdsClient.WriteSymbol(Variable, Convert.ToInt16(tmp.Text), False)

                                                End If

                                            Catch ex As Exception
                                                Console.WriteLine(ex.Message)
                                            End Try
                                        End Sub

        ElseIf Obj.GetType() = GetType(RadioButton) Then
            Dim tmp As RadioButton = Obj

            'Add actions for button clicks (momentary)
            AddHandler tmp.MouseDown, Sub(sender As Object, e As MouseEventArgs)

                                          AdsClient.WriteSymbol(Variable, True, False)
                                      End Sub

            AddHandler tmp.MouseUp, Sub()
                                        AdsClient.WriteSymbol(Variable, False, False)
                                    End Sub
            AddHandler tmp.EnabledChanged, Sub(sender As Object, e As EventArgs)
                                               Console.WriteLine("RadioButton disabled/enabled")
                                               AdsClient.WriteSymbol(Variable, False, False)
                                           End Sub
        End If

    End Sub

End Class

Public Class AdsLinkVariable

    Public Property tagName As String
    Public Property action As Object
    Public Property dotNetDataType As Type = Nothing
    Public Property AdsDataSize As Integer
    Public Property handle As Integer = 0
    Public Property SymbolInfo As ITcAdsSymbol5
    Public Property UI_Element As Object

    Public Sub New(tagName As String, action As Object, handle As Integer, Optional dotNetDataType As Type = Nothing)
        _tagName = tagName
        _action = action
        _dotNetDataType = dotNetDataType
        _handle = handle
    End Sub

End Class
