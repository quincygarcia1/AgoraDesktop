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
    public class GenericDataService<T> : IDataService<T> where T : UserData
    {

        private readonly UserDataContextFactory _contextFactory;
        public GenericDataService(UserDataContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<T> Create(T entity)
        {
            using(UserDataContext context = _contextFactory.CreateDbContext())
            {
                EntityEntry<T> newEntity = await context.Set<T>().AddAsync(entity);
                await context.SaveChangesAsync();

                return newEntity.Entity;
            }
        }

        public async Task<bool> Delete(string userName)
        {
            using (UserDataContext context = _contextFactory.CreateDbContext())
            {
                var newEntity = await context.Set<T>().FirstOrDefaultAsync((e) => e.UserName == userName);
                if (newEntity != null)
                {
                    T entity = newEntity;
                    context.Set<T>().Remove(entity);
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            
        }

        public async Task<T> Get(string userName)
        {
            using (UserDataContext context = _contextFactory.CreateDbContext())
            {
                var newEntity = await context.Set<T>().FirstOrDefaultAsync((e) => e.UserName == userName);
                if (newEntity == null)
                {
                    return null;
                }
                T entityCopy = newEntity;
                return entityCopy;
            }
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            using (UserDataContext context = _contextFactory.CreateDbContext())
            {
                IEnumerable<T> allEntities = await context.Set<T>().ToListAsync();
                return allEntities;
            }
        }

        public async Task<T> Update(string newActivityString, T entity)
        {
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
