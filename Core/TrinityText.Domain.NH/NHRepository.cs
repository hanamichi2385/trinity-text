using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrinityText.Domain.NH
{
    public class NHRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly TrinityNHContext _dataContext;

        public string ConnectionString => _dataContext.ConnectionString;

        public IQueryable<T> Repository => _dataContext.CurrentSession.Query<T>();

        public NHRepository(TrinityNHContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task BeginTransaction()
        {
            await _dataContext.BeginTransaction();
        }

        public async Task CommitTransaction()
        {
            await _dataContext.CommitTransaction();
        }

        public async Task RollbackTransaction()
        {
            await _dataContext.RollbackTransaction();
        }

        public async Task<T> Create(T newEntity)
        {
            // SaveAsync returns the generated identifier, not the entity
            await _dataContext.CurrentSession.SaveAsync(newEntity);
            await _dataContext.FlushIfNoTransaction();

            return newEntity;
        }

        public async Task Delete(T entityToDelete)
        {
            await _dataContext.CurrentSession.DeleteAsync(entityToDelete);
            await _dataContext.FlushIfNoTransaction();
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                await _dataContext.CurrentSession.SaveAsync(entity);
            }
            await _dataContext.CurrentSession.FlushAsync();
        }

        public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> source)
            => LinqExtensionMethods.ToListAsync(source);

        public Task<TResult> FirstOrDefaultAsync<TResult>(IQueryable<TResult> source)
            => LinqExtensionMethods.FirstOrDefaultAsync(source);

        public Task<int> CountAsync<TResult>(IQueryable<TResult> source)
            => LinqExtensionMethods.CountAsync(source);

        public Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> source) where TEntity : class
            => DmlExtensionMethods.DeleteAsync(source);

        public Task<int> ExecuteUpdateAsync<TEntity>(IQueryable<TEntity> source, Action<UpdateSetters<TEntity>> configure) where TEntity : class
        {
            var setters = new UpdateSetters<TEntity>();
            configure(setters);
            if (setters.Items.Count == 0)
            {
                return Task.FromResult(0);
            }

            var builder = DmlExtensionMethods.UpdateBuilder(source);
            foreach (var setter in setters.Items)
            {
                // IUpdateBuilder<T>.Set<TProp>(Expression<Func<T, TProp>>, TProp)
                var method = typeof(UpdateBuilder<TEntity>)
                    .GetMethods()
                    .Single(m => m.Name == nameof(UpdateBuilder<TEntity>.Set)
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType.IsGenericParameter)
                    .MakeGenericMethod(setter.PropertyType);

                builder = (UpdateBuilder<TEntity>)method.Invoke(builder, [setter.Property, setter.Value]);
            }

            return builder.UpdateAsync();
        }

        public async Task<T> Read(params object[] id)
        {
            // GetAsync takes the identifier itself, not an array containing it
            if (id == null || id.Length != 1)
            {
                throw new NotSupportedException("Read supports a single identifier value");
            }

            var entity = await _dataContext.CurrentSession.GetAsync<T>(id[0]);

            return entity;
        }

       

        public async Task<T> Update(T modifiedEntity)
        {
            await _dataContext.CurrentSession.UpdateAsync(modifiedEntity);
            await _dataContext.FlushIfNoTransaction();

            return modifiedEntity;
        }
    }
}