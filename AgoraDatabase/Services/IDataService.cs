using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase.Services
{
    // Interface used for services that interact with the database.
    public interface IDataService<T>
    {

        Task<T> Get(string userName);

        Task<T> Create(T identity);

        Task<T> Update(string newActivityString, T entity);

        Task<bool> Delete(string userName);
    }
}
