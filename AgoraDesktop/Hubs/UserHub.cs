using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDesktop.Hubs
{
    public class UserHub : Hub
    {
        // TODO: Keep a collection of connectionIDs, the login info corresponding to the connection and the timers for the
        // clients active processes

        // Method to notify a user that they've had an app minimized for a while
        public async Task NotifyUser()
        {

        }

        // Method to add a process for a client's active applications. An entry with a clock should be started.
        public async Task AddCurrentProcess()
        {

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
