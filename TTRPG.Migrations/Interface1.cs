public interface IMemberRepository
{
    Task<int> CreateAsync(Member member, CancellationToken cancellationToken = default);
    Task<Member?> GetByIdAsync(int memberId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Member>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Member member, CancellationToken cancellationToken = default);
    Task DeleteAsync(int memberId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Member>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string sortBy,
        string sortDirection,
        string? filterValue,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<MemberSessionDto>> GetMembersWithSessionsAsync(
        CancellationToken cancellationToken = default);
}

