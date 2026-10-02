namespace API.Furnistore.Application.Common
{
    public static class LikePattern
    {
        public const string Escape = "\\";

        public static string Contains(string value) =>
            $"%{value.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
    }
}
