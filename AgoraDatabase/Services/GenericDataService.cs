using AgoraDatabase.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase.Services
{
    // A service to pass data to and from the database through the DB  factory
    public class GenericDataService<T> : IDataService<T> where T : UserData
    {
        // Create a global factory for DB context
        private readonly UserDataContextFactory _contextFactory;
        public GenericDataService(UserDataContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<T> Create(T entity)
        {
            using(UserDataContext context = _contextFactory.CreateDbContext())
            {
                // Add a new entry into the database through the DB context.
                // TODO: work with the await call to eleviate the deadlocking issue with the UI
                EntityEntry<T> newEntity = await context.Set<T>().AddAsync(entity);
                await context.SaveChangesAsync();

                return newEntity.Entity;
            }
        }

        public async Task<bool> Delete(string userName)
        {
            using (UserDataContext context = _contextFactory.CreateDbContext())
            {
                // Attempts to find the requested user in the database
                var newEntity = await context.Set<T>().FirstOrDefaultAsync((e) => e.UserName == userName);
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
            using (UserDataContext context = _contextFactory.CreateDbContext())
            {
                // Find the user with Username username and return it's UserData object. If the Username doesn't exist in the database, return a default.
                // TODO: work with the await calls here to prevent deadlock in the UI
                T newEntity = await context.Set<T>().FirstOrDefaultAsync((e) => e.UserName == userName);
                return newEntity;
            }
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            // Get all users in the database. Return them as an async list (could change to fight deadlock)
            using (UserDataContext context = _contextFactory.CreateDbContext())
            {
                IEnumerable<T> allEntities = await context.Set<T>().ToListAsync();
                return allEntities;
            }
        }

        public async Task<T> Update(string newActivityString, T entity)
        {
            // Update a user's activity string for each time they close out of Agora. Update the Database to include the user's activity from their most recent session.
            // May possibly include another field to allow a user to change their password if they desire.
            using (UserDataContext context = _contextFactory.CreateDbContext())
            {
                entity.ActivityString = newActivityString;
                context.Set<T>().Update(entity);
                await context.SaveChangesAsync();

                return entity;
            }
        }
    }
}
