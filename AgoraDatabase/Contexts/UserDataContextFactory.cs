﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase.Contexts
{

    // A factory pattern used to create new DB contexts for the login, register, and possibly delete user pages.
    public class UserDataContextFactory : IDesignTimeDbContextFactory<UserDataContext>
    {
        public UserDataContext CreateDbContext(string[] args = null)
        {
            var options = new DbContextOptionsBuilder<UserDataContext>();
            options.UseSqlServer("Server=localhost\\SQLEXPRESS01;Database=master;Trusted_Connection=True;");
            return new UserDataContext(options.Options);
        }
    }
}
