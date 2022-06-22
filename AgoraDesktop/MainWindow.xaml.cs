using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
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
        Hashtable currentNumProcesses = new Hashtable();

        // TODO: create a data type, most likely a hash table, to count the number of processes that share a name. This will be used
        //       so that the code knows when to send a request to the server to stop the application timer and add the time spent to the database.
        ManagementEventWatcher processStartEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
        ManagementEventWatcher processStopEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");
        HubConnection connection;
        public MainWindow()
        {
            // establish a connection with the server when the server code is complete

            InitializeComponent();

            // Set up the local connection to the hub with automatic reconnecting.
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7142/userhub")
                .WithAutomaticReconnect()
                .Build();

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
            
        }

        // Handler for when a process starts. Pass the process to the server so the server can start a timer for the process and can update the list
        // of stored processes.
        private async void startConnection()
        {
            await connection.StartAsync();
        }
        
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

            // pass the data to the server to start the title
            if (connection != null)
            {
                if ((App.Current as App) != null && (App.Current as App).UserName != null)
                {
                    await sendAdd((App.Current as App).UserName, processName, pid, processTitle);
                    
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
            

            try
            {
                currentNumProcesses.Remove(pid);
            }
            catch
            {
                //pass
            }

            // Notify the server to stop the timer and record the usage 
            if (connection != null)
            {
                if ((App.Current as App) != null && (App.Current as App).UserName != null)
                {
                    await sendDelete((App.Current as App).UserName, processName, pid, processTitle);
                    
                }
            }
        }

        private async Task sendAdd(string username, string processname, int pid, string windowName)
        {
            await connection.SendAsync("AddCurrentProcess", username, pid, windowName);
        }

        private async Task sendDelete(string username, string processname, int pid, string windowName)
        {
            await connection.SendAsync("RemoveCurrentProcess", username, pid, windowName);
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
                        if (connection != null)
                        {
                            if ((App.Current as App) != null && (App.Current as App).UserName != null)
                            {
                                await connection.SendAsync("AddCurrentProcess", (App.Current as App).UserName, p.ProcessName.ToString(), p.Id, p.MainWindowTitle.ToString());
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

        // TODO: Create a handler for the server when a time alert should be made. Time alerts should be made every 30 minutes that a
        //       program is minimized.

        void createTimeAlert()
        {
            // Should be encapsulated in a method called by the server. Buttons will be added to the reminder as well to either dismiss, kill the process
            // or kill the program
            new ToastContentBuilder()
                .AddArgument("action", "openApp")
                .AddText("*App* has been running for *Time*")
                .AddText("Manage your applications in Agora")
                .Show();
        }
    }
}
