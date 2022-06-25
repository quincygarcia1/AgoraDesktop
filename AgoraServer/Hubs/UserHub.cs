using AgoraServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace AgoraServer.Hubs
{
    public class UserHub : Hub
    {
        // TODO: Keep a collection of connectionIDs, the login info corresponding to the connection and the timers for the
        // clients active processes

        // Dictionary within a dictionary. keeps the username as a key and a dictionary within
        // PID key, list of process name, window name, timer, total time spent as the items within the second dictionary>

        Dictionary<string, Dictionary<int, CustomCollection>> connectedUsers = new Dictionary<string, Dictionary<int, CustomCollection>>();

        // Dictionary that keeps the current connectionID for each user logged in. Each page has a different connectionID so this dictionary will be updated
        // on different connects

        // Username key, connectionID value
        Dictionary<string, string> correspondingConnections = new Dictionary<string, string>();

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
        public async Task NotifyUser(object sender, ElapsedEventArgs e, string username, string connectionId, string processName, int pid, string appName)
        {
            // Increases the total timer by 30 minutes. Could change this at some point maybe
            connectedUsers[username][pid].TotalTime += (1000 * 60 * 30);
            connectedUsers[username][pid].resetStarted();
            await Clients.Client(connectionId).SendAsync("createTimeAlert", processName, pid, appName, connectedUsers[connectionId][pid].TotalTime);

        }

        // Method to add a process for a client's active applications. An entry with a clock should be started.
        public async Task AddCurrentProcess(string username, string processName, int pid, string appName)
        {
            // Intializes the timer to 30 mins
       
            // set the elapsed method. The NotifyUser method will need the connectionID as a precaution to know where to send the alert
            // The alert will also ask whether the user wants to close all the entire program <appName> or just the specific process <processName>.
            // The ID is passed to kill a single process if needed.
            
            
            if (correspondingConnections.ContainsKey(username))
            {
                CustomCollection individualGroup = new CustomCollection(processName, appName);
                individualGroup.TimerAttribute.Elapsed += (sender, e) => NotifyUser(sender, e, username, Context.ConnectionId, processName, pid, appName);
                individualGroup.TimerAttribute.Enabled = true;
                connectedUsers[username].Add(pid, individualGroup);

                // Send to the client that a new process is there. This is to live update the home page
                // for new active processes.
                await Clients.Client(correspondingConnections[username]).SendAsync("AddToList", processName);
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

        public void updateMinimizedForUser(int pid)
        {
            // Method to be used to update a program to the "minimized status". To be implemented later.
        }


        // Method to be called when a user stops an application on their computer. Stop the process clock
        // and properly update the user's lifetime app history
        public async Task RemoveCurrentProcess(string username, string processName, int pid, string appName)
        {
            if (connectedUsers.ContainsKey(username))
            {
                if (connectedUsers[username].ContainsKey(pid))
                {
                    double timeElapsed = (DateTime.Now - connectedUsers[username][pid].StartTime).TotalMilliseconds;
                    connectedUsers[username][pid].TotalTime += (int)timeElapsed;

                    // update the database

                    // Send to the client that a process is gone. This is to live update the home page
                    // for new active processes.
                    await Clients.Client(correspondingConnections[username]).SendAsync("RemoveFromList", processName);

                    connectedUsers[username][pid].TimerAttribute.Elapsed -= 
                        (sender, e) => NotifyUser(sender, e, username, Context.ConnectionId, processName, pid, appName);
                    connectedUsers[username][pid].StopTimer();
                    connectedUsers[username].Remove(pid);

                    
                    
                }
            }
            else
            {
                // Method to be called if a process doesn't register. Called in the event of an error.
                await Clients.Caller.SendAsync("FailedProcessReg", processName, pid, appName);
            }
        }


        // Method to transfer the app history of a client. Make sure to add their current session activity to the total.
        public async Task GetClientAppHistory(string username)
        {
            // Connect to the database
            // Get the activity data
            // add the current total time
            // send the data to the user
        }

        // Quicksort algorithm to sort the app usage times from least to greatest. Used to organize the user display.
        internal List<CustomCollection> SortCollections(string username)
        {
            Dictionary<int, CustomCollection> focus = connectedUsers[username];
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
            List<CustomCollection> smaller = new List<CustomCollection>();
            List<CustomCollection > larger = new List<CustomCollection>();

            for (int i = 0; i < endIndex; i++)
            {
                if ((collection[i].TotalTime + (DateTime.Now - collection[i].StartTime).TotalMilliseconds) < (pivot.TotalTime + (DateTime.Now - pivot.StartTime).TotalMilliseconds))
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
        public async Task GetCurrentApplications(string username)
        {
            List<CustomCollection> processList = SortCollections(username);
            await Clients.Caller.SendAsync("UpdateList", processList);
            
        }

        public async Task setConnectionId(string username)
        {
            correspondingConnections[username] = Context.ConnectionId;
        }

        public async Task addSignedUser(string username)
        {
            if (connectedUsers.Keys.ToList().Count == 0)
            {
                await StartGlobalTimer();
            }
            connectedUsers[username] = new Dictionary<int, CustomCollection>();

        }

    }
}
