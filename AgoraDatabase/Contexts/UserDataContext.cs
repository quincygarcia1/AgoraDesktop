using AgoraDatabase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase
{
    // DB context which provides the database with a set of UserData
    public class UserDataContext : DbContext
    {

        // Set this to a custom type
        public DbSet<UserData> UserData { get; set; }

        public UserDataContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
