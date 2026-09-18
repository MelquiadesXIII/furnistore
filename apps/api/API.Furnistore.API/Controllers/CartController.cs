using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/cart")]
    public sealed class CartController(CartService cart) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
            (await cart.GetAsync(User.UserId(), cancellationToken)).ToActionResult(this);

        [HttpPost("items")]
        [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItem(
            AddCartItemRequest request,
            CancellationToken cancellationToken
        ) => (await cart.AddItemAsync(User.UserId(), request, cancellationToken)).ToActionResult(this);

        [HttpPut("items/{productId:int}")]
        [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetItemQuantity(
            int productId,
            UpdateCartItemRequest request,
            CancellationToken cancellationToken
        ) =>
            (await cart.SetItemQuantityAsync(User.UserId(), productId, request, cancellationToken))
                .ToActionResult(this);

        [HttpDelete("items/{productId:int}")]
        [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveItem(int productId, CancellationToken cancellationToken) =>
            (await cart.RemoveItemAsync(User.UserId(), productId, cancellationToken)).ToActionResult(this);

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Clear(CancellationToken cancellationToken) =>
            (await cart.ClearAsync(User.UserId(), cancellationToken)).ToNoContentResult(this);
    }
}
