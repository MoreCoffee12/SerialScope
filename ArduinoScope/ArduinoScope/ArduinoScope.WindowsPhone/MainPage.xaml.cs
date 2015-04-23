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


namespace ArduinoScope
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
            uihelper.CRTMargin_Vert = 50;
            uihelper.CRTMargin_Horz = 25;
            vcHelper = new VerticalControlHelper();
            hcHelper = new HorizontalControlHelper();
            hcHelper.iDivisionCount = ScopeGrid.ColumnDefinitions.Count;
            tHelper = new TriggerHelper();

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
            idxCharCount = 0;
            byteAddress = 0;
            iUnsignedShortArray = new UInt16[iShortCount];

            // Data buffers
            iScopeDataLength = hcHelper.iGetScopeDataLength();
            dataScope1 = new float[iScopeDataLength];
            dataScope2 = new float[iScopeDataLength];
            dataNull = new float[iScopeDataLength];
            for (int idx = 0; idx < iScopeDataLength; idx++)
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
            graphScope1.setMarkIndex(Convert.ToInt32(iScopeDataLength / 2));
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            double dCRT_Vert = _CRTRow.ActualHeight - uihelper.CRTMargin_Vert;
            double dCRT_Horz = CRTGrid.ActualWidth - uihelper.CRTMargin_Horz;

            LineGraphScope1.Width = dCRT_Horz-1;
            LineGraphScope1.Height = dCRT_Vert;
            
            ScopeGrid.Width = dCRT_Horz-1;
            ScopeGrid.Height = dCRT_Vert;

            // Initialize the UI
            uihelper.Initialize(ScopeGrid,
            tbCh1VertDiv, tbCh1VertDivValue, tbCh1VertDivEU,
            tbCh2VertDiv, tbCh2VertDivValue, tbCh2VertDivEU,
            rectCh1button, rectCh2button,
            tbCh1VertTick, tbCh2VertTick);

            // Initialize the buffer for the frame timebase and set the color
            graphScope1.setColor(Convert.ToSingle(uihelper.clrTrace1.R) / 255.0f, Convert.ToSingle(uihelper.clrTrace1.G) / 255.0f, Convert.ToSingle(uihelper.clrTrace1.B) / 255.0f, 0.0f, 0.5f + uihelper.fOffset, 0.5f + uihelper.fOffset);
            graphScope1.setColorBackground(uihelper.fR, uihelper.fG, uihelper.fB, 0.0f);

            // Configure the line plots scaling based on the grid
            bUpdateScopeParams();
            bUpdateDateTime();

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

        private bool bUpdateDateTime()
        {
            // Get the current date.
            DateTime thisDay = DateTime.Today;

            tbDateTime.Text = thisDay.ToString("d-MMM-yy");

            return true;
        }

        private bool bUpdateScopeParams()
        {
            // Set the limits of the graph in the image
            graphScope1.setYLim(0.0f, Convert.ToSingle(uihelper.iGridRowCount));

            // Set the individual trace scale factors
            graphScope1.setCh1Scale( 1.0f/vcHelper.fGetCh1VertDiv_V());
            graphScope1.setCh2Scale(1.0f / vcHelper.fGetCh2VertDiv_V());

            // Update the scope vertical divisions scale factor
            if( vcHelper.fGetCh1VertDiv_V() < 1.0f)
            {
                tbCh1VertDivValue.Text = (vcHelper.fGetCh1VertDiv_V() * 1000.0f).ToString("F0", CultureInfo.InvariantCulture);
                tbCh1VertDivEU.Text = "mV";
            }
            else
            {
                tbCh1VertDivValue.Text = vcHelper.fGetCh1VertDiv_V().ToString("F2", CultureInfo.InvariantCulture);
                tbCh1VertDivEU.Text = "V";
            }
            if (vcHelper.fGetCh2VertDiv_V() < 1.0f)
            {
                tbCh2VertDivValue.Text = (vcHelper.fGetCh2VertDiv_V() * 1000.0f).ToString("F0", CultureInfo.InvariantCulture);
                tbCh2VertDivEU.Text = "mV";
            }
            else
            {
                tbCh2VertDivValue.Text = vcHelper.fGetCh2VertDiv_V().ToString("F2", CultureInfo.InvariantCulture);
                tbCh2VertDivEU.Text = "V";
            }

            // Update the horizontal divisions
            UpdateHorzDiv();

            // Update vertical tick markers
            UpdateVertTicks();

            // Update trigger status
            UpdateTrigger();

            return true;
        }

        private void UpdateTrigger()
        {
            txtTriggerMode.Text = tHelper.TriggerModeText();
            btnTriggerMode.Content = tHelper.TriggerModeText();

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
            }


        }

        private void UpdateHorzDiv()
        {
            // Update the horizontal divisions
            float fDivRaw = hcHelper.fGetDivRaw();
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

        }

        private void UpdateVertTicks()
        {
            int iVert = 0;
            float fCRTUpper = Convert.ToSingle(uihelper.CRTMargin_Vert / 2);
            float fScopeLimits = graphScope1.getYLimMax() - graphScope1.getYLimMin();

            if( fScopeLimits < 1e-6)
            {
                return;
            }

            if (tbCh1VertTick.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                tbCh1VertTick.Text = "1→";
                iVert = Convert.ToInt32((Convert.ToSingle(uihelper.iGridRowCount) - vcHelper.fCh1VertOffset) * (Convert.ToSingle(LineGraphScope1.Height) / fScopeLimits));

                if (iVert > Convert.ToInt32(LineGraphScope1.Height))
                {
                    iVert = Convert.ToInt32(LineGraphScope1.Height);
                    tbCh1VertTick.Text = "1↓";
                }
                if (iVert < 0)
                {
                    iVert = 0;
                    tbCh1VertTick.Text = "1↑";
                }

                iVert = iVert + Convert.ToInt32(fCRTUpper - Convert.ToSingle(tbCh1VertTick.ActualHeight / 2));
                tbCh1VertTick.Margin = new Thickness(0, iVert, 0, 0);
            }



            if (tbCh2VertTick.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                tbCh2VertTick.Text = "2→";
                iVert = Convert.ToInt32((Convert.ToSingle(uihelper.iGridRowCount) - vcHelper.fCh2VertOffset) * (Convert.ToSingle(LineGraphScope1.Height) / fScopeLimits));
                if (iVert > Convert.ToInt32(LineGraphScope1.Height))
                {
                    iVert = Convert.ToInt32(LineGraphScope1.Height);
                    tbCh2VertTick.Text = "2↓";
                }
                if (iVert < 0)
                {
                    iVert = 0;
                    tbCh2VertTick.Text = "2↑";
                }

                iVert = iVert + Convert.ToInt32(fCRTUpper - Convert.ToSingle(tbCh1VertTick.ActualHeight / 2));
                tbCh2VertTick.Margin = new Thickness(0, iVert, 0, 0);
            }


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

                    // Point to the next location in the buffer
                    iNextDataIdx();

                    // Fill the spaces in the buffer with data
                    dataScope1[idxData] = Convert.ToSingle(iUnsignedShortArray[0]) * fScope1ScaleADC;
                    dataScope2[idxData] = Convert.ToSingle(iUnsignedShortArray[1]) * fScope2ScaleADC;

                    iNextDataIdx();

                    // Fill the spaces in the buffer with data
                    dataScope1[idxData] = Convert.ToSingle(iUnsignedShortArray[2]) * fScope1ScaleADC;
                    dataScope2[idxData] = Convert.ToSingle(iUnsignedShortArray[3]) * fScope2ScaleADC;

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

        private void iNextDataIdx()
        {
            ++idxData;
            idxData = idxData % hcHelper.iGetCRTDataLength();
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

                    // Append that line to our TextOutput
                    try
                    {
                        if (bCollectData)
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

        private void UpdateTraces()
        {

            // Calculate the portion of data to display
            iCRTDataStart = 0;
            iCRTDataEnd = hcHelper.iGetCRTDataLength();

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
            Array.Clear(dataScope1, 0, dataScope1.Length);
            Array.Clear(dataScope2, 0, dataScope2.Length);
            idxData = 0;
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

        private void btnCh1_Click(object sender, RoutedEventArgs e)
        {
            bTrace1Active = !bTrace1Active;
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

        private void btnTriggerMode_Click(object sender, RoutedEventArgs e)
        {

            if( tHelper.Mode == TriggerMode.Normal)
            {
                tHelper.Mode = TriggerMode.Scan;
            }
            else
            {
                tHelper.Mode = TriggerMode.Normal;
            }

            UpdateTrigger();

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

        private async void btnStartAcq_Click(object sender, RoutedEventArgs e)
        {

            // Toggle the data acquisition state and update the controls
            bCollectData = !bCollectData;

            if (bCollectData)
            {

                btnStartAcq.Content = "Stop Acquisition";
                ResetLEDs();
                ResetBuffers();
                this.textOutput.Text = "";

                // Arduino bluetooth
                if ( btHelper.State != BluetoothConnectionState.Connected )
                {
                    // Displays a PopupMenu above the ConnectButton - uses debug window
                    await btHelper.EnumerateDevicesAsync(GetElementRect((FrameworkElement)sender));
                    textOutput.Text = btHelper.strException;
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
            else
            {

                textOutput.Text = "";
                btnStartAcq.Content = "Start Acquisition";
                textOutput.Text = btHelper.strException;

            }


        }

        #endregion

        #region private fields

        // The socket we'll use to pull data from the the Aurduino
        private BluetoothHelper btHelper = new BluetoothHelper();

        // graphs, controls, and variables related to plotting
        ScopeUIHelper uihelper;
        VisualizationTools.LineGraph graphScope1;
        uint iScopeDataLength;
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
        uint idxCharCount;
        byte byteAddress;
        UInt16[] iUnsignedShortArray;

        // Data bus structures used to pull data off of the Arduino
        DataBus.MinSegBus mbus;

        #endregion


    }
}