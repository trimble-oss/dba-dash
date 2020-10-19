using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace DBAChecksGUI
{
    class Histogram
    {

        public static Bitmap GetHistogram(List<double> values,Int32 width,Int32 height,bool ColorPoints)
        {
            var chart = new Chart();
            var chartArea = new ChartArea();
            chart.ChartAreas.Add(chartArea);
            chartArea.AxisY.Enabled = AxisEnabled.False;
            chartArea.AxisY.MajorGrid.Enabled =  false;
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.LabelStyle.Enabled = false;
                 

            var s = chart.Series.Add("s1");
            s.Points.DataBindY(values);
            if (ColorPoints)
            {
                if (values.Count != 10)
                {
                    throw new Exception("Expected 10 points for histogram");
                }
                s.Points[0].Color = Color.FromArgb(0, 100, 0);
                s.Points[1].Color = Color.FromArgb(26, 126, 26);
                s.Points[2].Color = Color.FromArgb(77, 177, 77);
                s.Points[3].Color = Color.FromArgb(101, 202, 102);
                s.Points[4].Color = Color.FromArgb(153, 253, 153);
                s.Points[5].Color = Color.FromArgb(255, 165, 0);
                s.Points[6].Color = Color.FromArgb(255, 165, 0);
                s.Points[7].Color = Color.FromArgb(255, 77, 77);
                s.Points[8].Color = Color.FromArgb(255, 26, 26);
                s.Points[9].Color = Color.FromArgb(255, 0, 0);
            }

            chart.ClientSize = new Size(width, height);
            var bmp = new Bitmap(width, height);
            chart.DrawToBitmap(bmp, chart.ClientRectangle);
            return bmp;
        }
    }
}
