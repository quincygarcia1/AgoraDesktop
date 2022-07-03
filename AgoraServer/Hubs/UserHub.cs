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
using AgoraDatabase.Contexts;
using AgoraDatabase.Services;
using AgoraDatabase;

namespace AgoraServer.Hubs
{
    public class UserHub : Hub
    {
        // TODO: Keep a collection of connectionIDs, the login info corresponding to the connection and the timers for the
        // clients active processes

        // Dictionary within a dictionary. keeps the username as a key and a dictionary within
        // PID key, list of process name, window name, timer, total time spent as the items within the second dictionary>

        static Dictionary<string, Dictionary<int, CustomCollection>> connectedUsers { get; set; }

        // Dictionary that keeps the current connectionID for each user logged in. Each page has a different connectionID so this dictionary will be updated
        // on different connects

        // Username key, connectionID value
        static Dictionary<string, string> correspondingConnections = new Dictionary<string, string>();

        // constant timer to be used to check whether processes are minimized.
        Timer minimizedTimer = new Timer(60000);
        IDataService<UserData> dbService = new GenericDataService<UserData>(new UserDataContextFactory());

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
                // create a static dictionary for the inner part of the dictionary storage.
                if (connectedUsers[username].Count == 0)
                {
                    connectedUsers[username] = new Dictionary<int, CustomCollection>();
                    connectedUsers[username].Clear();
                }
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
            for (int i = 0; i < connectedUsers.Keys.ToList().Count; i++)
            {
                await GetClientAppHistory(connectedUsers.Keys.ToList()[i]);
                await GetCurrentApplications(connectedUsers.Keys.ToList()[i]);
                await PageCommunication(connectedUsers.Keys.ToList()[i]);
            }
        }

        public async Task PageCommunication(string user)
        {
            await Clients.Client(correspondingConnections[user]).SendAsync("NotifyMainWindow");
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
                    var foundUser = dbService.Get(username);
                    var dict = ActivityDictionary(foundUser.Result.ActivityString);
                    if (dict.ContainsKey(GetAppName(appName)))
                    {
                        dict[appName] = (Int32.Parse(dict[appName]) + timeElapsed).ToString();
                    }
                    else
                    {
                        dict.Add(appName, timeElapsed.ToString());
                    }
                    string updatedActivityString = string.Join(";", dict.Select(item => item.Key + "=" + item.Value).ToArray());
                    await dbService.Update(updatedActivityString, foundUser.Result);

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
            
            var foundUser = dbService.Get(username);
            if (foundUser == null || foundUser.Result == null)
            {
                return;
            }
            UserData associatedAccount = foundUser.Result;
            // Convert the activity string to a dictionary format
            var dict = ActivityDictionary(associatedAccount.ActivityString);
            for (int i = 0; i < connectedUsers[username].Keys.ToList().Count; i++)
            {
                var userObject = connectedUsers[username][connectedUsers[username].Keys.ToList()[i]];
                string appName = GetAppName(userObject.WindowName);
                if (dict.ContainsKey(appName))
                {
                    int historyTime = Int32.Parse(dict[appName]);
                    int currentSessionTime = (int)(userObject.TotalTime + (DateTime.Now - userObject.StartTime).TotalMilliseconds);
                    dict[appName] = (historyTime + currentSessionTime).ToString();
                }
                else
                {
                    dict.Add(appName, ((int)(userObject.TotalTime + (DateTime.Now - userObject.StartTime).TotalMilliseconds)).ToString());
                }
            }
            await Clients.Client(correspondingConnections[username]).SendAsync("UpdateHistoryList", dict);
        }

        // Quicksort algorithm to sort the app usage times from least to greatest. Used to organize the user display.
        internal List<CustomCollection> SortCollections(string username)
        {
            Dictionary<int, CustomCollection> focus = connectedUsers[username];
            Console.WriteLine(focus.Count.ToString());
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
            List<CustomCollection> smallerRes;
            List<CustomCollection> largerRes;
            if (smaller.Count == 0)
            {
                smallerRes = new List<CustomCollection>();
            }else
            {
                smallerRes = QuickSort(smaller.Count - 1, smaller, smaller[smaller.Count - 1]);
            }
            if (larger.Count == 0)
            {
                largerRes = new List<CustomCollection>();
            }else
            {
                largerRes = QuickSort(larger.Count - 1, larger, larger[larger.Count - 1]);
            }
            
            result.AddRange(largerRes);
            result.AddRange(pivotList);
            result.AddRange(smallerRes);
            return result.ToList();
        }

        // Gets the timers for all the current processes. To be displayed on the home page.
        public async Task GetCurrentApplications(string username)
        {
            
            List<CustomCollection> processList = SortCollections(username);
            Console.WriteLine(processList.Count.ToString());
            Console.WriteLine(Context.ConnectionId);
            Console.WriteLine(Clients.Caller.ToString());
            await Clients.Client(Context.ConnectionId).SendAsync("UpdateList", processList);
            
        }


        public async Task setConnectionId(string username)
        {
            correspondingConnections[username] = Context.ConnectionId;
        }

        public async Task AddSignedUser(string username)
        {
            if (connectedUsers == null)
            {
                connectedUsers = new Dictionary<string, Dictionary<int, CustomCollection>>();
                correspondingConnections = new Dictionary<string, string>();
                
            }
            if (connectedUsers.Keys.ToList().Count == 0)
            {
                
                connectedUsers.Clear();
                correspondingConnections.Clear();
                await StartGlobalTimer();
            }
            
            Dictionary<int, CustomCollection> dict = new Dictionary<int, CustomCollection>();
            dict.Clear();
            connectedUsers.Add(username, dict);
            correspondingConnections.Add(username, Context.ConnectionId);
            
        }

        string GetAppName(string input)
        {
            int starting_index = 0;
            for (int i = 5; i < input.Length; i++)
            {
                if ((input[i - 5] == ' ') && (input[i - 1] == input[i - 5]) && (input[i - 2] == '-') && (input[i - 2] == input[i - 3]))
                {
                    starting_index = i;
                }
            }
            return input.Substring(starting_index, input.Length);
        }

        // Converts the activity string from the database to a dictionary of an app name and the corresponding time in milliseconds.
        private Dictionary<string, string> ActivityDictionary(string activityString)
        {
            return activityString.Split(';')
                .Select(section => section.Split('='))
                .Where(section => section.Length == 2)
                .ToDictionary(splitVal => splitVal[0], splitVal => splitVal[1]);
        }

    }
}
