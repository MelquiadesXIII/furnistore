using API.Furnistore.Application.Common;
using API.Furnistore.Application.Orders;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using API.Furnistore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Furnistore.Application.Carts
{
    public sealed class CartService(APIFurnistoreContext db, ILogger<CartService> logger)
    {
        public async Task<Result<CartResponse>> GetAsync(string userId, CancellationToken cancellationToken)
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Ok(new CartResponse([], 0m, 0m, 0m));

            return Result.Ok(await BuildCartResponseAsync(clientId.Value, cancellationToken));
        }

        public async Task<Result<CartResponse>> AddItemAsync(
            string userId,
            AddCartItemRequest request,
            CancellationToken cancellationToken
        )
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Fail<CartResponse>(SelfNotFound());

            var product = await db.Products.FirstOrDefaultAsync(
                p => p.Id == request.ProductId,
                cancellationToken
            );
            if (product is null)
                return Result.Fail<CartResponse>(ProductNotFound(request.ProductId));

            if (!product.IsActive)
                return Result.Fail<CartResponse>(ProductUnavailable(product.Id));

            var item = await db.CartItems.FirstOrDefaultAsync(
                ci => ci.ClientId == clientId.Value && ci.ProductId == request.ProductId,
                cancellationToken
            );

            var newQuantity = (item?.Quantity ?? 0) + request.Quantity;
            if (newQuantity > product.Stock)
                return Result.Fail<CartResponse>(InsufficientStock(product.Id, product.Stock));

            if (item is null)
            {
                db.CartItems.Add(
                    new CartItem
                    {
                        ClientId = clientId.Value,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                    }
                );
            }
            else
            {
                item.Quantity = newQuantity;
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.CartItemAdded,
                "Client {ClientId} added product {ProductId} (qty {Quantity}) to cart",
                clientId,
                request.ProductId,
                request.Quantity
            );

            return Result.Ok(await BuildCartResponseAsync(clientId.Value, cancellationToken));
        }

        public async Task<Result<CartResponse>> SetItemQuantityAsync(
            string userId,
            int productId,
            UpdateCartItemRequest request,
            CancellationToken cancellationToken
        )
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Fail<CartResponse>(SelfNotFound());

            var item = await db.CartItems.FirstOrDefaultAsync(
                ci => ci.ClientId == clientId.Value && ci.ProductId == productId,
                cancellationToken
            );
            if (item is null)
                return Result.Fail<CartResponse>(ItemNotFound(productId));

            var product = await db.Products
                .Where(p => p.Id == productId)
                .Select(p => new { p.Stock, p.IsActive })
                .FirstOrDefaultAsync(cancellationToken);
            if (product is null)
                return Result.Fail<CartResponse>(ProductNotFound(productId));

            if (!product.IsActive)
                return Result.Fail<CartResponse>(ProductUnavailable(productId));

            if (request.Quantity > product.Stock)
                return Result.Fail<CartResponse>(InsufficientStock(productId, product.Stock));

            item.Quantity = request.Quantity;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ApiEvents.CartItemUpdated,
                "Client {ClientId} set product {ProductId} to qty {Quantity} in cart",
                clientId,
                productId,
                request.Quantity
            );

            return Result.Ok(await BuildCartResponseAsync(clientId.Value, cancellationToken));
        }

        public async Task<Result<CartResponse>> RemoveItemAsync(
            string userId,
            int productId,
            CancellationToken cancellationToken
        )
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Fail<CartResponse>(SelfNotFound());

            var item = await db.CartItems.FirstOrDefaultAsync(
                ci => ci.ClientId == clientId.Value && ci.ProductId == productId,
                cancellationToken
            );

            if (item is not null)
            {
                db.CartItems.Remove(item);
                await db.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    ApiEvents.CartItemRemoved,
                    "Client {ClientId} removed product {ProductId} from cart",
                    clientId,
                    productId
                );
            }

            return Result.Ok(await BuildCartResponseAsync(clientId.Value, cancellationToken));
        }

        public async Task<Result> ClearAsync(string userId, CancellationToken cancellationToken)
        {
            var clientId = await GetClientIdAsync(userId, cancellationToken);
            if (clientId is null)
                return Result.Fail(SelfNotFound());

            await db.CartItems.Where(ci => ci.ClientId == clientId.Value).ExecuteDeleteAsync(cancellationToken);

            logger.LogInformation(ApiEvents.CartCleared, "Client {ClientId} cleared their cart", clientId);

            return Result.Ok();
        }

        private Task<int?> GetClientIdAsync(string userId, CancellationToken cancellationToken) =>
            db.Clients
                .Where(client => client.UserId == userId)
                .Select(client => (int?)client.ID)
                .SingleOrDefaultAsync(cancellationToken);

        private async Task<CartResponse> BuildCartResponseAsync(
            int clientId,
            CancellationToken cancellationToken
        )
        {
            var rows = await (
                from cartItem in db.CartItems.AsNoTracking()
                join product in db.Products.AsNoTracking() on cartItem.ProductId equals product.Id
                where cartItem.ClientId == clientId
                orderby cartItem.Id
                select new
                {
                    product.Id,
                    product.Name,
                    product.ImageUrl,
                    product.Price,
                    product.Stock,
                    cartItem.Quantity,
                    product.IsActive,
                }
            ).ToListAsync(cancellationToken);

            var items = rows
                .Select(row => new CartItemResponse(
                    row.Id,
                    row.Name,
                    row.ImageUrl,
                    row.Price,
                    row.Stock,
                    row.Quantity,
                    row.Price * row.Quantity,
                    row.IsActive
                ))
                .ToList();

            var subtotal = items.Sum(i => i.LineTotal);
            var shippingCost = ShippingPolicy.CostFor(subtotal);

            return new CartResponse(items, subtotal, shippingCost, subtotal + shippingCost);
        }

        private Error SelfNotFound()
        {
            logger.LogWarning(ApiEvents.CartRejected, "No client is linked to the authenticated user");
            return Error.NotFound(
                "cart.client_not_found",
                "No se encontró un cliente asociado a esta cuenta."
            );
        }

        private Error ProductNotFound(int productId)
        {
            logger.LogWarning(ApiEvents.CartRejected, "Product {ProductId} does not exist", productId);
            return Error.Validation("cart.product_not_found", $"No existe el producto {productId}.");
        }

        private Error ProductUnavailable(int productId)
        {
            logger.LogWarning(ApiEvents.CartRejected, "Product {ProductId} is archived", productId);
            return Error.Conflict("cart.product_unavailable", "Este producto ya no está disponible.");
        }

        private Error ItemNotFound(int productId)
        {
            logger.LogWarning(ApiEvents.CartRejected, "Cart item for product {ProductId} not found", productId);
            return Error.NotFound("cart.item_not_found", $"El producto {productId} no está en el carrito.");
        }

        private Error InsufficientStock(int productId, int stock)
        {
            logger.LogWarning(
                ApiEvents.CartRejected,
                "Insufficient stock for product {ProductId} ({Stock} available)",
                productId,
                stock
            );
            return Error.Conflict(
                "cart.insufficient_stock",
                $"Stock insuficiente para el producto {productId}: quedan {stock}."
            );
        }
    }
}
