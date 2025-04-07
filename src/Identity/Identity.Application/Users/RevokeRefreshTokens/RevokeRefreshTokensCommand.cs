using Identity.Application.Abstractions.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Users.RevokeRefreshTokens;
public record RevokeRefreshTokensCommand(long UserId) : ICommand<long>;