namespace API.Furnistore.Shared.Common
{
    public sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Total,
        int Page,
        int PageSize
    )
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
    }
}
