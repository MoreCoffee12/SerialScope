using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace ArduinoScope
{
    public class ScopeUIHelper
    {

        #region Public Methods

        public ScopeUIHelper()
        {
            colorCurrentBackground = (Color)Application.Current.Resources["PhoneBackgroundColor"];
            colorCurrentForeground = (Color)Application.Current.Resources["PhoneForegroundColor"];

            // Retrieve the phone theme settings so that the plots can 
            // be tailored to match
            fR = Convert.ToSingle(colorCurrentBackground.R) / 255.0f;
            fG = Convert.ToSingle(colorCurrentBackground.G) / 255.0f;
            fB = Convert.ToSingle(colorCurrentBackground.B) / 255.0f;
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

            // Default field values
            iGridRowCount = 0;
            iGridColCount = 0;

        }

        // Helper function to plot the lines on the scope grid
        public void addScopeGridLine(Grid ScopeGrid, double X1, double Y1, double X2, double Y2,
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

        public void Initialize(Grid ScopeGrid,
            TextBlock tbCh1VertDiv, TextBlock tbCh1VertDivValue, TextBlock tbCh1VertDivEU,
            TextBlock tbCh2VertDiv, TextBlock tbCh2VertDivValue, TextBlock tbCh2VertDivEU,
            Rectangle rectCh1button)
        {
            // Features from the grid, defined in XAML
            iGridRowCount = ScopeGrid.RowDefinitions.Count;
            iGridColCount = ScopeGrid.ColumnDefinitions.Count;
            double dGridWidth = ScopeGrid.Width;
            double dGridCellWidth = dGridWidth / Convert.ToDouble(iGridColCount);
            double dGridHeight = ScopeGrid.Height;
            double dGridCellHeight = dGridHeight / Convert.ToDouble(iGridRowCount);
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
                    addScopeGridLine(ScopeGrid, 0, 0, dGridWidth, 0,
                        colorCurrentForeground, 2, iRows, 0, 1, iGridColCount);
                }
                else
                {
                    addScopeGridLine(ScopeGrid, 0, 0, dGridWidth, 0,
                        colorCurrentForeground, 1, iRows, 0, 1, iGridColCount);

                }
            }
            addScopeGridLine(ScopeGrid, 0, dGridCellHeight, dGridWidth, dGridCellHeight,
                colorCurrentForeground, 1, iGridRowCount, 0, 1, iGridColCount);

            // Render the vertical lines for the oscilliscope screen
            for (int iCols = 0; iCols < iGridColCount; iCols++)
            {
                if (iCols == iGridColCount / 2)
                {
                    addScopeGridLine(ScopeGrid, 0, 0, 0, dGridHeight,
                        colorCurrentForeground, 2, 0, iCols, iGridRowCount, 1);
                }
                else
                {
                    addScopeGridLine(ScopeGrid, 0, 0, 0, dGridHeight,
                        colorCurrentForeground, 1, 0, iCols, iGridRowCount, 1);
                }
            }
            addScopeGridLine(ScopeGrid, dGridCellWidth, 0, dGridCellWidth, dGridHeight,
                colorCurrentForeground, 1, 0, iGridColCount, iGridRowCount, 1);

            // Render vertical controls
            rectCh1button.Fill = new SolidColorBrush(clrTrace1);


        }
        #endregion

        #region Access Methods

        public Color colorCurrentBackground
        {
            get
            {
                return _colorCurrentBackground;
            }

            set
            {
                _colorCurrentBackground = value;
            }
        }

        public Color colorCurrentForeground
        {
            get
            {
                return _colorCurrentForeground;
            }
            set
            {
                _colorCurrentForeground = value;
            }
        }

        public float fR
        {
            get
            {
                return _fR;
            }
            set
            {
                _fR = value;
            }
        }

        public float fG
        {
            get
            {
                return _fG;
            }
            set
            {
                _fG = value;
            }
        }

        public float fB
        {
            get
            {
                return _fB;
            }
            set
            {
                _fB = value;
            }
        }

        public float fOffset
        {
            get
            {
                return _fOffset;
            }
            set
            {
                _fOffset = value;
            }
        }

        public Color clrTrace1
        {
            get
            {
                return _clrTrace1;
            }
            set
            {
                _clrTrace1 = value;
            }
        }

        public Color clrTrace2
        {
            get
            {
                return _clrTrace2;
            }
            set
            {
                _clrTrace2 = value;
            }
        }

        public int iGridRowCount
        {
            get
            {
                return _iGridRowCount;
            }
            set
            {
                _iGridRowCount = value;
            }
        }

        public int iGridColCount
        {
            get
            {
                return _iGridColCount;
            }
            set
            {
                _iGridColCount = value;
            }
        }

        #endregion

        #region Private Fields

        int _iGridRowCount;
        int _iGridColCount;

        private Color _colorCurrentBackground;
        private Color _colorCurrentForeground;

        private float _fR;
        private float _fG;
        private float _fB;
        private float _fOffset;

        private Color _clrTrace1;
        private Color _clrTrace2;

        #endregion

    }
}