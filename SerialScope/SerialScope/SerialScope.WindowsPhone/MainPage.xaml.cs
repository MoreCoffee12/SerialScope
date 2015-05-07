using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Networking.Proximity;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.Graphics.Display;
using VisualizationTools;
using DataBus;


namespace SerialScope
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            // Create our LineGraph and hook the up to their respective images
            graphScope1 = new VisualizationTools.LineGraph((int)LineGraphScope1.Width, (int)LineGraphScope1.Height);
            LineGraphScope1.Source = graphScope1;

            // Initialize the helper functions for the scope user interface (UI)
            uihelper = new ScopeUIHelper();
            uihelper.CRTMargin_Vert = 20;
            uihelper.CRTMargin_Horz = 20;
            vcHelper = new VerticalControlHelper();
            hcHelper = new HorizontalControlHelper();
            hcHelper.iDivisionCount = ScopeGrid.ColumnDefinitions.Count;
            tHelper = new TriggerHelper();
            tHelper.fTriggerLevel_V = 1.0f;
            tHelper.bTriggerSet = false;
            tHelper.bAcquiring = false;

            // The sampling frequency here must match that configured in the Arduino firmware
            hcHelper.fSamplingFreq_Hz = 625;

            // Initialize the data bus
            mbus = new MinSegBus();

            // Initialize the buffers that recieve the data from the Arduino
            bCollectData = false;
            iChannelCount = 2;
            iStreamSampleCount = 2;
            iShortCount = iChannelCount * iStreamSampleCount;
            iFrameSize = mbus.iGetFrameCount_Short(iShortCount);
            idxData = 0;
            iStreamBuffLength = 128;
            idxData = 0;
            idxData_MinCount = 0;
            idxCharCount = 0;
            byteAddress = 0;
            iUnsignedShortArray = new UInt16[iShortCount];

            // Data buffers
            dataScope1 = new float[hcHelper.iScopeDataLength];
            dataScope2 = new float[hcHelper.iScopeDataLength];
            dataNull = new float[hcHelper.iScopeDataLength];
            for (int idx = 0; idx < hcHelper.iScopeDataLength; idx++)
            {
                dataScope1[idx] = Convert.ToSingle(1.0 + Math.Sin(Convert.ToDouble(idx) * (2.0 * Math.PI / Convert.ToDouble(hcHelper.iGetCRTDataLength()))));
                dataScope2[idx] = Convert.ToSingle(1.0 - Math.Sin(Convert.ToDouble(idx) * (2.0 * Math.PI / Convert.ToDouble(hcHelper.iGetCRTDataLength()))));
                dataNull[idx] = -100.0f;
            }

            // Scale factors
            fScope1ScaleADC = 5.0f / 1024.0f;
            fScope2ScaleADC = 5.0f / 1024.0f;

            // Default scope parameters
            bTrace1Active = true;
            bTrace2Active = true;

            // Update scope
            UpdateTraces();
            graphScope1.setMarkIndex(Convert.ToInt32(hcHelper.iScopeDataLength / 2));
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            double dCRT_Vert = _CRTRow.ActualHeight - uihelper.CRTMargin_Vert;
            double dCRT_Horz = CRTGrid.ActualWidth - uihelper.CRTMargin_Horz;

            LineGraphScope1.Width = dCRT_Horz-1;
            LineGraphScope1.Height = dCRT_Vert;
            
            ScopeGrid.Width = dCRT_Horz-1;
            ScopeGrid.Height = dCRT_Vert;

            hcHelper.dHorzPosTickWidth = tbHorzTick.ActualWidth;

            // Initialize the UI
            uihelper.Initialize(ScopeGrid,
            tbCh1VertDiv, tbCh1VertDivValue, tbCh1VertDivEU,
            tbCh2VertDiv, tbCh2VertDivValue, tbCh2VertDivEU,
            rectCh1button, rectCh2button,
            tbCh1VertTick, tbCh2VertTick);

            // Initialize the buffer for the frame timebase and set the color
            graphScope1.setColorBackground(uihelper.fR, uihelper.fG, uihelper.fB, 0.0f);

            // Configure the line plots scaling based on the grid
            bUpdateScopeParams();
            bUpdateDateTime();
            UpdateTriggerUI();

            // Also hook the Rendering cycle up to the CompositionTarget Rendering event so we draw frames when we're supposed to
            CompositionTarget.Rendering += graphScope1.Render;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            // Reset the debug window
            this.textOutput.Text = "";


        }

        protected override void OnNavigatedFrom(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            btHelper.Disconnect();
        }

        # region Private Methods

        private void SetTraceActive()
        {
            graphScope1.setColor(Convert.ToSingle(uihelper.clrTrace1.R) / 255.0f, Convert.ToSingle(uihelper.clrTrace1.G) / 255.0f, Convert.ToSingle(uihelper.clrTrace1.B) / 255.0f, Convert.ToSingle(uihelper.clrTrace2.R) / 255.0f, Convert.ToSingle(uihelper.clrTrace2.G) / 255.0f, Convert.ToSingle(uihelper.clrTrace2.B) / 255.0f);
        }

        private void SetTraceAcquiring()
        {
            graphScope1.setColor(Convert.ToSingle(uihelper.clrTrace1.R) / 128.0f, Convert.ToSingle(uihelper.clrTrace1.G) / 128.0f, Convert.ToSingle(uihelper.clrTrace1.B) / 128.0f, Convert.ToSingle(uihelper.clrTrace2.R) / 128.0f, Convert.ToSingle(uihelper.clrTrace2.G) / 128.0f, Convert.ToSingle(uihelper.clrTrace2.B) / 128.0f);
        }

        private bool bUpdateDateTime()
        {
            // Get the current date.
            DateTime thisDay = DateTime.Today;

            tbDateTime.Text = thisDay.ToString("d-MMM-yy");

            return true;
        }

        private void UpdateTextBlockVoltage(TextBlock tbTextBlock, TextBlock tbTextBlockEU, float fVolts)
        {
            // Update the scope vertical divisions scale factor
            if (fVolts < 1.0f)
            {
                tbTextBlock.Text = (fVolts * 1000.0f).ToString("F0", CultureInfo.InvariantCulture);
                tbTextBlockEU.Text = "mV";
            }
            else
            {
                tbTextBlock.Text = fVolts.ToString("F2", CultureInfo.InvariantCulture);
                tbTextBlockEU.Text = "V";
            }
        }

        private bool bUpdateScopeParams()
        {
            // Set the limits of the graph in the image
            graphScope1.setYLim(0.0f, Convert.ToSingle(uihelper.iGridRowCount));

            // Set the individual trace scale factors
            graphScope1.setCh1Scale( 1.0f / vcHelper.fGetCh1VertDiv_V());
            graphScope1.setCh2Scale (1.0f / vcHelper.fGetCh2VertDiv_V());

            // Update the scope vertical divisions scale factor
            UpdateTextBlockVoltage(tbCh1VertDivValue, tbCh1VertDivEU, vcHelper.fGetCh1VertDiv_V());
            UpdateTextBlockVoltage(tbCh2VertDivValue, tbCh2VertDivEU, vcHelper.fGetCh2VertDiv_V());
 
            // Update the horizontal divisions
            UpdateHorzDiv();

            // Update vertical tick markers
            UpdateVertTicks();

            // Update trigger status
            UpdateTriggerUI();

            return true;
        }

        // Method to control visibility of horizontal position
        private void HorzPosVisible(bool bIsVisible)
        {
            if(bIsVisible)
            {
                tbHorzTick.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbHorzPos.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbHorzPosValue.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbHorzPosEU.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                tbHorzTick.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbHorzPos.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbHorzPosValue.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbHorzPosEU.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        // These trigger controls are updated based on the data stream
        private void UpdateTrigger()
        {
            switch(tHelper.Mode)
            {
                case TriggerMode.Normal:
                    HorzPosVisible(true);
                    if( idxData > idxData_MinCount)
                    {
                        if( tHelper.Status == TriggerStatus.Armed)
                        {
                            tHelper.Status = TriggerStatus.Ready;
                            rectTriggerOK.Fill = new SolidColorBrush(Colors.Yellow);
                            tHelper.bAcquiring = true;
                        }
                    }
                    else
                    {
                        tHelper.Status = TriggerStatus.Armed;
                        tHelper.bAcquiring = true;
                    }
                    if( tHelper.Status == TriggerStatus.Trigd)
                    {
                        tHelper.bAcquiring = false;
                        rectTriggerOK.Fill = new SolidColorBrush(Colors.Green);
                    }
                    break;
                case TriggerMode.Scan:
                    tHelper.bAcquiring = false;
                    HorzPosVisible(false);
                    break;
                default:
                    break;
            }

            // Update the UI
            txtTriggerMode.Text = tHelper.TriggerStatusText();
        }

        // These trigger controls are updated when the user makes a 
        // change to the scope settings
        private void UpdateTriggerUI()
        {

            UpdateTrigger();

            btnTriggerMode.Content = tHelper.TriggerModeText().ToUpper();
            if (tHelper.Mode == TriggerMode.Scan)
            {
                setRectGray(true, rectTriggerOK, btnHorzOffsetLeft.Foreground);
                setRectGray(true, rectTriggerSlope, btnTriggerSlope.Foreground);
                setRectGray(true, rectHorzToZero, btnHorzToZero.Foreground);

                setRectGray(true, rectHorzOffsetLeft, btnHorzOffsetLeft.Foreground);
                setRectGray(true, rectHorzOffsetRight, btnHorzOffsetRight.Foreground);
            }
            else
            {
                setRectGray(false, rectHorzOffsetLeft, btnHorzOffsetLeft.Foreground);
                setRectGray(false, rectHorzOffsetRight, btnHorzOffsetRight.Foreground);
                setRectGray(false, rectHorzToZero, btnHorzToZero.Foreground);

                setRectGray(false, rectTriggerOK, btnHorzOffsetLeft.Foreground);
                setRectGray(false, rectTriggerSlope, btnTriggerSlope.Foreground);

                UpdateHorzPos();
            }

            btnTriggerSource.Content = tHelper.TriggerSourceText().ToUpper();
            switch (tHelper.Source)
            {
                case TriggerSource.Ch1:
                    SolidColorBrush brushTrigger1 = new SolidColorBrush();
                    brushTrigger1.Color = uihelper.clrTrace1;
                    tbTriggerSource.Foreground = brushTrigger1;
                    tbTriggerSlope.Foreground = brushTrigger1;
                    tbTriggerLevel.Foreground = brushTrigger1;
                    tbTriggerLevelEU.Foreground = brushTrigger1;
                    tbTriggerTick.Foreground = brushTrigger1;
                    break;

                case TriggerSource.Ch2:
                    SolidColorBrush brushTrigger2 = new SolidColorBrush();
                    brushTrigger2.Color = uihelper.clrTrace2;
                    tbTriggerSource.Foreground = brushTrigger2;
                    tbTriggerSlope.Foreground = brushTrigger2;
                    tbTriggerLevel.Foreground = brushTrigger2;
                    tbTriggerLevelEU.Foreground = brushTrigger2;
                    tbTriggerTick.Foreground = brushTrigger2;
                    break;

                case TriggerSource.Ext:
                    tbTriggerSource.Foreground = txtHeader.Foreground;
                    tbTriggerSlope.Foreground = txtHeader.Foreground;
                    tbTriggerLevel.Foreground = txtHeader.Foreground;
                    tbTriggerLevelEU.Foreground = txtHeader.Foreground;
                    tbTriggerTick.Foreground = txtHeader.Foreground;
                    break;

                default:
                    break;

            }

            if( rectTriggerSlope.ActualWidth < 80)
            {
                btnTriggerSlope.Content = tHelper.TriggerSlopeText().Substring(0, 3).ToUpper() + "...";
            }
            else
            {
                btnTriggerSlope.Content = tHelper.TriggerSlopeText().ToUpper();
            }
            if ( tHelper.Slope == TriggerSlope.Rising )
            {
                ScaleTransform scaleTemp = new ScaleTransform();
                scaleTemp.ScaleX = 1;
                TransformGroup tgTemp = new TransformGroup();
                tgTemp.Children.Add(scaleTemp);
                tbTriggerSlope.RenderTransform = tgTemp;
                tbTriggerSlope.TextAlignment = TextAlignment.Left;
            }
            else
            {
                ScaleTransform scaleTemp = new ScaleTransform();
                scaleTemp.ScaleX = -1;
                TransformGroup tgTemp = new TransformGroup();
                tgTemp.Children.Add(scaleTemp);
                tbTriggerSlope.RenderTransform = tgTemp;
                tbTriggerSlope.TextAlignment = TextAlignment.Right;
            }

            UpdateTextBlockVoltage(tbTriggerLevel, tbTriggerLevelEU, tHelper.fTriggerLevel_V);
            UpdateTriggerTick();

        }

        private void UpdateHorzDiv()
        {
            // Update the horizontal divisions
            float fDivRaw = hcHelper.fGetDivRaw();
            idxData_MinCount = Convert.ToUInt32(hcHelper.fGetDivRaw()*hcHelper.fSamplingFreq_Hz);
            if (fDivRaw < 1)
            {
                tbHorzDivValue.Text = (fDivRaw * 1000).ToString("F0", CultureInfo.InvariantCulture);
                tbHorzDivEU.Text = "ms";
            }
            else
            {
                tbHorzDivValue.Text = fDivRaw.ToString("F2", CultureInfo.InvariantCulture);
                tbHorzDivEU.Text = "s";
            }

            // Update the position marker
            UpdateHorzPos();
            UpdateTraces();

        }

        private void UpdateHorzPos()
        {
            // Update the horizontal position, in seconds
            int iPosIdx = hcHelper.iHorzPosIdx + Convert.ToInt32(hcHelper.iGetCRTDataLength() / 2);
            float fPosRaw_s = -Convert.ToSingle(hcHelper.iHorzPosIdx) / hcHelper.fSamplingFreq_Hz;
            if (fPosRaw_s < 1)
            {
                tbHorzPosValue.Text = (fPosRaw_s * 1000).ToString("F0", CultureInfo.InvariantCulture);
                tbHorzPosEU.Text = "ms";
            }
            else
            {
                tbHorzPosValue.Text = fPosRaw_s.ToString("F2", CultureInfo.InvariantCulture);
                tbHorzPosEU.Text = "s";
            }

            // Locate the horizontal tick mark
            double dHorzTickLocation = -hcHelper.dHorzPosTickWidth/2.0;
            dHorzTickLocation = dHorzTickLocation + (LineGraphScope1.ActualWidth / Convert.ToDouble(hcHelper.iCRTDataLength)) * Convert.ToDouble(iPosIdx);
            tbHorzTick.Margin = new Thickness(dHorzTickLocation, 0, 0, 0);

        }

        private void UpdateTickPosition( String strDefault, String strHigh, String strLow, TextBlock tbTick, float fOffset_Volts )
        {

            float fCRTUpper = Convert.ToSingle(uihelper.CRTMargin_Vert / 2);
            float fScopeLimits = graphScope1.getYLimMax() - graphScope1.getYLimMin();

            if (fScopeLimits < 1e-6)
            {
                return;
            }

            if (tbTick.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                tbTick.Text = strDefault;
                int iVert = Convert.ToInt32((Convert.ToSingle(uihelper.iGridRowCount) - fOffset_Volts) * (Convert.ToSingle(LineGraphScope1.Height) / fScopeLimits));

                if (iVert > Convert.ToInt32(LineGraphScope1.Height))
                {
                    iVert = Convert.ToInt32(LineGraphScope1.Height);
                    tbTick.Text = strLow;
                }
                if (iVert < 0)
                {
                    iVert = 0;
                    tbTick.Text = strHigh;
                }

                iVert = iVert + Convert.ToInt32(fCRTUpper - Convert.ToSingle(tbTick.ActualHeight / 2));
                tbTick.Margin = new Thickness(0, iVert, 0, 0);
            }

        }

        private void UpdateTriggerTick()
        {
            switch(tHelper.Mode)
            {
                case TriggerMode.Normal:

                    switch(tHelper.Source)
                    {
                        case TriggerSource.Ch1:
                            tbTriggerTick.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            UpdateTickPosition("←", "↑", "↓", tbTriggerTick, ((tHelper.fTriggerLevel_V / vcHelper.fGetCh1VertDiv_V()) + vcHelper.fCh1VertOffset));
                            break;
                        case TriggerSource.Ch2:
                            tbTriggerTick.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            UpdateTickPosition("←", "↑", "↓", tbTriggerTick, ((tHelper.fTriggerLevel_V / vcHelper.fGetCh2VertDiv_V()) + vcHelper.fCh2VertOffset));
                            break;
                        case TriggerSource.Ext:
                            tbTriggerTick.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            break;
                        default:
                            break;
                    }
                    break;
                case TriggerMode.Scan:
                    tbTriggerTick.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                default:
                    break;

            }
        }

        private void UpdateVertTicks()
        {

            UpdateTickPosition("1→", "1↑", "1↓", tbCh1VertTick, vcHelper.fCh1VertOffset);
            UpdateTickPosition("2→", "2↑", "2↓", tbCh2VertTick, vcHelper.fCh2VertOffset);
            UpdateTriggerTick();

        }

        // Read in iBuffLength number of samples
        private async Task<bool> bReadSource(DataReader input)
        {
            UInt32 k;
            byte byteInput;
            uint iErrorCount = 0;

            // Read in the data
            for (k = 0; k < iStreamBuffLength; k++)
            {

                // Wait until we have 1 byte available to read
                await input.LoadAsync(1);

                // Read in the byte
                byteInput = input.ReadByte();

                // Save to the ring buffer and see if the frame can be parsed
                if (++idxCharCount < iFrameSize)
                {
                    mbus.writeRingBuff(byteInput);
                }
                else
                {
                    iUnsignedShortArray = mbus.writeRingBuff(byteInput, iShortCount);
                    iErrorCount = mbus.iGetErrorCount();
                }

                if (iErrorCount == 0 && idxCharCount >= iFrameSize)
                {
                    // The frame was valid
                    rectFrameOK.Fill = new SolidColorBrush(Colors.Green);

                    // Was it the next one in the sequence?
                    if( ++byteAddress == Convert.ToByte(mbus.iGetAddress()))
                    {
                        rectFrameSequence.Fill = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        rectFrameSequence.Fill = new SolidColorBrush(Colors.Red);
                        byteAddress = Convert.ToByte(mbus.iGetAddress());
                    }


                    AddScopeData( Convert.ToSingle(iUnsignedShortArray[0]) * fScope1ScaleADC, Convert.ToSingle(iUnsignedShortArray[1]) * fScope2ScaleADC);
                    AddScopeData(Convert.ToSingle(iUnsignedShortArray[2]) * fScope1ScaleADC, Convert.ToSingle(iUnsignedShortArray[3]) * fScope2ScaleADC);

                    // Reset the character counter
                    idxCharCount = 0;
                }

                if(idxCharCount>(iFrameSize>>1))
                {
                    rectFrameOK.Fill = new SolidColorBrush(Colors.Black);

                }


            }

            // Success, return a true
            return true;
        }

        private void AddScopeData(float fCh1, float fCh2)
        {

            // Pass the new points into the trigger helper class
            if (idxData > hcHelper.iCRTDataHalfLength && idxData < (hcHelper.iCRTDataHalfLength * 3))
            {
                if (tHelper.bNewDataPointsSetTrigger(dataScope1[idxData], dataScope2[idxData], fCh1, fCh2, 0.0f, idxData))
                {
                    ClearDataArrays(Convert.ToInt32(idxData+1));
                }
            }

            // Point to the next location in the buffer
            iNextDataIdx();

            // Fill the spaces in the buffer with data
            dataScope1[idxData] = fCh1;
            dataScope2[idxData] = fCh2;


        }

        private void iNextDataIdx()
        {
            ++idxData;
            switch (tHelper.Mode)
            {
                case TriggerMode.Scan:
                    idxData = idxData % hcHelper.iGetCRTDataLength();
                    break;
                case TriggerMode.Normal:
                    if( idxData == 0)
                    {
                        ClearDataArrays();
                        tHelper.ResetTrigger();
                    }
                    idxData = idxData % ( hcHelper.iGetCRTDataLength() << 1);
                    break;
                default:
                    break;
            }
        }

        private async void ReadData()
        {
            bool bTemp;

            // Made it this far so status is ok
            rectBTOK.Fill = new SolidColorBrush(Colors.Green);

            // Loop so long as the collect data button is enabled
            while (bCollectData)
            {
                // Read a line from the input, once again using await to translate a "Task<xyz>" to an "xyz"
                bTemp = (await bReadSource(btHelper.input));
                if (bTemp == true)
                {

                    // Update trigger state
                    UpdateTrigger();
                    UpdateTracesAcquiring();

                    // Append that line to our TextOutput
                    try
                    {
                        if (bCollectData  && !tHelper.bAcquiring )
                        {
                            // Update the blank space and plot the data
                            graphScope1.setMarkIndex(Convert.ToInt32(idxData));
                            UpdateTraces();
                        }

                    }
                    catch (Exception ex)
                    {
                        this.textOutput.Text = "Exception:  " + ex.ToString();
                    }
                }

            }
            
            // close the connection
            await btHelper.Disconnect();

            // Clear LEDs
            ResetLEDs();

        }

        private void UpdateTracesAcquiring()
        {
            if (tHelper.bAcquiring)
            {
                SetTraceAcquiring();
            }
            else
            {
                SetTraceActive();
            }

        }

        private void UpdateTraces()
        {

            // Calculate the portion of data to display
            if( tHelper.Mode == TriggerMode.Scan)
            {
                iCRTDataStart = 0;
                iCRTDataEnd = hcHelper.iGetCRTDataLength();
            }
            else 
            {
                if( tHelper.Status == TriggerStatus.Trigd  || tHelper.Status == TriggerStatus.Ready)
                {
                    iCRTDataStart = Convert.ToUInt32(tHelper.idxTrigger - (hcHelper.iGetCRTDataLength() >> 1) - hcHelper.iHorzPosIdx);
                    iCRTDataEnd = Convert.ToUInt32(tHelper.idxTrigger + (hcHelper.iGetCRTDataLength() >> 1) - hcHelper.iHorzPosIdx);
                    //this.textOutput.Text = tHelper.idxTrigger.ToString() + "|" + hcHelper.iHorzPosIdx.ToString() + "|" + iCRTDataStart.ToString()  + "|" + iCRTDataEnd.ToString();
                }
            }

            UpdateTracesAcquiring();

                // Update the plots
            if (bTrace1Active && bTrace2Active)
            {
                graphScope1.setArray(dataScope1, dataScope2, iCRTDataStart, iCRTDataEnd);
            }
            if (bTrace1Active && !bTrace2Active)
            {
                graphScope1.setArray(dataScope1, dataNull, iCRTDataStart, iCRTDataEnd);
            }
            if (!bTrace1Active && bTrace2Active)
            {
                graphScope1.setArray(dataNull, dataScope2, iCRTDataStart, iCRTDataEnd);
            }
            if (!bTrace1Active && !bTrace2Active)
            {
                graphScope1.setArray(dataNull, dataNull, iCRTDataStart, iCRTDataEnd);
            }

            setCh1Visible(bTrace1Active);
            setCh2Visible(bTrace2Active);

        }

        private void setRectGray(bool bIsGray, Rectangle rect, Brush rectBrush)
        {
            if (bIsGray)
            {
                rect.Fill = rectBrush;
                rect.Opacity = 0.5;
            }
            else
            {
                rect.Fill = null;
                rect.Opacity = 1.0;
            }
        }

        private void setCh1Visible(bool bIsVisible)
        {
            if (bIsVisible)
            {
                tbCh1VertDiv.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbCh1VertDivValue.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbCh1VertDivEU.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbCh1VertTick.Visibility = Windows.UI.Xaml.Visibility.Visible;

            }
            else
            {
                tbCh1VertDiv.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbCh1VertDivValue.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbCh1VertDivEU.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbCh1VertTick.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            }

            setRectGray(!bIsVisible, rectCh1OffsetPlus, btnCh1OffsetPlus.Foreground);
            setRectGray(!bIsVisible, rectCh1OffsetMinus, btnCh1OffsetMinus.Foreground);
            setRectGray(!bIsVisible, rectCh1ScalePlus, btnCh1ScalePlus.Foreground);
            setRectGray(!bIsVisible, rectCh1ScaleMinus, btnCh1ScaleMinus.Foreground);
        }

        private void setCh2Visible(bool bIsVisible)
        {
            if (bIsVisible)
            {
                tbCh2VertDiv.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbCh2VertDivValue.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbCh2VertDivEU.Visibility = Windows.UI.Xaml.Visibility.Visible;
                tbCh2VertTick.Visibility = Windows.UI.Xaml.Visibility.Visible;

            }
            else
            {
                tbCh2VertDiv.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbCh2VertDivValue.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbCh2VertDivEU.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                tbCh2VertTick.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            }

            setRectGray(!bIsVisible, rectCh2OffsetPlus, btnCh2OffsetPlus.Foreground);
            setRectGray(!bIsVisible, rectCh2OffsetMinus, btnCh2OffsetMinus.Foreground);
            setRectGray(!bIsVisible, rectCh2ScalePlus, btnCh2ScalePlus.Foreground);
            setRectGray(!bIsVisible, rectCh2ScaleMinus, btnCh2ScaleMinus.Foreground);
        }

        private void ClearDataArrays()
        {
            ClearDataArrays(0);
            idxData = 0;
        }

        private void ClearDataArrays(int idxStart)
        {
            Array.Clear(dataScope1, idxStart, dataScope1.Length - idxStart);
            Array.Clear(dataScope2, idxStart, dataScope1.Length - idxStart);
        }

        private void btnCh1HorzScalePlus_Click(object sender, RoutedEventArgs e)
        {
            --hcHelper.iHorzDivIdx;
            ClearDataArrays();
            UpdateHorzDiv();
        }

        private void btnCh1HorzScaleMinus_Click(object sender, RoutedEventArgs e)
        {
            ++hcHelper.iHorzDivIdx;
            ClearDataArrays();
            UpdateHorzDiv();
        }

        private void btnHorzOffsetLeft_Click(object sender, RoutedEventArgs e)
        {
            hcHelper.iHorzPosIdx = hcHelper.iHorzPosIdx - ( Convert.ToInt32(hcHelper.iGetCRTDataLength()) >> 7 );
            UpdateHorzPos();
        }

        private void btnHorzOffsetRight_Click(object sender, RoutedEventArgs e)
        {
            hcHelper.iHorzPosIdx = hcHelper.iHorzPosIdx + (Convert.ToInt32(hcHelper.iGetCRTDataLength()) >> 7);
            UpdateHorzPos();
        }

        private void btnHorzToZero_Click(object sender, RoutedEventArgs e)
        {
            hcHelper.iHorzPosIdx = 0;
            UpdateHorzPos();
        }

        private void btnCh1_Click(object sender, RoutedEventArgs e)
        {
            bTrace1Active = !bTrace1Active;
            UpdateTraces();
        }

        private void btnCh1OffsetPlus_Click(object sender, RoutedEventArgs e)
        {
            if( bTrace1Active)
            {
                ++vcHelper.fCh1VertOffset;
                graphScope1.setCh1VertOffset(vcHelper.fCh1VertOffset);
                UpdateVertTicks();

            }
        }

        private void btnCh1OffsetMinus_Click(object sender, RoutedEventArgs e)
        {
            if( bTrace1Active)
            {
                --vcHelper.fCh1VertOffset;
                graphScope1.setCh1VertOffset(vcHelper.fCh1VertOffset);
                UpdateVertTicks();
            }
        }

        private void btnCh1ScalePlus_Click(object sender, RoutedEventArgs e)
        {
            if (bTrace1Active)
            {
                --vcHelper.iCh1VertDivIdx;
                bUpdateScopeParams();
            }
        }

        private void btnCh1ScaleMinus_Click(object sender, RoutedEventArgs e)
        {
            if (bTrace1Active)
            {
                ++vcHelper.iCh1VertDivIdx;
                bUpdateScopeParams();
            }
        }


        private void btnCh2_Click(object sender, RoutedEventArgs e)
        {
            bTrace2Active = !bTrace2Active;
            UpdateTraces();
        }

        private void btnCh2OffsetPlus_Click_1(object sender, RoutedEventArgs e)
        {
            if(bTrace2Active)
            {
                ++vcHelper.fCh2VertOffset;
                graphScope1.setCh2VertOffset(vcHelper.fCh2VertOffset);
                UpdateVertTicks();
            }
        }

        private void btnCh2OffsetMinus_Click(object sender, RoutedEventArgs e)
        {
            if(bTrace2Active)
            {
                --vcHelper.fCh2VertOffset;
                graphScope1.setCh2VertOffset(vcHelper.fCh2VertOffset);
                UpdateVertTicks();
            }
        }

        private void btnCh2ScalePlus_Click(object sender, RoutedEventArgs e)
        {
            if (bTrace2Active)
            {
                --vcHelper.iCh2VertDivIdx;
                bUpdateScopeParams();
            }
        }

        private void btnCh2ScaleMinus_Click(object sender, RoutedEventArgs e)
        {
            if (bTrace2Active)
            {
                ++vcHelper.iCh2VertDivIdx;
                bUpdateScopeParams();
            }
        }

        private void btnTriggerLevelPlus_Click(object sender, RoutedEventArgs e)
        {
            switch (tHelper.Source)
            {
                case TriggerSource.Ch1:
                    tHelper.fTriggerLevel_V = tHelper.fTriggerLevel_V + (vcHelper.fGetCh1VertDiv_V() / 4);
                    break;
                case TriggerSource.Ch2:
                    tHelper.fTriggerLevel_V = tHelper.fTriggerLevel_V + (vcHelper.fGetCh2VertDiv_V() / 4);
                    break;
                case TriggerSource.Ext:
                    tHelper.fTriggerLevel_V = tHelper.fTriggerLevel_V + 0.1f;
                    break;

            }
            UpdateTriggerUI();
        }

        private void btnTriggerLevelMinus_Click(object sender, RoutedEventArgs e)
        {
            switch (tHelper.Source)
            {
                case TriggerSource.Ch1:
                    tHelper.fTriggerLevel_V = tHelper.fTriggerLevel_V - (vcHelper.fGetCh1VertDiv_V() / 4);
                    break;
                case TriggerSource.Ch2:
                    tHelper.fTriggerLevel_V = tHelper.fTriggerLevel_V - (vcHelper.fGetCh2VertDiv_V() / 4);
                    break;
                case TriggerSource.Ext:
                    tHelper.fTriggerLevel_V = tHelper.fTriggerLevel_V - 0.1f;
                    break;

            }
            UpdateTriggerUI();
        }

        private void btnTriggerMode_Click(object sender, RoutedEventArgs e)
        {

            tHelper.Mode = tHelper.NextMode();
            UpdateTriggerUI();

        }

        private void btnTriggerSource_Click(object sender, RoutedEventArgs e)
        {

            tHelper.Source = tHelper.NextSource();
            UpdateTriggerUI();

        }

        private void btnTriggerSlope_Click(object sender, RoutedEventArgs e)
        {
            tHelper.Slope = tHelper.NextSlope();
            UpdateTriggerUI();
        }


        private void ResetLEDs()
        {
            rectFrameOK.Fill = new SolidColorBrush(Colors.Black);
            rectBTOK.Fill = new SolidColorBrush(Colors.Black);
            rectFrameSequence.Fill = new SolidColorBrush(Colors.Black);

        }

        private void ResetBuffers()
        {
            Array.Clear(dataScope1, 0, dataScope1.Length);
            Array.Clear(dataScope2, 0, dataScope2.Length);
            idxData = 0;
            UpdateTraces();
        }

        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private void UpdateAcquisitionUI()
        {
            if( bCollectData)
            {
                btnStartAcq.Content = "Stop Acquisition";
                ResetLEDs();
                ResetBuffers();
                this.textOutput.Text = "";
            }
            else
            {
                btnStartAcq.Content = "Start Acquisition";
            }
        }

        private async void btnStartAcq_Click(object sender, RoutedEventArgs e)
        {

            // Toggle the data acquisition state and update the controls
            bCollectData = !bCollectData;

            UpdateAcquisitionUI();

            if (bCollectData)
            {

                // Arduino bluetooth
                if ( btHelper.State != BluetoothConnectionState.Connected )
                {
                    // Displays a PopupMenu above the ConnectButton - uses debug window
                    await btHelper.EnumerateDevicesAsync(GetElementRect((FrameworkElement)sender));
                    if( btHelper.State == BluetoothConnectionState.Disconnected)
                    {
                        textOutput.Text = btHelper.strException;
                        bCollectData = false;
                        UpdateAcquisitionUI();
                        return;
                    }
                    await btHelper.ConnectToServiceAsync();
                    textOutput.Text = btHelper.strException;
                    if (btHelper.State == BluetoothConnectionState.Connected)
                    {
                        ReadData();
                    }

                    // Update with any errors
                    textOutput.Text = btHelper.strException;

                }

            }

        }

        #endregion

        #region private fields

        // The socket we'll use to pull data from the the Aurduino
        private BluetoothHelper btHelper = new BluetoothHelper();

        // graphs, controls, and variables related to plotting
        ScopeUIHelper uihelper;
        VisualizationTools.LineGraph graphScope1;
        float[] dataScope1;
        float fScope1ScaleADC;
        bool bTrace1Active;
        float[] dataScope2;
        float fScope2ScaleADC;
        bool bTrace2Active;
        float[] dataNull;
        uint iCRTDataStart;
        uint iCRTDataEnd;
        VerticalControlHelper vcHelper;
        HorizontalControlHelper hcHelper;
        TriggerHelper tHelper;

        // Buffer and controls for the data from the instrumentation
        private bool bCollectData;
        UInt32 iStreamBuffLength;
        uint iChannelCount;
        uint iStreamSampleCount;
        uint iShortCount;
        uint iFrameSize;
        uint idxData;
        uint idxData_MinCount;
        uint idxCharCount;
        byte byteAddress;
        UInt16[] iUnsignedShortArray;

        // Data bus structures used to pull data off of the Arduino
        DataBus.MinSegBus mbus;

        #endregion




    }
}