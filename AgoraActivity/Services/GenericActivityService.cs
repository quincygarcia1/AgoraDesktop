using AgoraActivity.Contexts;
using AgoraActivity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraActivity.Services
{
    public class GenericActivityService<T> : IActivityService<T> where T : CurrentActivity
    {
        // Create a global factory for DB context
        private readonly ActivityContextFactory _contextFactory;
        public GenericActivityService(ActivityContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<T> Create(T entity)
        {
            using (ActivityContext context = _contextFactory.CreateDbContext())
            {
                // Add a new entry into the database through the DB context.
                // TODO: work with the await call to eleviate the deadlocking issue with the UI
                var newEntity = context.Set<T>().AddAsync(entity);
                await context.SaveChangesAsync();

                return newEntity.Result.Entity;
            }
        }

        public async Task<bool> Delete(string userName)
        {
            using (ActivityContext context = _contextFactory.CreateDbContext())
            {
                // Attempts to find the requested user in the database
                var newEntity = await context.Set<T>().FirstOrDefaultAsync((e) => e.Username == userName);
                if (newEntity != null)
                {
                    // if the User is found, remove it and return an indication that the deletion worked
                    T entity = newEntity;
                    context.Set<T>().Remove(entity);
                    await context.SaveChangesAsync();
                    return true;
                }
                // return an indication that the deletion didn't work
                return false;
            }

        }
        public async Task<T> Get(string userName)
        {
            using (ActivityContext context = _contextFactory.CreateDbContext())
            {
                // Find the user with Username username and return it's UserData object. If the Username doesn't exist in the database, return a default.
                // TODO: work with the await calls here to prevent deadlock in the UI
                var newEntity = context.Set<T>().FirstOrDefaultAsync((e) => e.Username == userName);

                if (newEntity == null || Object.Equals(newEntity, default(T)))
                {
                    return null;
                }
                return await newEntity.ConfigureAwait(false);
            }
        }


        public async Task<T> Update(string newActivityString, T entity)
        {
            // Update a user's activity string for each time they close out of Agora. Update the Database to include the user's activity from their most recent session.
            // May possibly include another field to allow a user to change their password if they desire.
            using (ActivityContext context = _contextFactory.CreateDbContext())
            {
                entity.SessionActivity = newActivityString;
                context.Set<T>().Update(entity);
                await context.SaveChangesAsync();

                return entity;
            }
        }
    }
}
