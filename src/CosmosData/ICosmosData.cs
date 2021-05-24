using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosData
{
    public interface ICosmosData<T> where T : ICosmosModel
    {
        Task<T> Create(T item);
        Task Delete(string id, string pk, string eTag);
        Task<T> Get(string id, string pk);
        Task<IEnumerable<T>> GetAll();
        Task<T> Replace(T item, string ifMatch);
    }
}