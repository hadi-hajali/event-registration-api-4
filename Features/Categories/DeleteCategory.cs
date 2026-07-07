using Dapper;
using EventRegistration.Api.Exceptions;
using EventRegistration.Api.Interfaces;
using MediatR;

namespace EventRegistration.Api.Features.Categories;

public sealed record DeleteCategoryCommand(long Id) : IRequest;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IEventRegistrationDatabase _database;

    public DeleteCategoryCommandHandler(IEventRegistrationDatabase database)
    {
        _database = database;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        await using var connection = await _database.CreateConnectionAsync();

        const string categorySql = @"
            SELECT Id
            FROM Categories
            WHERE Id = @Id";

        var categoryId = await connection.ExecuteScalarAsync<long?>(categorySql, new { Id = request.Id });
        if (!categoryId.HasValue)
        {
            throw new NotFoundException("Category not found.");
        }

        const string eventReferenceSql = @"
            SELECT Id
            FROM Events
            WHERE CategoryId = @Id
            LIMIT 1";

        var eventId = await connection.ExecuteScalarAsync<long?>(eventReferenceSql, new { Id = request.Id });
        if (eventId.HasValue)
        {
            throw new ConflictException("Category is in use by one or more events.");
        }

        const string deleteSql = @"
            DELETE FROM Categories
            WHERE Id = @Id";

        await connection.ExecuteAsync(deleteSql, new { Id = request.Id });
    }
}
