using API.Furnistore.API.Extensions;
using API.Furnistore.Application.Admin.Reports;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers.Admin
{
    [Route("api/admin/reports")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public sealed class AdminReportsController(AdminReportService reports) : AdminControllerBase
    {
        [HttpGet("sales")]
        [ProducesResponseType<SalesReport>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Sales([FromQuery] ReportQuery query, CancellationToken cancellationToken) =>
            (await reports.SalesAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("products")]
        [ProducesResponseType<ProductsReport>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Products([FromQuery] ReportQuery query, CancellationToken cancellationToken) =>
            (await reports.ProductsAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("operations")]
        [ProducesResponseType<OperationsReport>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Operations([FromQuery] ReportQuery query, CancellationToken cancellationToken) =>
            (await reports.OperationsAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("cancellations")]
        [ProducesResponseType<CancellationsReport>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Cancellations(
            [FromQuery] ReportQuery query,
            CancellationToken cancellationToken
        ) => (await reports.CancellationsAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("inventory")]
        [ProducesResponseType<InventoryReport>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Inventory([FromQuery] ReportQuery query, CancellationToken cancellationToken) =>
            (await reports.InventoryAsync(query, cancellationToken)).ToActionResult(this);

        [HttpGet("customers")]
        [ProducesResponseType<CustomersReport>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Customers([FromQuery] ReportQuery query, CancellationToken cancellationToken) =>
            (await reports.CustomersAsync(query, cancellationToken)).ToActionResult(this);

        [HttpPost("exports")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordExport(
            [FromBody] ReportExportRequest request,
            CancellationToken cancellationToken
        ) => (await reports.RecordExportAsync(request, User.UserId(), cancellationToken)).ToNoContentResult(this);
    }
}
