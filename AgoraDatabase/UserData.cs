using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase
{
    // A representation of data kept in the database
    public class UserData
    {
        // obligatory ID key
        public int ID { get; set; }
        // Username for each user
        public string UserName { get; set; }
        // The corresponding password for a user.
        public string Password { get; set; }
        // An individual user's "activity string". This is a string representation of a user's total app history.
        // The activity string will update after closing Agora to reflect the previous session's activity.
        public string ActivityString { get; set; }  

    }
}
