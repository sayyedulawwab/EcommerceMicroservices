﻿using SharedKernel.Domain;

namespace Identity.Application.Abstractions.Auth;
public interface IJwtService
{
    Result<string> GetAccessToken(string email, long userId, CancellationToken cancellationToken = default);
}
