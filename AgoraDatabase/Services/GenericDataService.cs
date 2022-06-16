using AgoraDatabase.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase.Services
{
    public class GenericDataService<T> : IDataService<T> where T : class
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
                var newEntity = await context.Set<T>().AddAsync(entity);
                await context.SaveChangesAsync();

                return newEntity.Entity;
            }
        }

        public async Task<bool> Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<T> Get(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            throw new NotImplementedException();
        }

        public async Task<T> Update(int id, T entity)
        {
            throw new NotImplementedException();
        }
    }
}
