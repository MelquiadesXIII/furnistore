using API.Furnistore.Application.Clients;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.Application.Orders
{
    public static class OrderMapping
    {
        public static string Label(OrderStatus status) =>
            status switch
            {
                OrderStatus.Paid => "pagado",
                OrderStatus.Processing => "en preparación",
                OrderStatus.Shipped => "enviado",
                OrderStatus.Delivered => "entregado",
                OrderStatus.Cancelled => "cancelado",
                _ => status.ToString(),
            };

        public static async Task<Dictionary<int, string?>> LoadImagesAsync(
            APIFurnistoreContext db,
            IEnumerable<Order> orders,
            CancellationToken cancellationToken
        )
        {
            var productIds = orders
                .SelectMany(order => order.OrderDetails)
                .Select(detail => detail.ProductId)
                .Distinct()
                .ToList();

            return await db
                .Products.Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.ImageUrl, cancellationToken);
        }

        public static OrderResponse ToResponse(Order order, IReadOnlyDictionary<int, string?> images) =>
            new(
                order.Id,
                order.OrderNumber,
                order.Status,
                OrderTransitions.CustomerCancel.From.Contains(order.Status),
                order.PlacedAt,
                order.PaidAt,
                order.EstimatedDeliveryDate,
                order.ProcessingAt,
                order.ShippedAt,
                order.DeliveredAt,
                order.CancelledAt,
                order.CancelReason,
                order.Subtotal,
                order.ShippingCost,
                order.Total,
                new OrderShipToResponse(
                    order.ShipToName,
                    order.ShipToPhone,
                    new ShippingAddress
                    {
                        Street = order.ShipToStreet,
                        City = order.ShipToCity,
                        Province = order.ShipToProvince,
                        DeliveryNotes = order.ShipToDeliveryNotes,
                    }
                ),
                order
                    .OrderDetails.OrderBy(d => d.ProductName)
                    .ThenBy(d => d.ProductId)
                    .Select(d => new OrderLineResponse(
                        d.ProductId,
                        d.ProductName,
                        images.GetValueOrDefault(d.ProductId),
                        d.Quantity,
                        d.UnitPrice,
                        d.UnitPrice * d.Quantity
                    ))
                    .ToList()
            );
    }
}
