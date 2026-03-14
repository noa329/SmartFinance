using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<List<T>> GetAll();
        Task<T?> GetById(int id);
        Task<T> Add(T entity);
        Task<T> Update(int id,T entity);
        Task Delete(int id);
        Task<List<T>> Find(Expression<Func<T, bool>> predicate);
    }
}
