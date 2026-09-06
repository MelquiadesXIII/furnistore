namespace API.Furnistore.Shared.Common
{
    public enum ErrorType
    {
        Validation,
        NotFound,
        Conflict,
        Unauthorized,
        Forbidden,
        Unexpected,
    }

    public sealed record Error(string Code, string Message, ErrorType Type)
    {
        public static Error Validation(string code, string message) =>
            new(code, message, ErrorType.Validation);

        public static Error NotFound(string code, string message) =>
            new(code, message, ErrorType.NotFound);

        public static Error Conflict(string code, string message) =>
            new(code, message, ErrorType.Conflict);

        public static Error Unauthorized(string code, string message) =>
            new(code, message, ErrorType.Unauthorized);

        public static Error Forbidden(string code, string message) =>
            new(code, message, ErrorType.Forbidden);

        public static Error Unexpected(string code, string message) =>
            new(code, message, ErrorType.Unexpected);
    }

    public class Result
    {
        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }

        public Error? Error { get; }

        public static Result Ok() => new(true, null);

        public static Result Fail(Error error) => new(false, error);

        public static Result<T> Ok<T>(T value) => new(value, true, null);

        public static Result<T> Fail<T>(Error error) => new(default, false, error);
    }

    public sealed class Result<T> : Result
    {
        private readonly T? _value;

        internal Result(T? value, bool isSuccess, Error? error)
            : base(isSuccess, error) => _value = value;

        public T Value =>
            IsSuccess
                ? _value!
                : throw new InvalidOperationException("No se puede leer Value de un Result fallido.");
    }
}
