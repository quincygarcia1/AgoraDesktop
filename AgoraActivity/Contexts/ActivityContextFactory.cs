using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraActivity.Contexts
{
    public class ActivityContextFactory : IDesignTimeDbContextFactory<ActivityContext>
    {
        public ActivityContext CreateDbContext(string[] args = null)
        {
            var options = new DbContextOptionsBuilder<ActivityContext>();
            options.UseSqlServer("Server=localhost\\SQLEXPRESS01;Database=AgoraActivity;Trusted_Connection=True;");
            return new ActivityContext(options.Options);
        }
    }
}
