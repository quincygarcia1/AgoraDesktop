using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AgoraDesktop.Hubs
{
    internal class CustomCollection
    {
        private Timer timerAttribute;
        private string processName;
        private string windowName;
        private int totalTime;

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
        }

        public void StopTimer()
        {
            this.timerAttribute.Enabled = false;
            this.timerAttribute.Dispose();
        }
    }
}
