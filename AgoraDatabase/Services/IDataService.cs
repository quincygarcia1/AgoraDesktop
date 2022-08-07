using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDatabase.Services
{
    // Interface used for services that interact with the database.
    // Implemented CRUD operations for database interactions
    public interface IDataService<T>
    {
        // Read
        Task<T> Get(string userName);
        // Create
        Task<T> Create(T identity);
        // Update
        Task<T> Update(string newActivityString, T entity);
        // Delete
        Task<bool> Delete(string userName);
    }
}
