using EventRegistration.Api.Features.Events;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventRegistration.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedEventResponse>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] ulong? categoryId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEventsQuery(page, pageSize, search, categoryId, isActive),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<EventResponse>> GetById([FromRoute] ulong id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create([FromBody] EventRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new CreateEventCommand(
                request.Name,
                request.Description,
                request.Location,
                request.CategoryId,
                request.Capacity,
                request.StartAt,
                request.EndAt,
                request.RegistrationDeadline,
                request.IsActive),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<EventResponse>> Update([FromRoute] ulong id, [FromBody] EventRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new UpdateEventCommand(
                id,
                request.Name,
                request.Description,
                request.Location,
                request.CategoryId,
                request.Capacity,
                request.StartAt,
                request.EndAt,
                request.RegistrationDeadline,
                request.IsActive),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] ulong id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteEventCommand(id), cancellationToken);
        return NoContent();
    }
}
