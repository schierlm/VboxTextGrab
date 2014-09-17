using System;
using System.Drawing;
using System.Windows.Forms;

namespace VboxTextGrab
{
    public partial class MainForm : Form
    {
        private Calibration calibration = null;
        private TaskbarState taskbar;
        private FormWindowState prevState = FormWindowState.Normal;

        public MainForm()
        {
            InitializeComponent();
            taskbar = new TaskbarState(this);
        }

        private void calibrateButton_Click(object sender, EventArgs e)
        {
            richTextBox.Text = "Calibrating...";
            calibration = new Calibration();
            taskbar.StartCalibration();
            Go();
        }

        private void grabButton_Click(object sender, EventArgs e)
        {
            richTextBox.Text = "Grabbing...";
            taskbar.StartGrabbing();
            Go();
        }

        private void Go()
        {
            richTextBox.SelectAll();
            richTextBox.SelectionColor = richTextBox.ForeColor;
            richTextBox.SelectionBackColor = richTextBox.BackColor;
            prevState = WindowState;
            WindowState = FormWindowState.Minimized;
            timer.Enabled = true;
            richTextBox.Select(richTextBox.Text.Length, 0);
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (timer.Enabled)
            {
                timer.Enabled = false;
                if (calibration != null)
                {
                    richTextBox.Text = calibration.Status;
                    calibration = null;
                    taskbar.EndCalibration();
                }
                else
                {
                    taskbar.EndGrabbing();
                }
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Bitmap bmp = Grabber.GrabScreen();
            if (bmp == null)
                return;

            bmp = Parser.RemoveBorder(bmp);

            if (calibration != null)
            {
                if (calibration.Add(bmp) && !calibration.IsFinished)
                {
                    taskbar.StepCalibration();
                }
                else if (!calibration.WasSame)
                {
                    WindowState = prevState;
                }
                bmp.Dispose();
                return;
            }

            ColorInformation ci = new ColorInformation();
            richTextBox.Text = Parser.Parse(bmp, ci);
            bmp.Dispose();

            if (ci.ForegroundColors != null)
            {
                for (int y = 0; y < ci.ForegroundColors.Length / ci.ScreenWidth; y++)
                {
                    for (int x = 0; x < ci.ScreenWidth; x++)
                    {
                        int idx = y * ci.ScreenWidth + x;
                        richTextBox.Select(ci.MessageLength + y * (ci.ScreenWidth + 1) + x, 1);
                        richTextBox.SelectionColor = ci.ForegroundColors[idx];
                        richTextBox.SelectionBackColor = ci.BackgroundColors[idx];
                    }
                }

                if (ci.CursorX != -1)
                {
                    richTextBox.Select(ci.MessageLength + ci.CursorY * (ci.ScreenWidth + 1) + ci.CursorX, 1);
                    richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Underline | FontStyle.Bold);
                }
            }
            richTextBox.Select(richTextBox.Text.Length, 0);
            WindowState = prevState;
        }
    }
}
