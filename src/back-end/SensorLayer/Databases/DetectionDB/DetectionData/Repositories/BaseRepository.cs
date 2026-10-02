using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace DetectionData.Repositories;

public abstract class BaseRepository<T>
{
    protected readonly IMongoCollection<T> _collection;

    protected BaseRepository(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var result = await _collection.FindAsync(_ => true);
        return await result.ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(string id, Expression<Func<T, string?>> idSelector)
    {
        var filter = Builders<T>.Filter.Eq(idSelector, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var result = await _collection.FindAsync(predicate);
        return await result.ToListAsync();
    }

    public virtual async Task InsertAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
    }

    public virtual async Task InsertManyAsync(IEnumerable<T> entities)
    {
        await _collection.InsertManyAsync(entities);
    }

    public virtual async Task DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        await _collection.DeleteManyAsync(predicate);
    }
}