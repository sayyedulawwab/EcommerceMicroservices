using SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Users;
public static class RefreshTokenErrors
{
    public static readonly Error Expired = Error.BadRequest(
        "RefreshTokne.Expired",
        "THe refresh token is expired");
}