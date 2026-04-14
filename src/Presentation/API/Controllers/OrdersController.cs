using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReliableEvents.Sample.API.Contracts;
using ReliableEvents.Sample.Application.Orders;

namespace ReliableEvents.Sample.API.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var orderId = await mediator.Send(new CreateOrderCommand(request.CustomerEmail, request.TotalAmount), cancellationToken);
        return Accepted(new { orderId });
    }
}
