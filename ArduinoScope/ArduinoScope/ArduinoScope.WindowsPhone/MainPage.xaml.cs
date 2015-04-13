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

            this.NavigationCacheMode = NavigationCacheMode.Required;

            // Grab the actual device width and then manually set the width to
            // match that of the device.
            if (Double.IsNaN(LineGraphScope1.Width))
            {
                var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                LineGraphScope1.Height = 95;
                LineGraphScope1.Width = Windows.UI.Xaml.Window.Current.Bounds.Width;
            }

            // Create our LineGraph and hook the up to their respective images
            graphScope1 = new VisualizationTools.LineGraph((int)LineGraphScope1.Width, (int)LineGraphScope1.Height);
            LineGraphScope1.Source = graphScope1;

            // The sampling frequency here must match that configured in the Arduino firmware
            fSamplingFreq_Hz = 500;

            // Initialize the data bus
            mbus = new MinSegBus();

            // Initialize the buffers that recieve the data from the Arduino
            bCollectData = false;
            iChannelCount = 2;
            iBuffLength = 1000;
            iBuffData = new int[iChannelCount * iBuffLength];
            Array.Clear(iBuffData, 0, iBuffData.Length);
            idxData = 0;
            iStreamBuffLength = 128;
            idxData = 0;
            byteAddress = 0;
            iUnsignedShortArray = new UInt16[4];

            // Data buffers
            dataScope1 = new float[iBuffLength];
            dataScope2 = new float[iBuffLength];
            for (int idx = 0; idx < iBuffLength; idx++)
            {
                dataScope1[idx] = Convert.ToSingle(1.0 + Math.Sin(Convert.ToDouble(idx) * (2.0 * Math.PI / 1024.0)));
                dataScope2[idx] = Convert.ToSingle(1.0 - Math.Sin(Convert.ToDouble(idx) * (2.0 * Math.PI / 1024.0)));
            }

            // Scale factors
            fScope1Scale = 5.0f / 1024.0f;
            fScope2Scale = 5.0f / 1024.0f;

            // Default scope parameters
            fDivVert1 = 1.0f;
            fDivVert2 = 1.0f;

            // Update scope
            graphScope1.setArray(dataScope1, dataScope2);
            graphScope1.setMarkIndex(Convert.ToInt32(iBuffLength / 2));
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            float fOffset = 0.0f;

            // Reset the debug window
            this.textOutput.Text = "";

            // Control appearance features
            Color colorCurrentBackground = (Color)Application.Current.Resources["PhoneBackgroundColor"];
            Color colorCurrentForeground = (Color)Application.Current.Resources["PhoneForegroundColor"];

            // Retrieve the phone theme settings so that the plots can 
            // be tailored to match
            float fR = Convert.ToSingle(colorCurrentBackground.R) / 255.0f;
            float fG = Convert.ToSingle(colorCurrentBackground.G) / 255.0f;
            float fB = Convert.ToSingle(colorCurrentBackground.B) / 255.0f;
            if (fR + fG + fB < 1.5)
            {
                fOffset = 0.5f;
            }

            // Initilize colors for the traces
            clrTrace1 = new Color();
            byte btRed = 0;
            byte btGreen = Convert.ToByte((0.5f + fOffset) * 255.0f);
            byte btBlue = 0;
            clrTrace1 = Color.FromArgb(255, btRed, btGreen, btBlue);

            clrTrace2 = new Color();
            btRed = 0;
            btGreen = Convert.ToByte((0.5f + fOffset) * 255.0f);
            btBlue = Convert.ToByte((0.5f + fOffset) * 255.0f);
            clrTrace2 = Color.FromArgb(255, btRed, btGreen, btBlue);


            // Initialize the buffer for the frame timebase and set the color
            graphScope1.setColor(Convert.ToSingle(clrTrace1.R) / 255.0f, Convert.ToSingle(clrTrace1.G) / 255.0f, Convert.ToSingle(clrTrace1.B) / 255.0f, 0.0f, 0.5f + fOffset, 0.5f + fOffset);
            graphScope1.setColorBackground(fR, fG, fB, 0.0f);

            // Features from the grid, defined in XAML
            iGridRowCount = ScopeGrid.RowDefinitions.Count();
            iGridColCount = ScopeGrid.ColumnDefinitions.Count();
            SolidColorBrush Brush1 = new SolidColorBrush();
            Brush1.Color = clrTrace1;
            tbCh1VertDiv.Foreground = Brush1;
            tbCh1VertDivValue.Foreground = Brush1;
            tbCh1VertDivEU.Foreground = Brush1;

            SolidColorBrush Brush2 = new SolidColorBrush();
            Brush2.Color = clrTrace2;
            tbCh2VertDiv.Foreground = Brush2;
            tbCh2VertDivValue.Foreground = Brush2;
            tbCh2VertDivEU.Foreground = Brush2;

            // Render the horizontal lines for the oscilliscope screen
            for (int iRows = 0; iRows < iGridRowCount; iRows++)
            {
                if (iRows == iGridRowCount / 2)
                {
                    addScopeGridLine(ScopeGrid, 0, 0, 300, 0,
                        colorCurrentForeground, 2, iRows, 0, 1, iGridColCount);
                }
                else
                {
                    addScopeGridLine(ScopeGrid, 0, 0, 300, 0,
                        colorCurrentForeground, 1, iRows, 0, 1, iGridColCount);

                }
            }
            addScopeGridLine(ScopeGrid, 0, 25, 300, 25,
                colorCurrentForeground, 1, iGridRowCount, 0, 1, iGridColCount);

            // Render the vertical lines for the oscilliscope screen
            for (int iCols = 0; iCols < iGridColCount; iCols++)
            {
                if (iCols == iGridColCount / 2)
                {
                    addScopeGridLine(ScopeGrid, 0, 0, 0, 300,
                        colorCurrentForeground, 2, 0, iCols, iGridRowCount, 1);
                }
                else
                {
                    addScopeGridLine(ScopeGrid, 0, 0, 0, 300,
                        colorCurrentForeground, 1, 0, iCols, iGridRowCount, 1);
                }
            }
            addScopeGridLine(ScopeGrid, 25, 0, 25, 300,
                colorCurrentForeground, 1, 0, iGridColCount, iGridRowCount, 1);

            // Configure the line plots scaling based on the grid
            bUpdateScopeParams();
            bUpdateDateTime();

            // Also hook the Rendering cycle up to the CompositionTarget Rendering event so we draw frames when we're supposed to
            CompositionTarget.Rendering += graphScope1.Render;

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
            graphScope1.setYLim(0.0f, Convert.ToSingle(iGridRowCount));

            // Update the scope vertical divisions scale factor
            tbCh1VertDivValue.Text = fDivVert1.ToString("F2", CultureInfo.InvariantCulture);
            tbCh2VertDivValue.Text = fDivVert2.ToString("F2", CultureInfo.InvariantCulture);

            // Update the horizontal divisions
            float fDivRaw = Convert.ToSingle(iBuffLength) / (fSamplingFreq_Hz * Convert.ToSingle(iGridColCount));
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

        private async Task<bool> SetupBluetoothLink()
        {

            // Tell PeerFinder that we're a pair to anyone that has been paried with us over BT
            PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = "";

            // Find all peers
            System.Collections.Generic.IReadOnlyList<Windows.Networking.Proximity.PeerInformation> devices;
            try
            {
                devices = await PeerFinder.FindAllPeersAsync();
            }
            catch (Exception ex)
            {
                this.textOutput.Text = "Failed to find any Bluetooth devices.  Is your Bluetooth turned on?\n";
                this.textOutput.Text += "Exception:  " + ex.ToString();
                return false;
            }

            // If there are no peers, then complain
            if (devices.Count == 0)
            {
                await new MessageDialog("No bluetooth devices are paired, please pair your Arduino").ShowAsync();

                // Neat little line to open the bluetooth settings
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }
            this.textOutput.Text = "Found paired bluetooth devices\n";

            // Convert peers to array from strange datatype return from PeerFinder.FindAllPeersAsync()
            PeerInformation[] peers = devices.ToArray();

            // Find paired peer that is the default device name for the Arduino
            PeerInformation peerInfo = devices.FirstOrDefault(c => c.DisplayName.Contains("HC"));

            // If that doesn't exist, complain!
            if (peerInfo == null)
            {
                await new MessageDialog("No bluetooth devices are paired, please pair your HC").ShowAsync();
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }
            this.textOutput.Text += ("Found the HC bluetooth device:" + peerInfo.HostName + "\n");

            // Otherwise, create our StreamSocket and connect it!
            btHelper.s = new StreamSocket();
            try
            {
                await btHelper.s.ConnectAsync(peerInfo.HostName, "1");
            }
            catch (Exception ex)
            {
                this.textOutput.Text = "ConnectAsync Failed\n";
                this.textOutput.Text += "Exception:  " + ex.ToString();
                return false;
            }

            // At this point, open the writer
            //output = new DataWriter(s.OutputStream);
            return true;

        }

        // Read in iBuffLength number of samples
        private async Task<bool> bReadSource(DataReader input)
        {
            UInt32 k;
            byte byteInput;
            uint iErrorCount;

            // Read in the data
            for (k = 0; k < iStreamBuffLength; k++)
            {

                // Wait until we have 1 byte available to read
                await input.LoadAsync(1);

                // Read in the byte
                byteInput = input.ReadByte();

                // Save to the ring buffer and see if the frame can be parsed
                iUnsignedShortArray = mbus.writeRingBuff(byteInput, 4);
                iErrorCount = mbus.iGetErrorCount();

                if (iErrorCount == 0)
                {
                    // The frame was valid
                    rectFrameOK.Fill = new SolidColorBrush(Colors.Green);

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
                }
                else
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
                            // Update the blank space
                            graphScope1.setMarkIndex(Convert.ToInt32(idxData));

                            // Update the plots
                            graphScope1.setArray(dataScope1, dataScope2);
                        }

                    }
                    catch (Exception ex)
                    {
                        this.textOutput.Text = "Exception:  " + ex.ToString();
                    }
                }

            }

        }

        private void ResetLEDs()
        {
            rectFrameOK.Fill = new SolidColorBrush(Colors.Black);
            rectBTOK.Fill = new SolidColorBrush(Colors.Black);
            rectFrameSequence.Fill = new SolidColorBrush(Colors.Black);

        }

        private async void btnStartAcq_Click(object sender, RoutedEventArgs e)
        {

            // Toggle the data acquisition state and update the controls
            bCollectData = !bCollectData;

            if (bCollectData)
            {

                btnStartAcq.Content = "Stop Acquisition";
                ResetLEDs();
                // Reset the debug window
                this.textOutput.Text = "";

                // Arduino bluetooth
                try
                {
                    //displays a PopupMenu above the ConnectButton - uses debug window
                    Rect rect = new Rect(100, 100, 100, 100);
                    await btHelper.EnumerateDevicesAsync(rect);
                    await btHelper.ConnectToServiceAsync();
                    if(btHelper.State == BluetoothConnectionState.Connected)
                    {
                        ReadData();
                    }
                }
                catch (Exception ex)
                {
                    this.textOutput.Text = "Exception:  " + ex.ToString();
                }

            }
            else
            {
                
                btnStartAcq.Content = "Start Acquisition";
                ResetLEDs();
            }


        }

        // Helper function to plot the lines on the scope grid
        private void addScopeGridLine(Grid ScopeGrid, double X1, double Y1, double X2, double Y2,
            Color colorLineColor, int StrokeThickness, int iRow, int iCol, int iRowSpan, int iColSpan)
        {
            Line myline = new Line();
            myline.X1 = X1;
            myline.Y1 = Y1;
            myline.X2 = X2;
            myline.Y2 = Y2;
            myline.StrokeThickness = StrokeThickness;
            myline.Stroke = new SolidColorBrush(colorLineColor);
            myline.StrokeDashArray = new DoubleCollection() { 4 / StrokeThickness };
            myline.SetValue(Grid.RowProperty, iRow);
            myline.SetValue(Grid.ColumnProperty, iCol);
            myline.SetValue(Grid.RowSpanProperty, iRowSpan);
            myline.SetValue(Grid.ColumnSpanProperty, iColSpan);

            ScopeGrid.Children.Add(myline);

        }


        #endregion

        #region private fields

        // The socket we'll use to pull data from the the Aurduino
        private BluetoothHelper btHelper = new BluetoothHelper();

        // graphs, controls, and variables related to plotting
        int iGridRowCount;
        int iGridColCount;
        private VisualizationTools.LineGraph graphScope1;
        private float[] dataScope1;
        private float fScope1Scale;
        private float[] dataScope2;
        private float fScope2Scale;
        private Color clrTrace1;
        private Color clrTrace2;
        private float fDivVert1;
        private float fDivVert2;

        // Buffer and controls for the data from the instrumentation
        private float fSamplingFreq_Hz;
        private bool bCollectData;
        uint iBuffLength;
        UInt32 iStreamBuffLength;
        uint iChannelCount;
        int[] iBuffData;
        uint idxData;
        byte byteAddress;
        UInt16[] iUnsignedShortArray;

        // Data bus structures used to pull data off of the Arduino
        DataBus.MinSegBus mbus;

        #endregion

    }
}