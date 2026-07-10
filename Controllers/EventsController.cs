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
    public async Task<ActionResult<EventPagedResult<EventListItem>>> Get(
        [FromQuery] EventListQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEventsQuery(
            queryParameters.Page,
            queryParameters.PageSize,
            queryParameters.Search,
            queryParameters.CategoryId,
            queryParameters.FromDate,
            queryParameters.ToDate,
            queryParameters.IsActive);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<EventDetailsResponse>> GetById([FromRoute] ulong id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EventDetailsResponse>> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateEventCommand(
            request.CategoryId,
            request.Name,
            request.Description,
            request.Location,
            request.StartAt,
            request.EndAt,
            request.RegistrationDeadline,
            request.Capacity,
            request.IsActive);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<EventDetailsResponse>> Update(
        [FromRoute] ulong id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateEventCommand(
            id,
            request.CategoryId,
            request.Name,
            request.Description,
            request.Location,
            request.StartAt,
            request.EndAt,
            request.RegistrationDeadline,
            request.Capacity,
            request.IsActive);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:long}/active")]
    public async Task<ActionResult<EventDetailsResponse>> SetActiveState(
        [FromRoute] ulong id,
        [FromBody] SetEventActiveStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new SetEventActiveStateCommand(id, request.IsActive), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] ulong id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteEventCommand(id), cancellationToken);
        return NoContent();
    }
}
