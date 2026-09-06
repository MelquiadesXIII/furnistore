using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Common
{
    public static class ApiEvents
    {
        public static readonly EventId ProductCreated = new(1001, nameof(ProductCreated));
        public static readonly EventId ProductUpdated = new(1002, nameof(ProductUpdated));
        public static readonly EventId ProductDeleted = new(1003, nameof(ProductDeleted));
        public static readonly EventId ProductNotFound = new(1010, nameof(ProductNotFound));
        public static readonly EventId ProductCategoryMissing = new(1011, nameof(ProductCategoryMissing));

        public static readonly EventId CategoryCreated = new(1101, nameof(CategoryCreated));
        public static readonly EventId CategoryUpdated = new(1102, nameof(CategoryUpdated));
        public static readonly EventId CategoryDeleted = new(1103, nameof(CategoryDeleted));
        public static readonly EventId CategoryNotFound = new(1110, nameof(CategoryNotFound));
        public static readonly EventId CategoryNameTaken = new(1111, nameof(CategoryNameTaken));
        public static readonly EventId CategoryInUse = new(1112, nameof(CategoryInUse));

        public static readonly EventId ClientCreated = new(1201, nameof(ClientCreated));
        public static readonly EventId ClientUpdated = new(1202, nameof(ClientUpdated));
        public static readonly EventId ClientDeleted = new(1203, nameof(ClientDeleted));
        public static readonly EventId ClientNotFound = new(1210, nameof(ClientNotFound));

        public static readonly EventId UserRegistered = new(2001, nameof(UserRegistered));
        public static readonly EventId LoginSucceeded = new(2002, nameof(LoginSucceeded));
        public static readonly EventId TokenRefreshed = new(2003, nameof(TokenRefreshed));
        public static readonly EventId EmailConfirmed = new(2004, nameof(EmailConfirmed));
        public static readonly EventId LoginFailed = new(2010, nameof(LoginFailed));
        public static readonly EventId RegistrationRejected = new(2011, nameof(RegistrationRejected));
        public static readonly EventId RefreshTokenRejected = new(2012, nameof(RefreshTokenRejected));
        public static readonly EventId EmailConfirmationFailed = new(2013, nameof(EmailConfirmationFailed));
        public static readonly EventId EmailSendFailed = new(2020, nameof(EmailSendFailed));

        public static readonly EventId OrderCreated = new(3001, nameof(OrderCreated));
        public static readonly EventId OrderUpdated = new(3002, nameof(OrderUpdated));
        public static readonly EventId OrderDeleted = new(3003, nameof(OrderDeleted));
        public static readonly EventId OrderNotFound = new(3010, nameof(OrderNotFound));
        public static readonly EventId OrderRejected = new(3011, nameof(OrderRejected));

        public static readonly EventId RequestCompleted = new(5001, nameof(RequestCompleted));
        public static readonly EventId UnhandledException = new(5002, nameof(UnhandledException));
        public static readonly EventId SlowRequest = new(5003, nameof(SlowRequest));
        public static readonly EventId EndpointsRegistered = new(5004, nameof(EndpointsRegistered));
        public static readonly EventId DatabaseWarmedUp = new(5005, nameof(DatabaseWarmedUp));
        public static readonly EventId DatabaseWarmupFailed = new(5006, nameof(DatabaseWarmupFailed));
    }
}
