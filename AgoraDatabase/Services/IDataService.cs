using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase.Services
{
    internal interface IDataService<T>
    {
        Task<IEnumerable<T>> GetAll();

        Task<T> Get(int id);

        Task<T> Create(T identity);

        Task<T> Update(int id, T entity);

        Task<bool> Delete(int id);
    }
}
