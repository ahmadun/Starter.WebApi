using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record MemberDto(
    long MemberId,
    long TenantId,
    string MemberNo,
    string? EmployeeCode,
    string FullName,
    string? IdentityNo,
    string? PhoneNumber,
    string? Email,
    string? AddressLine,
    DateOnly JoinDate,
    string MemberStatus,
    string? Notes);

public sealed record MemberLookupDto(
    long MemberId,
    string MemberNo,
    string? EmployeeCode,
    string FullName,
    string MemberStatus,
    string? PhoneNumber);

public sealed record CreateMemberRequest(
    long TenantId,
    string MemberNo,
    string? EmployeeCode,
    string FullName,
    string? IdentityNo,
    string? PhoneNumber,
    string? Email,
    string? AddressLine,
    DateOnly JoinDate,
    string MemberStatus,
    string? Notes);

public sealed record UpdateMemberRequest(
    string? EmployeeCode,
    string FullName,
    string? IdentityNo,
    string? PhoneNumber,
    string? Email,
    string? AddressLine,
    DateOnly JoinDate,
    string MemberStatus,
    string? Notes);

public sealed class MemberFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public string? MemberStatus { get; set; }
}

public sealed class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.MemberNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MemberStatus).NotEmpty();
    }
}

public sealed class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MemberStatus).NotEmpty();
    }
}
