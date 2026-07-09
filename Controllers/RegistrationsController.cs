using EventRegistration.Api.Features.Registrations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventRegistration.Api.Controllers;

[ApiController]
[Route("api/registrations")]
public sealed class RegistrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RegistrationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegistrationResponse>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] int? status = null, [FromQuery] ulong? eventId = null, [FromQuery] ulong? participantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRegistrationsQuery(page, pageSize, search, status, eventId, participantId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<RegistrationResponse>> GetById([FromRoute] ulong id, CancellationToken cancellationToken = default)
    {
        var query = new GetRegistrationByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RegistrationResponse>> Create([FromBody] CreateRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateRegistrationCommand(request.EventId, request.ParticipantId, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}/cancel")]
    public async Task<ActionResult<RegistrationResponse>> Cancel([FromRoute] ulong id, CancellationToken cancellationToken = default)
    {
        var command = new CancelRegistrationCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] ulong id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteRegistrationCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
