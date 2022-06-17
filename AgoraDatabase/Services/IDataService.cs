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

        Task<T> Get(string userName);

        Task<T> Create(T identity);

        Task<T> Update(string newActivityString, T entity);

        Task<bool> Delete(string userName);
    }
}
