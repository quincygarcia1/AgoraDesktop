﻿using Microsoft.AspNetCore.SignalR;
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
        Dictionary<string, Dictionary<int, ArrayList>> connectedUsers;

        // constant timer to be used to check whether processes are minimized.
        Timer minimzedTimer = new Timer(60000);
        


        // Method to notify a user that they've had an app minimized for a while
        public async Task NotifyUser(object sender, ElapsedEventArgs e, string connectionId, string processName, int pid, string appName)
        {

        }

        // Method to add a process for a client's active applications. An entry with a clock should be started.
        public async Task AddCurrentProcess(string processName, int pid, string appName)
        {
            // Intializes the timer to 30 mins
            Timer individualTimer = new Timer(1000 * 60 * 30);
            // set the elapsed method. The NotifyUser method will need the connectionID as a precaution to know where to send the alert
            // The alert will also ask whether the user wants to close all the entire program <appName> or just the specific process <processName>.
            // The ID is passed to kill a single process if needed.
            individualTimer.Elapsed += (sender, e) => NotifyUser(sender, e, Context.ConnectionId, processName, pid, appName);
            individualTimer.AutoReset = true;
            individualTimer.Enabled = true;
            if (connectedUsers.ContainsKey(Context.ConnectionId))
            {
                connectedUsers[Context.ConnectionId].Add(pid, new ArrayList()
                {
                    processName, appName, individualTimer
                });

            }
            else
            {
                await Clients.Caller.SendAsync("FailedProcessReg", processName, pid, appName);
            }

        }


        // Request to be made to the client to update the minimized status of applications
        public async Task IconicStatus()
        {

        }


        // Method to be called when a user stops an application on their computer. Stop the process clock
        // and properly update the user's lifetime app history
        public async Task RemoveCurrentProcess()
        {

        }


        // Method to transfer the app history of a client. Make sure to add their current session activity to the total.
        public async Task GetClientAppHistory()
        {
            
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
        public async Task IntializeNewUsing()
        {
            
        }
    }
}
