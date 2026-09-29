using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;

namespace TrinityText.Domain.EF
{
    public class EFRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly TrinityEFContext _trinityDbContext;

        public EFRepository(TrinityEFContext trinityDbContext)
        {
            _trinityDbContext = trinityDbContext;
        }

        public IQueryable<T> Repository => _trinityDbContext.Set<T>().AsQueryable<T>();

        public string ConnectionString => _trinityDbContext.ConnectionString;

        public async Task<T> Create(T newEntity)
        {
            var entity = await _trinityDbContext.AddAsync(newEntity);
            await _trinityDbContext.SaveChangesAsync();

            return entity.Entity;
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _trinityDbContext.AddRangeAsync(entities);
            await _trinityDbContext.SaveChangesAsync();
        }

        public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> source)
            => source.ToListAsync();

        public Task<TResult> FirstOrDefaultAsync<TResult>(IQueryable<TResult> source)
            => source.FirstOrDefaultAsync();

        public Task<int> CountAsync<TResult>(IQueryable<TResult> source)
            => source.CountAsync();

        public Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> source) where TEntity : class
            => source.ExecuteDeleteAsync();

        public Task<int> ExecuteUpdateAsync<TEntity>(IQueryable<TEntity> source, Action<UpdateSetters<TEntity>> configure) where TEntity : class
        {
            var setters = new UpdateSetters<TEntity>();
            configure(setters);
            if (setters.Items.Count == 0)
            {
                return Task.FromResult(0);
            }

            // s => s.SetProperty(x => x.A, valueA).SetProperty(x => x.B, valueB)...
            var callsType = typeof(SetPropertyCalls<TEntity>);
            var parameter = Expression.Parameter(callsType, "s");
            Expression body = parameter;
            foreach (var setter in setters.Items)
            {
                // EF Core 8 overload (Func<T, TProp>, TProp): second parameter is the generic argument itself.
                // Inside an expression tree the property selector is passed as a plain lambda node (no Quote).
                var method = callsType
                    .GetMethods()
                    .Single(m => m.Name == nameof(SetPropertyCalls<TEntity>.SetProperty)
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType.IsGenericParameter)
                    .MakeGenericMethod(setter.PropertyType);

                // the value goes through a closure field so EF sends it as a parameter (query plan reuse)
                var box = Activator.CreateInstance(typeof(StrongBox<>).MakeGenericType(setter.PropertyType));
                box.GetType().GetField(nameof(StrongBox<int>.Value)).SetValue(box, setter.Value);
                var value = Expression.Field(Expression.Constant(box), nameof(StrongBox<int>.Value));

                body = Expression.Call(body, method, setter.Property, value);
            }

            var lambda = Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(body, parameter);
            return source.ExecuteUpdateAsync(lambda);
        }

        public async Task Delete(T entityToDelete)
        {
            _trinityDbContext.Remove(entityToDelete);
            await _trinityDbContext.SaveChangesAsync();
        }

        public async Task<T> Read(params object[] id)
        {
            var entity = await _trinityDbContext.FindAsync<T>(id);

            return entity;
        }

        public async Task<T> Update(T modifiedEntity)
        {
            _trinityDbContext.ChangeTracker.Clear();
            var entity = _trinityDbContext.Update(modifiedEntity);
            await _trinityDbContext.SaveChangesAsync();

            return entity.Entity;
        }

        public async Task BeginTransaction() 
        {
            await _trinityDbContext.BeginTransaction();
        }

        public async Task CommitTransaction()
        {
            await _trinityDbContext.CommitTransaction();
        }

        public async Task RollbackTransaction()
        {
            await _trinityDbContext.RollbackTransaction();
        }
    }
}
