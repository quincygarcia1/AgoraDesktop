using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AgoraDesktop.Hubs
{
    public class UserHub : Hub
    {
        // TODO: Keep a collection of connectionIDs, the login info corresponding to the connection and the timers for the
        // clients active processes

        // Dictionary within a dictionary. keeps the connection ID as a key and a dictionary within
        // PID key, list of process name, window name, timer, total time spent as the items within the second dictionary>
        
        Dictionary<string, Dictionary<int, CustomCollection>> connectedUsers = new Dictionary<string, Dictionary<int, CustomCollection>>();

        // constant timer to be used to check whether processes are minimized.
        Timer minimizedTimer = new Timer(60000);
        

        public async Task StartGlobalTimer()
        {
            minimizedTimer.Interval = 60000;
            minimizedTimer.Elapsed += SendUpdateRequest;
            minimizedTimer.AutoReset = true;
            minimizedTimer.Enabled = true;
        }

        // Method to notify a user that they've had an app minimized for a while
        public async Task NotifyUser(object sender, ElapsedEventArgs e, string connectionId, string processName, int pid, string appName)
        {
            // Increases the total timer by 30 minutes. Could change this at some point maybe
            connectedUsers[connectionId][pid].TotalTime += (1000 * 60 * 30);
            await Clients.Client(connectionId).SendAsync("createTimeAlert", processName, pid, appName, connectedUsers[connectionId][pid].TotalTime);

        }

        // Method to add a process for a client's active applications. An entry with a clock should be started.
        public async Task AddCurrentProcess(string processName, int pid, string appName)
        {
            // Intializes the timer to 30 mins
       
            // set the elapsed method. The NotifyUser method will need the connectionID as a precaution to know where to send the alert
            // The alert will also ask whether the user wants to close all the entire program <appName> or just the specific process <processName>.
            // The ID is passed to kill a single process if needed.
            
            
            if (connectedUsers.ContainsKey(Context.ConnectionId))
            {
                CustomCollection individualGroup = new CustomCollection(processName, appName);
                individualGroup.TimerAttribute.Elapsed += (sender, e) => NotifyUser(sender, e, Context.ConnectionId, processName, pid, appName);
                individualGroup.TimerAttribute.Enabled = true;
                connectedUsers[Context.ConnectionId].Add(pid, individualGroup);
                // Send to the client that a new process is there. This is to live update the home page
                // for new active processes.
            }
            else
            {
                // Method to be called if a process doesn't register. Called in the event of an error.
                await Clients.Caller.SendAsync("FailedProcessReg", processName, pid, appName);
            }

        }

        public async void SendUpdateRequest(object sender, ElapsedEventArgs e)
        {
            await IconicStatus();
        }

        // Request to be made to the client to update the minimized status of applications
        public async Task IconicStatus()
        {
            // Method to be passed to all clients to receive the minimized status of each clients applications.
            await Clients.All.SendAsync("CheckMinimized");

        }


        // Method to be called when a user stops an application on their computer. Stop the process clock
        // and properly update the user's lifetime app history
        public async Task RemoveCurrentProcess(string processName, int pid, string appName)
        {
            if (connectedUsers.ContainsKey(Context.ConnectionId))
            {
                if (connectedUsers[Context.ConnectionId].ContainsKey(pid))
                {
                    connectedUsers[Context.ConnectionId][pid].TimerAttribute.Elapsed -= 
                        (sender, e) => NotifyUser(sender, e, Context.ConnectionId, processName, pid, appName);
                    connectedUsers[Context.ConnectionId][pid].StopTimer();
                    connectedUsers[Context.ConnectionId].Remove(pid);

                    // Send to the client that a process is gone. This is to live update the home page
                    // for new active processes.
                }
            }
            else
            {
                // Method to be called if a process doesn't register. Called in the event of an error.
                await Clients.Caller.SendAsync("FailedProcessReg", processName, pid, appName);
            }
        }


        // Method to transfer the app history of a client. Make sure to add their current session activity to the total.
        public async Task GetClientAppHistory()
        {
            List<string> processNames = new List<string>();
            List<string> appNames = new List<string>();

            List<CustomCollection> sortedUserDict = SortCollections(Context.ConnectionId);

            var keyList = connectedUsers[Context.ConnectionId].Keys.ToList();
            for (int i = 0; i < keyList.Count; i++)
            {

            }
        }

        // Quicksort algorithm to sort the app usage times from least to greatest. Used to organize the user display.
        internal List<CustomCollection> SortCollections(string connectionId)
        {
            Dictionary<int, CustomCollection> focus = connectedUsers[connectionId];
            List<CustomCollection> listOfValues = focus.Values.ToList();
            CustomCollection pivot = listOfValues[listOfValues.Count - 1];
            return QuickSort(listOfValues.Count - 1, listOfValues, pivot);
        }

        internal List<CustomCollection> QuickSort(int endIndex, List<CustomCollection> collection, CustomCollection pivot)
        {
            if (collection.Count <= 1)
            {
                return collection;
            }
            List<CustomCollection> smaller = new List<CustomCollection>;
            List<CustomCollection > larger = new List<CustomCollection>();

            for (int i = 0; i < endIndex; i++)
            {
                if (collection[i].TotalTime < pivot.TotalTime)
                {
                    smaller.Add(collection[i]);
                } else
                {
                    larger.Add(collection[i]);
                }
            }
            List<CustomCollection> pivotList = new List<CustomCollection> { pivot };
            List<CustomCollection> result = new List<CustomCollection>(smaller.Count + pivotList.Count + larger.Count);
            List<CustomCollection> smallerRes = QuickSort(smaller.Count - 1, smaller, smaller[smaller.Count - 1]);
            List<CustomCollection> largerRes = QuickSort(larger.Count - 1, larger, larger[larger.Count - 1]);
            result.AddRange(largerRes);
            result.AddRange(pivotList);
            result.AddRange(smallerRes);
            return result.ToList();
        }

        // Gets the timers for all the current processes. To be displayed on the home page.
        public async Task GetCurrentApplications()
        {
            
        }

        // Check client login information through a database. Create a new space in the collection for the client and their connection ID
        public async Task VerifyLogin()
        {

        }


        // Creates a new user and updates the database accordingly
        public async Task IntializeNewUser()
        {
            
        }


    }
}
