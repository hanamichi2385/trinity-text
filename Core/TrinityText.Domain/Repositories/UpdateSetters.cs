using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TrinityText.Domain
{
    /// <summary>
    /// Provider-agnostic description of a set-based UPDATE (<c>UPDATE ... SET p1 = v1, p2 = v2 WHERE ...</c>).
    /// Used by <see cref="IRepository{T}.ExecuteUpdateAsync{TEntity}"/>; each provider translates it to its own API.
    /// </summary>
    public sealed class UpdateSetters<TEntity> where TEntity : class
    {
        private readonly List<UpdateSetter> _items = new();

        public IReadOnlyList<UpdateSetter> Items => _items;

        public UpdateSetters<TEntity> Set<TProperty>(Expression<Func<TEntity, TProperty>> property, TProperty value)
        {
            _items.Add(new UpdateSetter(property, typeof(TProperty), value));
            return this;
        }
    }

    public sealed class UpdateSetter
    {
        public UpdateSetter(LambdaExpression property, Type propertyType, object value)
        {
            Property = property;
            PropertyType = propertyType;
            Value = value;
        }

        public LambdaExpression Property { get; }

        public Type PropertyType { get; }

        public object Value { get; }
    }
}
