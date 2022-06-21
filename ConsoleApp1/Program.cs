

// See https://aka.ms/new-console-template for more information
using AgoraDatabase;
using AgoraDatabase.Contexts;
using AgoraDatabase.Services;

IDataService<UserData> dbService = new GenericDataService<UserData>(new UserDataContextFactory());

if (dbService.Get("bob").Result == null)
{
    Console.WriteLine("what");
}

