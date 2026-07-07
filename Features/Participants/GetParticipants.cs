using Dapper;
using MediatR;
using EventRegistration.Api.Interfaces;

namespace EventRegistration.Api.Features.Participants;

public static class GetParticipants
{
    public sealed record Query(
        int Page = 1,
        int PageSize = 10,
        string? Search = null,
        bool? IsActive = null
    ) : IRequest<PagedResult<ParticipantDto>>;

    public sealed class Handler : IRequestHandler<Query, PagedResult<ParticipantDto>>
    {
        private readonly IEventRegistrationDatabase _database;

        public Handler(IEventRegistrationDatabase database)
        {
            _database = database;
        }

        public async Task<PagedResult<ParticipantDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var offset = (request.Page - 1) * request.PageSize;

            var search = string.IsNullOrWhiteSpace(request.Search)
                ? null
                : $"%{request.Search.Trim()}%";

            const string countSql = """
                SELECT COUNT(*)
                FROM Participants
                WHERE
                    (@Search IS NULL OR FullName LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search)
                    AND (@IsActive IS NULL OR IsActive = @IsActive);
                """;

            const string dataSql = """
                SELECT
                    Id,
                    FullName,
                    Email,
                    Phone,
                    DateOfBirth,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM Participants
                WHERE
                    (@Search IS NULL OR FullName LIKE @Search OR Email LIKE @Search OR Phone LIKE @Search)
                    AND (@IsActive IS NULL OR IsActive = @IsActive)
                ORDER BY FullName
                LIMIT @PageSize OFFSET @Offset;
                """;

            using var connection = _database.Open();

            var parameters = new
            {
                Search = search,
                request.IsActive,
                request.PageSize,
                Offset = offset
            };

            var totalCount = await connection.ExecuteScalarAsync<long>(countSql, parameters);
            var items = (await connection.QueryAsync<ParticipantDto>(dataSql, parameters)).ToList();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new PagedResult<ParticipantDto>(
                items,
                request.Page,
                request.PageSize,
                totalCount,
                totalPages);
        }
    }
}