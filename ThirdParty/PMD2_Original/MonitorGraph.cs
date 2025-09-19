using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using PMD2;

namespace PMD2
{
    public partial class MonitorGraph : UserControl
    {
        public class ValueAddedEventArgs : EventArgs
        {
            public double Value { get; set; }
        }

        public delegate void ValueAddedEventHandler(object sender, ValueAddedEventArgs args);
        public event ValueAddedEventHandler ValueAdded;

        private const int MaxNumValues = 10000;

        public DBPanel panel1;
        public List<double> values;

        public int id;
        public string desc;
        public string suffix;
        public string latestVal = "";

        public double minValue = Double.MaxValue;
        public double maxValue = Double.MinValue;
        public double minValueTotal = Double.MaxValue;
        public double maxValueTotal = Double.MinValue;

        public string format;

        private int displayX;

        bool border;

        DateTime last_draw;

        //Timer timerAnimation;
        //int frame_counter = -1;

        ContextMenu context_menu;
        private int logging_id;

        public int x_step = 1;

        string ParentDescription = "";

        public MonitorGraph(int id, string desc, string suffix, string value_format, int pos_x, int pos_y, int width, int height, bool border, string parent_desc)
        {
            InitializeComponent();

            this.Location = new Point(pos_x, pos_y);
            this.Size = new Size(width, height);

            //maxWidth = this.Size.Width;
            //maxHeight = this.Size.Height - 20;

            this.id = id;
            this.desc = desc;
            this.suffix = suffix;
            this.format = value_format;
            this.border = border;
            this.displayX = -1;

            panel1 = new DBPanel();
            panel1.AutoSize = false;
            panel1.AutoSizeMode = AutoSizeMode.GrowOnly;
            //panel1.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left;
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Size = this.Size;
            this.Controls.Add(panel1);

            values = new List<double>();
            values.Capacity = 100000;

            panel1.Paint += new PaintEventHandler(panel1_Paint);

            context_menu = new ContextMenu();
            context_menu.MenuItems.Add("Show in detached window", OpenDetachedView);
            context_menu.MenuItems.Add("Add to Data Logger", AddToDataLogger);
            context_menu.MenuItems.Add("Reset Min/Max", ResetMinMax);

            panel1.ContextMenu = context_menu;
            logging_id = -1;
            ParentDescription = parent_desc;
        }

        private void ResetMinMax(object sender, EventArgs e)
        {
            this.minValueTotal = Double.MaxValue;
            this.maxValueTotal = Double.MinValue;
        }

        private void OpenDetachedView(object sender, EventArgs e)
        {
            //FormMonitorGraph fmg = new FormMonitorGraph("", this);
            //fmg.Show();
        }

        private void AddToDataLogger(object sender, EventArgs e)
        {
            /*if(logging_id == -1) {
                // Add logging item
                string log_desc;
                if(ParentDescription != "") {
                    log_desc = ParentDescription + " - " + desc;
                } else {
                    log_desc = desc;
                }
                logging_id = Program.data_logger.AddLogItem(log_desc, suffix);
                context_menu.MenuItems[1].Text = "Remove from Data Logger";
            } else {
                // Remove logging item
                Program.data_logger.RemoveLogItem(logging_id);
                logging_id = -1;
                context_menu.MenuItems[1].Text = "Add to Data Logger";
            }*/
        }
        Font DescriptionFont = new Font("Consolas", 9);
        Font HoverFont = new Font("Consolas", 9);

        Point[] graphPoints;

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

            Graphics g = e.Graphics;
            //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            bool hoverActive = false;
            if (graphPoints == null || graphPoints.Length < 2)
            {
                if (border)
                {
                    g.DrawString(desc, DescriptionFont, Brushes.Black, new Point(0, 0));
                    g.DrawRectangle(Pens.LightGray, 0, 15, this.Width - 1, this.Height - 16);
                }
                return;
            }

            g.DrawLines(Pens.Purple, graphPoints);

            int x_pos = this.Width - displayX;// - (this.Width - graphPoints.Length);
            int x_value = graphPoints.Length - x_pos;
            if (x_pos > 0 && x_pos < graphPoints.Length && x_pos <= this.Width)
            {
                hoverActive = true;
            }

            // Static components
            if (border)
            {
                //if (!hoverActive)
                //{
                g.DrawString(desc, DescriptionFont, Brushes.Black, new Point(0, 0));
                g.DrawString(latestVal.PadLeft(10, ' '), DescriptionFont, Brushes.Black, new Point(this.Size.Width - 75, 0));
                //}
                g.DrawRectangle(Pens.LightGray, 0, 15, this.Width - 1, this.Height - 16);
            }

            // Display value at specific point on curve
            if (hoverActive)
            {
                g.FillEllipse(Brushes.Purple, displayX - 3, graphPoints[x_value].Y - 3, 6, 6);
                String hoverText = $"{values[x_value].ToString(format)}/{this.minValueTotal.ToString(format)}/{this.maxValueTotal.ToString(format)} {suffix}";
                SizeF str_size = g.MeasureString(hoverText, HoverFont);
                g.DrawString(hoverText, HoverFont, Brushes.MediumPurple, new Point(Size.Width - (int)str_size.Width - 1, Size.Height - (int)str_size.Height));

                /*if (e.Graphics.MeasureString(hoverText, HoverFont).Width <= this.Width)
                {

                }
                else
                {
                    hoverText = hoverText.Replace(suffix.ToString() + " ", "");
                    if (e.Graphics.MeasureString(hoverText, HoverFont).Width <= this.Width)
                    {
                        g.DrawString(hoverText, HoverFont, Brushes.Purple, new Point(0, Size.Height - 20));
                    }
                    else
                    {
                        g.DrawString((values[x_value].ToString(format) + " " + suffix).PadLeft(10, ' '), HoverFont, Brushes.Purple, new Point(this.Size.Width - 75, Size.Height-20));
                    }
                }*/

            }
            else
            {

            }

            last_draw = DateTime.Now;
        }


        public void ClearValues()
        {
            values.Clear();
            if (this.Enabled)
            {
                panel1.Invalidate();
            }
        }

        public void AddValue(double val)
        {

            /*if(logging_id != -1) {
                if(Program.data_logger.UpdateValue(logging_id, val) == false) {
                    AddToDataLogger(null, null);
                }
            }*/

            values.Add(val);

            if (values.Count > MaxNumValues)
            {
                values.RemoveAt(0);
            }

            latestVal = val.ToString(format) + " " + suffix;

            if (val > maxValueTotal) maxValueTotal = val;
            if (val < minValueTotal) minValueTotal = val;

            /*if (val > maxValue) maxValue = val;
            if (val < minValue) minValue = val;
            if(minValue > maxValue) {
                maxValue = minValue;
            }*/
            //}

            // Find min/max

            //double newMax = Double.MinValue;
            //double newMin = Double.MaxValue;
            //foreach(double value in values) {
            //if(value > newMax) newMax = value;
            //if(value < newMin) newMin = value;
            //}

            minValue = 0;
            maxValue = values.Max();

            graphPoints = new Point[values.Count];

            int y = 0;
            for (int i = 0; i < graphPoints.Length; i++)
            {
                if (maxValue == minValue)
                {
                    y = (this.Size.Height - 20) / 2;
                }
                else
                {
                    y = (int)(values[values.Count - graphPoints.Length + i] / (maxValue - minValue) * (this.Height - 25));
                    y = (this.Size.Height - 1) - y - 5;
                }
                graphPoints[i] = new Point(i + (this.Width - graphPoints.Length), y);

            }

            // Trigger animation
            /*if(this.Enabled && graphPoints.Length > 2) {
                graphPointsAnimate = new Point[graphPoints.Length - 1];
                for(int i = 0; i < graphPointsAnimate.Length; i++) {
                    graphPointsAnimate[i] = graphPoints[i];
                }
                animateFrom = graphPoints[graphPoints.Length - 2];
                animateTo = graphPoints[graphPoints.Length - 1];
                timerAnimation.Start();
            } else {
                // Update graph
                if(this.Enabled) {
                    panel1.Invalidate();
                }
            }*/

            // Update graph
            if (this.Enabled)
            {
                panel1.Invalidate();
            }

            if (ValueAdded != null)
            {
                ValueAddedEventArgs e = new ValueAddedEventArgs();
                e.Value = val;
                ValueAdded(this, e);

            }
        }

        public void AddValues(List<double> new_values)
        {

            values.Clear();
            values.AddRange(new_values);

            if (values.Count > MaxNumValues)
            {
                values.RemoveAt(0);
            }

            //latestVal = val.ToString(format) + " " + suffix;

            //if(val > maxValueTotal) maxValueTotal = val;
            //if(val < minValueTotal) minValueTotal = val;

            // Find min/max
            /*maxValue = Double.MinValue;
            foreach(double value in values) {
                if(value > maxValue) maxValue = value;
            }*/
            //maxValue = values.Max();
            //maxValue = 60;
            //maxValue = 15;

            //minValue = 0;

            graphPoints = new Point[values.Count];

            int y = 0;
            for (int i = 0; i < graphPoints.Length; i++)
            {
                if (maxValue == minValue)
                {
                    y = (this.Size.Height - 20) / 2;
                }
                else
                {
                    y = (int)(values[values.Count - graphPoints.Length + i] / (maxValue - minValue) * (this.Height - 25));
                    y = (this.Size.Height - 1) - y - 5;
                }
                graphPoints[i] = new Point(i * x_step + (this.Width - graphPoints.Length * x_step), y);

            }

            // Trigger animation
            /*if(this.Enabled && graphPoints.Length > 2) {
                graphPointsAnimate = new Point[graphPoints.Length - 1];
                for(int i = 0; i < graphPointsAnimate.Length; i++) {
                    graphPointsAnimate[i] = graphPoints[i];
                }
                animateFrom = graphPoints[graphPoints.Length - 2];
                animateTo = graphPoints[graphPoints.Length - 1];
                timerAnimation.Start();
            } else {
                // Update graph
                if(this.Enabled) {
                    panel1.Invalidate();
                }
            }*/

            // Update graph
            if (this.Enabled)
            {
                panel1.Invalidate();
            }
        }

        public void SetTrackX(int x)
        {
            displayX = x;
            if ((DateTime.Now - last_draw).TotalMilliseconds > 15)
            {
                panel1.Invalidate();
            }
        }
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MonitorGraph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "MonitorGraph";
            this.ResumeLayout(false);
        }
    }
}
