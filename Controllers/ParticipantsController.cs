using MediatR;
using Microsoft.AspNetCore.Mvc;
using EventRegistration.Api.Features.Participants;

namespace EventRegistration.Api.Controllers;

[ApiController]
[Route("api/participants")]
public sealed class ParticipantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ParticipantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetParticipants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetParticipants.Query(page, pageSize, search, isActive),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetParticipantById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetParticipantById.Query(id),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateParticipant(
        [FromBody] CreateParticipant.Command command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetParticipantById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateParticipant(
        long id,
        [FromBody] UpdateParticipant.Request request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateParticipant.Command(
                id,
                request.FullName,
                request.Email,
                request.Phone,
                request.DateOfBirth,
                request.IsActive),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteParticipant(
        long id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteParticipant.Command(id), cancellationToken);
        return NoContent();
    }
}