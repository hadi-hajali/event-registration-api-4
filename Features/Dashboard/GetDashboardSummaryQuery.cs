using MediatR;

namespace EventRegistration.Api.Features.Dashboard;

public sealed record GetDashboardSummaryQuery()
    : IRequest<DashboardSummaryResponse>;