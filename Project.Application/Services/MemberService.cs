using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class MemberService : IMemberService
{
    private readonly IMemberRepository _repository;
    private readonly IValidator<CreateMemberRequest> _createValidator;
    private readonly IValidator<UpdateMemberRequest> _updateValidator;

    public MemberService(IMemberRepository repository, IValidator<CreateMemberRequest> createValidator, IValidator<UpdateMemberRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponse<PagedResult<MemberDto>>> GetAllAsync(long tenantId, MemberFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetAllAsync(tenantId, filters);
        return ApiResponse<PagedResult<MemberDto>>.Ok(PagedResult<MemberDto>.Create(items.Select(Map), totalCount, filters));
    }

    public async Task<ApiResponse<MemberDto>> GetByIdAsync(long tenantId, long memberId)
    {
        var entity = await _repository.GetByIdAsync(tenantId, memberId);
        return entity is null ? ApiResponse<MemberDto>.NotFound("Member not found.") : ApiResponse<MemberDto>.Ok(Map(entity));
    }

    public async Task<ApiResponse<IEnumerable<MemberLookupDto>>> LookupAsync(long tenantId, string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ApiResponse<IEnumerable<MemberLookupDto>>.Ok(Array.Empty<MemberLookupDto>());

        var items = await _repository.LookupAsync(tenantId, query.Trim(), limit);
        return ApiResponse<IEnumerable<MemberLookupDto>>.Ok(items.Select(MapLookup));
    }

    public async Task<ApiResponse<MemberDto>> CreateAsync(CreateMemberRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MemberDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.MemberNoExistsAsync(request.TenantId, request.MemberNo.Trim()))
            return ApiResponse<MemberDto>.Fail($"Member number '{request.MemberNo}' is already in use.");

        if (await _repository.EmployeeCodeExistsAsync(request.TenantId, request.EmployeeCode?.Trim()))
            return ApiResponse<MemberDto>.Fail($"Employee code '{request.EmployeeCode}' is already in use.");

        var entity = new Member
        {
            TenantId = request.TenantId,
            MemberNo = request.MemberNo.Trim(),
            EmployeeCode = request.EmployeeCode?.Trim(),
            FullName = request.FullName.Trim(),
            IdentityNo = request.IdentityNo?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            AddressLine = request.AddressLine?.Trim(),
            JoinDate = request.JoinDate,
            MemberStatus = request.MemberStatus.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        entity.MemberId = await _repository.CreateAsync(entity);
        return ApiResponse<MemberDto>.Created(Map(entity));
    }

    public async Task<ApiResponse<MemberDto>> UpdateAsync(long tenantId, long memberId, UpdateMemberRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MemberDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var entity = await _repository.GetByIdAsync(tenantId, memberId);
        if (entity is null)
            return ApiResponse<MemberDto>.NotFound("Member not found.");

        if (await _repository.EmployeeCodeExistsAsync(tenantId, request.EmployeeCode?.Trim(), memberId))
            return ApiResponse<MemberDto>.Fail($"Employee code '{request.EmployeeCode}' is already in use.");

        entity.EmployeeCode = request.EmployeeCode?.Trim();
        entity.FullName = request.FullName.Trim();
        entity.IdentityNo = request.IdentityNo?.Trim();
        entity.PhoneNumber = request.PhoneNumber?.Trim();
        entity.Email = request.Email?.Trim().ToLowerInvariant();
        entity.AddressLine = request.AddressLine?.Trim();
        entity.JoinDate = request.JoinDate;
        entity.MemberStatus = request.MemberStatus.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);
        return ApiResponse<MemberDto>.Ok(Map(entity), "Updated successfully.");
    }

    private static MemberDto Map(Member member) => new(member.MemberId, member.TenantId, member.MemberNo, member.EmployeeCode, member.FullName, member.IdentityNo, member.PhoneNumber, member.Email, member.AddressLine, member.JoinDate, member.MemberStatus, member.Notes);
    private static MemberLookupDto MapLookup(Member member) => new(member.MemberId, member.MemberNo, member.EmployeeCode, member.FullName, member.MemberStatus, member.PhoneNumber);
}
