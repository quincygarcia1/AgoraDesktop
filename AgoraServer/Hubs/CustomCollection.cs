using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace AgoraServer.Hubs
{
    public class CustomCollection
    {
        private Timer timerAttribute;
        // possible timer to be kept for when a process is minimized

        private Timer minimizedTimer;
        private string processName;
        private string windowName;
        private int totalTime;
        private DateTime timeStarted;
        private DateTime minimizedStarted;

        public Timer MinimizedTimerAttribute
        {
            get { return timerAttribute; }
        }
        public DateTime MinimizedStarted
        {
            get { return minimizedStarted; }
            set { minimizedStarted = value; }
        }
        public DateTime StartTime
        {
            get { return timeStarted; }
            set { timeStarted = value; }
        }
        public Timer TimerAttribute
        {
            get { return timerAttribute; }
        }
        public string ProcessName
        {
            get { return processName;  }

        }
        public int TotalTime
        {
            get { return totalTime;  }
            set { totalTime = value; }
        }
        public string WindowName
        {
            get { return windowName; }
        }

        public CustomCollection(string processName, string windowName)
        {
            this.timerAttribute = new Timer(1000 * 60 * 30);
            this.timerAttribute.AutoReset = true;
            this.processName = processName;
            this.windowName = windowName;
            this.totalTime = 0;
            this.timeStarted = DateTime.Now;
        }

        public void StopTimer()
        {
            this.timerAttribute.Enabled = false;
            this.timerAttribute.Dispose();
        }

        public void resetStarted()
        {
            this.timeStarted = DateTime.Now;
        }

        public void resetMinimizedStartTime()
        {

        }
    }
}
