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

            // The sampling frequency here must match that configured in the Arduino firmware
            fSamplingFreq_Hz = 500;

            // Initialize the data bus
            mbus = new MinSegBus();

            // Initialize the buffers that recieve the data from the Arduino
            bCollectData = false;
            iChannelCount = 2;
            iStreamSampleCount = 2;
            iShortCount = iChannelCount * iStreamSampleCount;
            iFrameSize = mbus.iGetFrameCount_Short(iShortCount);
            iBuffLength = 1000;
            iBuffData = new int[iChannelCount * iBuffLength];
            Array.Clear(iBuffData, 0, iBuffData.Length);
            idxData = 0;
            iStreamBuffLength = 128;
            idxData = 0;
            idxCharCount = 0;
            byteAddress = 0;
            iUnsignedShortArray = new UInt16[iShortCount];

            // Data buffers
            dataScope1 = new float[iBuffLength];
            dataScope2 = new float[iBuffLength];
            dataNull = new float[iBuffLength];
            for (int idx = 0; idx < iBuffLength; idx++)
            {
                dataScope1[idx] = Convert.ToSingle(1.0 + Math.Sin(Convert.ToDouble(idx) * (2.0 * Math.PI / Convert.ToDouble(iBuffLength))));
                dataScope2[idx] = Convert.ToSingle(1.0 - Math.Sin(Convert.ToDouble(idx) * (2.0 * Math.PI / Convert.ToDouble(iBuffLength))));
                dataNull[idx] = -100.0f;
            }

            // Scale factors
            fScope1Scale = 5.0f / 1024.0f;
            fScope2Scale = 5.0f / 1024.0f;

            // Default scope parameters
            fDivVert1 = 1.0f;
            fDivVert2 = 1.0f;
            bTrace1Active = true;
            bTrace2Active = true;

            // Update scope
            UpdateTraces();
            graphScope1.setMarkIndex(Convert.ToInt32(iBuffLength / 2));
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            double dCRT_Horz = CRTGrid.ActualWidth-25;
            double dCRT_Vert = CRTGrid.ActualHeight-50;

            LineGraphScope1.Width = dCRT_Horz;
            LineGraphScope1.Height = dCRT_Vert;
            
            ScopeGrid.Width = dCRT_Horz;
            ScopeGrid.Height = dCRT_Vert;

            // Initialize the UI
            uihelper.Initialize(ScopeGrid,
            tbCh1VertDiv, tbCh1VertDivValue, tbCh1VertDivEU,
            tbCh2VertDiv, tbCh2VertDivValue, tbCh2VertDivEU,
            rectCh1button);

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
            // Set the limits of the graph correctly
            graphScope1.setYLim(0.0f, Convert.ToSingle(uihelper.iGridRowCount));

            // Update the scope vertical divisions scale factor
            tbCh1VertDivValue.Text = fDivVert1.ToString("F2", CultureInfo.InvariantCulture);
            tbCh2VertDivValue.Text = fDivVert2.ToString("F2", CultureInfo.InvariantCulture);

            // Update the horizontal divisions
            float fDivRaw = Convert.ToSingle(iBuffLength) / (fSamplingFreq_Hz * Convert.ToSingle(uihelper.iGridColCount));
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


            return true;
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
                    ++idxData;
                    idxData = idxData % iBuffLength;

                    // Fill the spaces in the buffer with data
                    dataScope1[idxData] = Convert.ToSingle(iUnsignedShortArray[0]) * fScope1Scale;
                    dataScope2[idxData] = Convert.ToSingle(iUnsignedShortArray[1]) * fScope2Scale;

                    ++idxData;
                    idxData = idxData % iBuffLength;

                    // Fill the spaces in the buffer with data
                    dataScope1[idxData] = Convert.ToSingle(iUnsignedShortArray[2]) * fScope1Scale;
                    dataScope2[idxData] = Convert.ToSingle(iUnsignedShortArray[3]) * fScope2Scale;

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

        private async void ReadData()
        {
            bool bTemp;

            // Construct a dataReader so we can read junk in
            btHelper.input = new DataReader(btHelper.s.InputStream);

            // Made it this far so status is ok
            rectBTOK.Fill = new SolidColorBrush(Colors.Green);
            //rectFrameSequence.Fill = new SolidColorBrush(Colors.Green);

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
            // Update the plots
            if (bTrace1Active && bTrace2Active)
            {
                graphScope1.setArray(dataScope1, dataScope2);
            }
            if (bTrace1Active && !bTrace2Active)
            {
                graphScope1.setArray(dataScope1, dataNull);
            }
            if (!bTrace1Active && bTrace2Active)
            {
                graphScope1.setArray(dataNull, dataScope2);
            }
            if (!bTrace1Active && !bTrace2Active)
            {
                graphScope1.setArray(dataNull, dataNull);
            }

        }

        private void btnCh1_Click(object sender, RoutedEventArgs e)
        {
            bTrace1Active = !bTrace1Active;
        }

        private void btnCh2_Click(object sender, RoutedEventArgs e)
        {
            bTrace2Active = !bTrace2Active;
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
                try
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
                }
                catch (Exception ex)
                {
                    this.textOutput.Text = "Exception:  " + ex.ToString();
                }

                // Update with any errors
                textOutput.Text = btHelper.strException;

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
        private VisualizationTools.LineGraph graphScope1;
        private float[] dataScope1;
        private float fScope1Scale;
        private float fDivVert1;
        private bool bTrace1Active;
        private float[] dataScope2;
        private float fScope2Scale;
        private float fDivVert2;
        private bool bTrace2Active;
        private float[] dataNull;

        // Buffer and controls for the data from the instrumentation
        private float fSamplingFreq_Hz;
        private bool bCollectData;
        uint iBuffLength;
        UInt32 iStreamBuffLength;
        uint iChannelCount;
        uint iStreamSampleCount;
        uint iShortCount;
        uint iFrameSize;
        int[] iBuffData;
        uint idxData;
        uint idxCharCount;
        byte byteAddress;
        UInt16[] iUnsignedShortArray;

        // Data bus structures used to pull data off of the Arduino
        DataBus.MinSegBus mbus;

        #endregion



    }
}