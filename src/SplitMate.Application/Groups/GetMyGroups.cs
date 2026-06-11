using MediatR;
using SplitMate.Application.Common;
using SplitMate.Application.Common.Interfaces;

namespace SplitMate.Application.Groups;

public record GroupSummaryDto(Guid Id, string Name, int MemberCount, DateTime CreatedAtUtc);

public record GetMyGroupsQuery(string CurrentUserId) : IRequest<Result<IReadOnlyList<GroupSummaryDto>>>;

public class GetMyGroupsQueryHandler(IGroupRepository groups)
    : IRequestHandler<GetMyGroupsQuery, Result<IReadOnlyList<GroupSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<GroupSummaryDto>>> Handle(
        GetMyGroupsQuery request, CancellationToken cancellationToken)
    {
        var myGroups = await groups.GetGroupsForUserAsync(request.CurrentUserId, cancellationToken);
        IReadOnlyList<GroupSummaryDto> dtos = myGroups
            .Select(g => new GroupSummaryDto(g.Id, g.Name, g.Members.Count, g.CreatedAtUtc))
            .ToList();
        return Result<IReadOnlyList<GroupSummaryDto>>.Success(dtos);
    }
}
