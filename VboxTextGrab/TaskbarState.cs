using System.Windows.Forms;
#if TASKBAR_PROGRESS
using Microsoft.WindowsAPICodePack.Taskbar;
#else
using System;
using System.Runtime.InteropServices;
#endif

namespace VboxTextGrab
{
    class TaskbarState
    {

        private Form mainForm;
        public TaskbarState(Form mainForm)
        {
            this.mainForm = mainForm;
        }

#if TASKBAR_PROGRESS

        TaskbarManager manager = TaskbarManager.Instance;
        int calibrationProgress;

        public void StartCalibration()
        {
            manager.SetProgressState(TaskbarProgressBarState.Error);
            calibrationProgress = 1;
            manager.SetProgressValue(calibrationProgress, 5);
        }

        public void StepCalibration()
        {
            calibrationProgress++;
            if (calibrationProgress == 5) calibrationProgress = 1;
            manager.SetProgressValue(calibrationProgress, 5);
        }

        public void EndCalibration()
        {
            manager.SetProgressState(TaskbarProgressBarState.NoProgress);
        }

        public void StartGrabbing()
        {
            manager.SetProgressState(TaskbarProgressBarState.Normal);
            manager.SetProgressValue(2, 5);
        }

        public void EndGrabbing()
        {
            manager.SetProgressState(TaskbarProgressBarState.NoProgress);
        }

#else

        public void StartCalibration() { }
        public void EndCalibration() { }
        public void StartGrabbing() { }
        public void EndGrabbing() { }

        public void StepCalibration()
        {
            FlashWindow(mainForm.Handle, true);
            Timer t = new Timer();
            t.Interval = 200;
            t.Tick += delegate(object sender, EventArgs e)
            {
                FlashWindow(mainForm.Handle, false);
                t.Enabled = false;
            };
            t.Enabled = true;
        }

        #region PInvoke declarations

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        #endregion

#endif
    }
}
