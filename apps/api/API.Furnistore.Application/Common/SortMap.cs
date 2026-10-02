using System.Linq.Expressions;
using API.Furnistore.Shared.Common;

namespace API.Furnistore.Application.Common
{
    public sealed class SortMap<T>
    {
        private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> keys =
            new(StringComparer.OrdinalIgnoreCase);

        public SortMap<T> Add<TKey>(string name, Expression<Func<T, TKey>> key)
        {
            keys[name] = (query, descending) =>
                descending ? query.OrderByDescending(key) : query.OrderBy(key);
            return this;
        }

        public IReadOnlyCollection<string> Names => keys.Keys;

        public Result<IOrderedQueryable<T>> Apply(IQueryable<T> query, string? sort, string fallback)
        {
            var requested = string.IsNullOrWhiteSpace(sort) ? fallback : sort.Trim();
            var descending = requested.StartsWith('-');
            var name = descending ? requested[1..] : requested;

            if (!keys.TryGetValue(name, out var order))
                return Result.Fail<IOrderedQueryable<T>>(
                    Error.Validation(
                        "query.invalid_sort",
                        $"No se puede ordenar por «{name}». Opciones: {string.Join(", ", keys.Keys)}."
                    )
                );

            return Result.Ok(order(query, descending));
        }
    }
}
