using EventRegistration.Api.Features.Registrations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventRegistration.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:long}/registrations")]
public sealed class RegistrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RegistrationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedRegistrationResponse>> Get([FromRoute] ulong eventId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] int? status = null, [FromQuery] ulong? participantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRegistrationsQuery(eventId, page, pageSize, search, status, participantId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RegistrationResponse>> Create([FromRoute] ulong eventId, [FromBody] CreateRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateRegistrationCommand(eventId, request.ParticipantId, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/events/{eventId}/registrations/{result.Id}", result);
    }

    [HttpPatch("{registrationId:long}/cancel")]
    public async Task<ActionResult<RegistrationResponse>> Cancel([FromRoute] ulong eventId, [FromRoute] ulong registrationId, CancellationToken cancellationToken = default)
    {
        var command = new CancelRegistrationCommand(eventId, registrationId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
