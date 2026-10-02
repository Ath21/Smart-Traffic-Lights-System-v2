using System;
using Microsoft.EntityFrameworkCore;

namespace TrafficLightData.Repositories;

public abstract class BaseRepository<TEntity> where TEntity : class
{
    protected readonly TrafficLightDbContext _context;

    protected BaseRepository(TrafficLightDbContext context)
    {
        _context = context;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        => await _context.Set<TEntity>().ToListAsync();

    public virtual async Task<TEntity?> GetByIdAsync(int id)
        => await _context.Set<TEntity>().FindAsync(id);

    public virtual async Task InsertAsync(TEntity entity)
    {
        await _context.Set<TEntity>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}
