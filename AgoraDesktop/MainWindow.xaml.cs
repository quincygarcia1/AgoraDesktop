using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
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
using AgoraServer.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AgoraDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<string> sessionActivityList;
        Hashtable currentNumProcesses = new Hashtable();

        // TODO: create a data type, most likely a hash table, to count the number of processes that share a name. This will be used
        //       so that the code knows when to send a request to the server to stop the application timer and add the time spent to the database.
        ManagementEventWatcher processStartEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
        ManagementEventWatcher processStopEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");
        private static Socket _soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // Will be useful when an "exit program" button is added.
        private bool programRunning = true;

        public MainWindow()
        {
            // establish a connection with the server when the server code is complete

            InitializeComponent();

            // Set up the local connection to the server with automatic reconnecting.
            

            this.Title = "Agora";

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddWpfBlazorWebView();
            

            Resources.Add("services", serviceCollection.BuildServiceProvider());

            

            // Added event Handlers for process starting and stopping
            processStartEvent.EventArrived += new EventArrivedEventHandler(processStartEvent_EventArrived);
            processStartEvent.Start();
            processStopEvent.EventArrived += new EventArrivedEventHandler(processStopEvent_EventArrivedAsync);
            processStopEvent.Start();

            // begin the connection to the hub.
            
            startConnection();
            Thread serverReceiver = new Thread(GetServerInput);
            serverReceiver.Start();
        }

        // Continuously attempt to connect to the server
        private static async void startConnection()
        {
            while (!_soc.Connected)
            {
                try
                {
                    _soc.Connect(IPAddress.Loopback, 7313);
                }
                catch (SocketException)
                {

                }
            }
            
            
        }

        // Handler for when a process starts. Pass the process to the server so the server can start a timer for the process and can update the list
        // of stored processes.
        async void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            
            int pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString();

            string processTitle;
            try
            {
                processTitle = Process.GetProcessById(pid).MainWindowTitle.ToString();
            }
            catch
            {
                return;
            }
            if (processTitle == "Agora")
            {
                return;
            }

            if (processTitle != "" && processTitle != null)
            {
                try
                {
                    currentNumProcesses.Add(pid, Tuple.Create(processName, processTitle));
                }
                catch
                {
                    //pass
                }
            }

            // pass the data to the server to start the timer
            if (_soc.Connected)
            {
                if ((App.Current as App) != null && (App.Current as App).UserName != null)
                {
                    string sendData = "AddCurrentProcess-" + String.Join(" $^% ", new string[] { (App.Current as App).UserName, pid.ToString(), processName, processTitle });
                    byte[] buf = Encoding.ASCII.GetBytes(sendData);
                    _soc.Send(buf);

                }
            }
        }


        // Handler to be used when a process is stopped. Pass the process name to the server so that the server knows the app has been closed.
        // Note for later reference: in the server or through client side keep a count of how many processes are made for an app
        async void processStopEvent_EventArrivedAsync(object sender, EventArrivedEventArgs e)
        {
            
            int pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            string processTitle;
            try
            {
                processTitle = Process.GetProcessById(pid).MainWindowTitle.ToString();
            }
            catch
            {
                return;
            }
            if (processTitle == "")
            {
                return;
            }

            try
            {
                currentNumProcesses.Remove(pid);
            }
            catch
            {
                //pass
            }

            // Notify the server to stop the timer and record the usage 
            if (_soc.Connected)
            {
                if ((App.Current as App) != null && (App.Current as App).UserName != null)
                {
                    string sendData = "RemoveCurrentProcess-" + String.Join(" $^% ", new string[] { (App.Current as App).UserName, pid.ToString(), processName, processTitle });
                    byte[] buf = Encoding.ASCII.GetBytes(sendData);
                    _soc.Send(buf);

                }
            }
        }

        // Check which processes are minimized on initialization.
        // Create a method to check which processes are minimized (will be called be server every minute).

        private void checkMinimized()
        {

        }

        // Method that should be run on start. Keep a data storage for the active processes so they can be passed to the server. Useful if the app
        // isn't being started on Windows login. When complete, send each unique process to the server.
        public async Task getActiveProcessesAsync()
        {
            foreach(Process p in Process.GetProcesses())
            {
                if (p.MainWindowTitle.ToString() != "")
                {
                    try
                    {
                        currentNumProcesses.Add(p.Id, Tuple.Create(p.ProcessName.ToString(), p.MainWindowTitle.ToString()));

                        // pass the info to the server for the start-up process
                        if (_soc.Connected)
                        {
                            if ((App.Current as App) != null && (App.Current as App).UserName != null)
                            {
                                string sendData = "AddCurrentProcess-" + String.Join(" $^% ", new string[] { (App.Current as App).UserName, p.Id.ToString(), p.ProcessName.ToString(), p.MainWindowTitle.ToString() });
                                byte[] buf = Encoding.ASCII.GetBytes(sendData);
                                _soc.Send(buf);

                            }
                        }
                    }
                    catch
                    {
                        //pass
                    }
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

        public async Task UpdateUserList()
        {
            if (_soc.Connected)
            {
                if ((App.Current as App) != null && (App.Current as App).UserName != null)
                {
                    string sendData = "GetCurrentApplications-" + (App.Current as App).UserName;
                    byte[] buf = Encoding.ASCII.GetBytes(sendData);
                    _soc.Send(buf);

                }
            }
                
        }

        // TODO: Create a handler for the server when a time alert should be made. Time alerts should be made every 30 minutes that a
        //       program is minimized.

        

        void CreateTimeAlert()
        {
            // Should be encapsulated in a method called by the server. Buttons will be added to the reminder as well to either dismiss, kill the process
            // or kill the program
            new ToastContentBuilder()
                .AddArgument("action", "openApp")
                .AddText("*App* has been running for *Time*")
                .AddText("Manage your applications in Agora")
                .Show();
        }

        // Run an infinite loop to read input from the server
        private void GetServerInput()
        {
            while (programRunning)
            {
                byte[] receivedBuf = new byte[1024];
                // convert this to BeginReceive() to prevent socket blocking.
                int bufLen = _soc.Receive(receivedBuf);
                byte[] dataReceived = new byte[bufLen];
                Array.Copy(receivedBuf, dataReceived, bufLen);
                string result = Encoding.ASCII.GetString(dataReceived);

                int dashIndex = result.IndexOf("-");
                if (dashIndex == -1 || dashIndex + 1 > result.Length)
                {
                    continue;
                }
                string processInfo = result.Substring(dashIndex + 1);
                string handlingMethod = result.Substring(0, dashIndex);

                if (handlingMethod == "CreateTimeAlert")
                {
                    string[] separation = processInfo.Split(" $^% ");
                    string processName = separation[0];
                    int pid = Int32.Parse(separation[1]);
                    string appName = separation[2];
                    int millisecondCount = Int32.Parse(separation[3]);

                    CreateTimeAlert();
                }
                
            }
        }
    }
}
