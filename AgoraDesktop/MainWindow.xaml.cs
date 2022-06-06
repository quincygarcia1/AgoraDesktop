using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;

namespace AgoraDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Hashtable currentNumProcesses = new Hashtable();

        // TODO: create a data type, most likely a hash table, to count the number of processes that share a name. This will be used
        //       so that the code knows when to send a request to the server to stop the application timer and add the time spent to the database.
        ManagementEventWatcher processStartEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
        ManagementEventWatcher processStopEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");
        public MainWindow()
        {
            // establish a connection with the server when the server code is complete

            InitializeComponent();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddWpfBlazorWebView();
            Resources.Add("services", serviceCollection.BuildServiceProvider());

            // Added event Handlers for process starting and stopping
            processStartEvent.EventArrived += new EventArrivedEventHandler(processStartEvent_EventArrived);
            processStartEvent.Start();
            processStopEvent.EventArrived += new EventArrivedEventHandler(processStopEvent_EventArrived);
            processStopEvent.Start();
        }

        // Handler for when a process starts. Pass the process to the server so the server can start a timer for the process and can update the list
        // of stored processes.
        void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            string processName = GetAppName(e.NewEvent.Properties["MainWindowTitle"].Value.ToString());
        }

        // Handler to be used when a process is stopped. Pass the process name to the server so that the server knows the app has been closed.
        // Note for later reference: in the server or through client side keep a count of how many processes are made for an app
        void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {

        }

        // Check which processes are minimized on initialization.
        // Create a method to check which processes are minimized (will be called be server every minute).

        // Method that should be run on start. Keep a data storage for the active processes so they can be passed to the server. Useful if the app
        // isn't being started on Windows login. When complete, send each unique process to the server.
        void getActiveProcesses()
        {
            foreach(Process p in Process.GetProcesses())
            {
                try
                {
                    currentNumProcesses.Add(GetAppName(p.MainWindowTitle.ToString()), 1);
                } catch
                {
                    currentNumProcesses[p.MainWindowTitle.ToString()] = (int)(currentNumProcesses[GetAppName(p.MainWindowTitle.ToString())]) + 1;
                }
                
            }
        }
        //method used to get the Application name of a process. Should have the stringified MainWindowTitle attribute passed in
        string GetAppName(string input)
        {
            int starting_index = 0;
            for(int i = 5; i < input.Length; i++)
            {
                if ((input[i - 5] == ' ') && (input[i - 1] == input[i - 5]) && (input[i-2] == '-') && (input[i-2] == input[i - 3]))
                {
                    starting_index = i;
                }
            }
            return input.Substring(starting_index, input.Length);
        }

        // TODO: Create a handler for the server when a time alert should be made. Time alerts should be made every 30 minutes that a
        //       program is minimized.
    }
}
