using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using AgoraDatabase;
using AgoraDatabase.Services;
using SocketServer;
using System.Timers;
using Timer = System.Timers.Timer;
using AgoraDatabase.Contexts;

namespace SocketServer
{
    class Program
    {
        private static byte[] _buffer = new byte[1024];
        private static Socket _serverSoc = 
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // Holds the active socket for every connected user.
        private static Dictionary<string, Socket> _correspondingSockets = new Dictionary<string, Socket>();

        // Dictionary with the connected user information. Username key followed by a dictionary with PID keys followed by corresponding app information and
        // a running timer.
        private static Dictionary<string, Dictionary<int, CustomCollection>> _connectedUsers = new Dictionary<string, Dictionary<int, CustomCollection>>();

        // A backlog of sockets that don't yet have a corresponding username.
        private static List<Socket> _identificationBacklog = new List<Socket>();

        static Timer minimizedTimer = new Timer(60000);
        static IDataService<UserData> dbService = new GenericDataService<UserData>(new UserDataContextFactory());

        static void Main(string[] args)
        {
            Setup();
            minimizedTimer.Elapsed += SendUpdateRequest;
            minimizedTimer.AutoReset = true;
            minimizedTimer.Enabled = true;
            Console.ReadLine();
        }

        private static void RegisterLogOn(object[] usernameList)
        {
            string username = (string)usernameList[0];
            Socket userSoc = (Socket)usernameList[1];
            _identificationBacklog.Remove(userSoc);
            _connectedUsers.Add(username, new Dictionary<int, CustomCollection>());
            _correspondingSockets.Add(username, userSoc);
        }

        private static void AddCurrentProcess(object[] arr)
        {

            // Intializes the timer to 30 mins

            // set the elapsed method. The NotifyUser method will need the connectionID as a precaution to know where to send the alert
            // The alert will also ask whether the user wants to close all the entire program <appName> or just the specific process <processName>.
            // The ID is passed to kill a single process if needed.
            Socket soc = (Socket)arr[1];
            if (!PollSocket(soc))
            {
                return;
            }
            string fullString = (String)arr[0];
            // Encoding to be used when passing data to prevent errors when splitting
            string[] splitString = fullString.Split(" $^% ");
            if (splitString.Length != 4)
            {
                return;
            }
            string username = splitString[0];
            int pid = Int32.Parse(splitString[1]);
            string processName = splitString[2];
            string appName = splitString[3];

            if (_connectedUsers.ContainsKey(username))
            {
                CustomCollection individualGroup = new CustomCollection(processName, appName);
                individualGroup.TimerAttribute.Elapsed += (sender, e) => NotifyUser(sender, e, username, soc, processName, pid, appName);
                individualGroup.TimerAttribute.Enabled = true;
                // create a static dictionary for the inner part of the dictionary storage.
                if (_connectedUsers[username].Count == 0)
                {
                    _connectedUsers[username] = new Dictionary<int, CustomCollection>();
                    _connectedUsers[username].Clear();
                }
                _connectedUsers[username].Add(pid, individualGroup);


                // Send to the client that a new process is there. This is to live update the home page
                // for new active processes.
                if (!PollSocket(_correspondingSockets[username]))
                {
                    return;
                }
                SendStrings("AddToList-" + processName, _correspondingSockets[username]);
                
            }
            else
            {
                // Method to be called if a process doesn't register. Called in the event of an error.
                SendStrings("FailedProcessReg", _correspondingSockets[username]);

            }

        }

        private async static void RemoveCurrentProcess(object[] arr)
        {
            string fullString = (String)arr[0];
            // Encoding to be used when passing data to prevent errors when splitting
            string[] splitString = fullString.Split(" $^% ");
            if (splitString.Length != 4)
            {
                return;
            }
            string username = splitString[0];
            int pid = Int32.Parse(splitString[1]);
            string processName = splitString[2];
            string appName = splitString[3];

            if (!PollSocket(_correspondingSockets[username]))
            {
                return;
            }

            if (_connectedUsers.ContainsKey(username))
            {
                if (_connectedUsers[username].ContainsKey(pid))
                {
                    double timeElapsed = (DateTime.Now - _connectedUsers[username][pid].StartTime).TotalMilliseconds;
                    _connectedUsers[username][pid].TotalTime += (int)timeElapsed;

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
                    
                    SendStrings("RemoveFromList-" + processName, _correspondingSockets[username]);

                    _connectedUsers[username][pid].TimerAttribute.Enabled = false;
                    _connectedUsers[username][pid].StopTimer();
                    _connectedUsers[username].Remove(pid);

                }
            }
            else
            {
                // Method to be called if a process doesn't register. Called in the event of an error.
                SendStrings("FailedProcessReg-" + processName, _correspondingSockets[username]);
            }
        }

        public static async void NotifyUser(object sender, ElapsedEventArgs e, string username, Socket soc, string processName, int pid, string appName)
        {
            // Increases the total timer by 30 minutes. Could change this at some point maybe
            _connectedUsers[username][pid].TotalTime += (1000 * 60 * 30);
            _connectedUsers[username][pid].resetStarted();
            string[] arrRep = { processName, pid.ToString(), appName, _connectedUsers[username][pid].TotalTime.ToString() };
            string infoString = String.Join(" $^% ",  arrRep);
            SendStrings("CreateTimeAlert-" + infoString, soc);

        }

        public static async void SendUpdateRequest(object sender, ElapsedEventArgs e)
        {
            for (int i = 0; i < _connectedUsers.Keys.ToList().Count; i++)
            {
                await GetCurrentApplications(_connectedUsers.Keys.ToList()[i]);
            }
        }

        // Request to be made to the client to update the minimized status of applications
        public static async Task IconicStatus()
        {
            // Method to be passed to all clients to receive the minimized status of each clients applications.


        }

        public static async Task GetCurrentApplications(string username)
        {

            List<CustomCollection> processList = SortCollections(username);

            if (_correspondingSockets[username] == null)
            {
                return;
            }
            string activityString = "";
            for (int i = 0; i < processList.Count; i++)
            {
                activityString += (processList[i].WindowName + ":" + (processList[i].TotalTime + ((DateTime.Now - processList[i].StartTime).TotalMilliseconds)).ToString());
                if (i != (processList.Count - 1))
                {
                    activityString += " $^% ";
                }

            }
            if (!PollSocket(_correspondingSockets[username]))
            {
                return;
            }
            Console.WriteLine("testing");
            SendStrings("UpdateList-" + activityString, _correspondingSockets[username]);
        }

        // The array "arr" will have the username as the first element and the socket to be set in the second element
        private async static void SetNewSocket(object[] arr)
        {
            string username = (string)arr[0];
            Socket callingSoc = (Socket)arr[1];
            if (!PollSocket(callingSoc))
            {
                return;
            }
            _correspondingSockets[username] = callingSoc;
        }

        internal static List<CustomCollection> SortCollections(string username)
        {
            Dictionary<int, CustomCollection> focus = _connectedUsers[username];
            Console.WriteLine(focus.Count.ToString());
            List<CustomCollection> listOfValues = focus.Values.ToList();
            CustomCollection pivot = listOfValues[listOfValues.Count - 1];
            return QuickSort(listOfValues.Count - 1, listOfValues, pivot);
        }

        internal static List<CustomCollection> QuickSort(int endIndex, List<CustomCollection> collection, CustomCollection pivot)
        {
            if (collection.Count <= 1)
            {
                return collection;
            }
            List<CustomCollection> smaller = new List<CustomCollection>();
            List<CustomCollection> larger = new List<CustomCollection>();

            for (int i = 0; i < endIndex; i++)
            {

                if ((collection[i].TotalTime + (DateTime.Now - collection[i].StartTime).TotalMilliseconds) < (pivot.TotalTime + (DateTime.Now - pivot.StartTime).TotalMilliseconds))
                {
                    smaller.Add(collection[i]);
                }
                else
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
            }
            else
            {
                smallerRes = QuickSort(smaller.Count - 1, smaller, smaller[smaller.Count - 1]);
            }
            if (larger.Count == 0)
            {
                largerRes = new List<CustomCollection>();
            }
            else
            {
                largerRes = QuickSort(larger.Count - 1, larger, larger[larger.Count - 1]);
            }

            result.AddRange(largerRes);
            result.AddRange(pivotList);
            result.AddRange(smallerRes);
            return result.ToList();
        }

        private static void Setup()
        {
            Console.WriteLine("setting up...");
            _serverSoc.Bind(new IPEndPoint(IPAddress.Any, 7313));
            _serverSoc.Listen(10);
            _serverSoc.BeginAccept(new AsyncCallback(AcceptCallback), null);

        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket soc = _serverSoc.EndAccept(ar);
            _identificationBacklog.Add(soc);
            soc.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), soc);
            _serverSoc.BeginAccept(new AsyncCallback(AcceptCallback), null);
            Console.WriteLine("New client connected");
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Socket soc = (Socket)ar.AsyncState;
            int dataReceived = soc.EndReceive(ar);

            byte[] tempbuf = new byte[dataReceived];
            Array.Copy(_buffer, tempbuf, dataReceived);

            string text = Encoding.ASCII.GetString(tempbuf);
            string[] allCommands = text.Split("\r\n");

            Console.WriteLine(text);
            Console.WriteLine(allCommands.Length);
            Type serverType = typeof(Program);
            for (int i = 0; i < allCommands.Length; i++)
            {
                string cmd = allCommands[i];

                Console.WriteLine(cmd);

                int dashIndex = cmd.IndexOf('-');
                if (dashIndex == -1)
                {
                    return;
                }
                string methodString = cmd.Substring(0, dashIndex);
                string parametersString;
                if (dashIndex + 1 >= cmd.Length)
                {
                    parametersString = "";
                }
                else
                {
                    parametersString = cmd.Substring(dashIndex + 1);
                }
                try
                {
                    MethodInfo definedMethod = serverType.GetMethod(methodString, BindingFlags.NonPublic | BindingFlags.Instance);
                    object[] arr = { parametersString, soc };
                    definedMethod.Invoke(definedMethod, arr);
                }
                catch
                {
                    return;
                }
            }
            
        }

        private static void SendStrings(string text, Socket clientSoc)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            clientSoc.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), clientSoc);
            clientSoc.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSoc);
        }

        private static bool PollSocket(Socket soc)
        {
            bool polledStatus = soc.Poll(1000, SelectMode.SelectRead);
            bool availableStatus = (soc.Available == 0);

            if ((polledStatus && availableStatus) || !soc.Connected)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        private static void SendCallback(IAsyncResult ar)
        {
            Socket soc = (Socket)ar.AsyncState;
            soc.EndSend(ar);
        }

        // Converts the activity string of a certain program to dictionary form.
        private static Dictionary<string, string> ActivityDictionary(string activityString)
        {
            return activityString.Split(';')
                .Select(section => section.Split('='))
                .Where(section => section.Length == 2)
                .ToDictionary(splitVal => splitVal[0], splitVal => splitVal[1]);
        }

        // Attempts to get the actual program name from the window name.
        private static string GetAppName(string input)
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
    }
}
