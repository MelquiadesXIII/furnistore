using API.Furnistore.Shared.Common;

namespace API.Furnistore.Application.Admin.Reports
{
    public static class StoreCalendar
    {
        public const string TimeZoneId = "America/Havana";
        public const string Currency = "USD";

        private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

        public static DateOnly Today(TimeProvider clock) => DateOf(clock.GetUtcNow().UtcDateTime);

        public static DateOnly DateOf(DateTime utc) =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));

        public static DateTime StartOfDayUtc(DateOnly day)
        {
            var local = day.ToDateTime(TimeOnly.MinValue);
            while (Zone.IsInvalidTime(local))
                local = local.AddMinutes(30);
            return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
        }
    }

    public readonly record struct ReportBucket(DateOnly Start, DateOnly End)
    {
        public bool Contains(DateOnly day) => day >= Start && day <= End;
    }

    public sealed record ReportPeriod(
        DateOnly From,
        DateOnly To,
        ReportGrouping GroupBy,
        IReadOnlyList<ReportGrouping> AvailableGroupings
    )
    {
        public const int MaxDays = 1096;

        public DateTime StartUtc => StoreCalendar.StartOfDayUtc(From);

        public DateTime EndUtc => StoreCalendar.StartOfDayUtc(To.AddDays(1));

        public ReportRange Range => new(From, To);

        public static Result<ReportPeriod> Resolve(ReportQuery query, DateOnly today)
        {
            var to = query.To ?? today;
            var from = query.From ?? to.AddDays(-29);

            if (from > to)
                return Result.Fail<ReportPeriod>(
                    Error.Validation("report.invalid_range", "La fecha inicial no puede ser posterior a la final.")
                );

            var days = to.DayNumber - from.DayNumber + 1;
            if (days > MaxDays)
                return Result.Fail<ReportPeriod>(
                    Error.Validation("report.range_too_long", "El rango no puede superar los 3 años.")
                );

            var available = AvailableFor(days);
            var groupBy = query.GroupBy is ReportGrouping requested && available.Contains(requested)
                ? requested
                : DefaultFor(days);

            return Result.Ok(new ReportPeriod(from, to, groupBy, available));
        }

        public IReadOnlyList<ReportBucket> Buckets()
        {
            var buckets = new List<ReportBucket>();
            var start = From;

            while (start <= To)
            {
                var end = GroupBy switch
                {
                    ReportGrouping.Week => start.AddDays(6 - ((int)start.DayOfWeek + 6) % 7),
                    ReportGrouping.Month => new DateOnly(start.Year, start.Month, 1).AddMonths(1).AddDays(-1),
                    _ => start,
                };

                if (end > To)
                    end = To;

                buckets.Add(new ReportBucket(start, end));
                start = end.AddDays(1);
            }

            return buckets;
        }

        private static IReadOnlyList<ReportGrouping> AvailableFor(int days)
        {
            var available = new List<ReportGrouping>();
            if (days <= 93)
                available.Add(ReportGrouping.Day);
            if (days >= 14 && days <= 731)
                available.Add(ReportGrouping.Week);
            if (days >= 56)
                available.Add(ReportGrouping.Month);
            return available;
        }

        private static ReportGrouping DefaultFor(int days) =>
            days switch
            {
                <= 31 => ReportGrouping.Day,
                <= 180 => ReportGrouping.Week,
                _ => ReportGrouping.Month,
            };
    }

    public static class ReportMath
    {
        public static decimal Share(decimal part, decimal total) => total == 0 ? 0 : Math.Round(part / total, 4);

        public static decimal Average(decimal total, int count) => count == 0 ? 0 : Math.Round(total / count, 2);
    }
}
