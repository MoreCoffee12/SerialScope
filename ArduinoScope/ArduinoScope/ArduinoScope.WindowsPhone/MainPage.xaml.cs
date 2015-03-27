using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Graphics.Display;
using VisualizationTools;
using DataBus;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

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

            // This is a royal pain...since in XAML the "auto" width is always a NaN in C#,
            // you have to grab the actual device width and then manually set the width to
            // match that of the device.  In effect, you have to hard code the "auto" value.
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            LineGraphScope1.Height = 95;
            LineGraphScope1.Width = Windows.UI.Xaml.Window.Current.Bounds.Width;

            // Create our LineGraph and hook the up to their respective images
            graphScope1 = new VisualizationTools.LineGraph((int)LineGraphScope1.Width, (int)LineGraphScope1.Height);
            LineGraphScope1.Source = graphScope1;

            // Initialize the buffers that recieve the data from the Arduino
            bCollectData = false;
            iChannelCount = 2;
            iBuffLength = 1024;
            iBuffStart = -1;
            iBuffData = new int[iChannelCount * iBuffLength];
            Array.Clear(iBuffData, 0, iBuffData.Length);
            idxData = 0;
            cBuff = new byte[iBuffLength];
            Array.Clear(cBuff, 0, cBuff.Length);
            iPreLoad = 48;
            idxData = 0;

            // Data buffers
            dataScope1 = new float[iBuffLength];

            // Update the plots
            graphScope1.setArray(dataScope1);

        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            float fOffset = 0.0f;

            // Retrieve the phone theme settings so that the plots can 
            // be tailored to match
            Color colorCurrentBackground = (Color)Application.Current.Resources["PhoneBackgroundColor"];
            float fR = Convert.ToSingle(colorCurrentBackground.R) / 255.0f;
            float fG = Convert.ToSingle(colorCurrentBackground.G) / 255.0f;
            float fB = Convert.ToSingle(colorCurrentBackground.B) / 255.0f;
            if (fR + fG + fB < 1.5)
            {
                fOffset = 0.5f;
            }

            // Initialize the buffer for the frame timebase and set the color
            graphScope1.setColor(0.0f, 0.5f + fOffset, 0.0f);
            graphScope1.setColorBackground(fR, fG, fB, 0.0f);

            // Configure the line plots scaling
            graphScope1.setYLim(0.0f, 51.0f);

            // Also hook the Rendering cycle up to the CompositionTarget Rendering event so we draw frames when we're supposed to
            CompositionTarget.Rendering += graphScope1.Render;
        }

        # region Private Methods

        private void btnStartAcq_Click(object sender, RoutedEventArgs e)
        {

            // Toggle the data acquisition state and update the controls
            bCollectData = !bCollectData;

            if (bCollectData)
            {
                btnStartAcq.Content = "Stop Acquisition";
            }
            else
            {
                btnStartAcq.Content = "Start Acquisition";
            }

            bClearOutput = true;

        }

        #endregion

        #region private fields

        // graphs, controls, and variables related to plotting
        private bool bClearOutput = false;
        private VisualizationTools.LineGraph graphScope1;
        private float[] dataScope1;

        // Buffer and controls for the data from the instrumentation
        private bool bCollectData;
        uint iBuffLength;
        byte[] cBuff;
        uint iChannelCount;
        int iBuffStart;
        int[] iBuffData;
        uint idxData;

        // Data bus structures used to pull data off of the Arduino
        DataBus.MinSegBus mbus;
        uint iPreLoad;

        #endregion

    }
}
