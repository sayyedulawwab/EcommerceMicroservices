using SharedKernel.Domain;

namespace Identity.Domain.Users;

public sealed record UserTokenIssuedDomainEvent(
    string Email,
    string FirstName,
    UserTokenPurpose Purpose,
    string RawToken) : IDomainEvent;
