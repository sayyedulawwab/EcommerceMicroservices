using Identity.Application.UnitTests.TestUtilities;
using Identity.Application.Users.ResendEmailConfirmation;
using Identity.Domain.Users;
using Moq;
using SharedKernel.Domain;

namespace Identity.Application.UnitTests.Users;

public sealed class ResendEmailConfirmationCommandHandlerTests
{
    private readonly TestDependencies _deps = new();

    [Fact]
    public async Task Handle_UnknownEmail_SucceedsAndIssuesNothing()
    {
        Result result = await CreateHandler().Handle(
            new ResendEmailConfirmationCommand("nobody@example.com"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(_deps.AddedUserTokens);
        _deps.UserTokens.Verify(
            r => r.ConsumeAllAsync(It.IsAny<long>(), It.IsAny<UserTokenPurpose>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_SucceedsAndIssuesNothing()
    {
        User user = TestData.CreateUser(emailConfirmed: true);
        _deps.SetupUserByEmail(user);

        Result result = await CreateHandler().Handle(
            new ResendEmailConfirmationCommand(user.Email),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(_deps.AddedUserTokens);
        _deps.VerifyNeverSaved();
    }

    [Fact]
    public async Task Handle_UnconfirmedUser_ConsumesOutstandingTokensThenIssuesNewOne()
    {
        User user = TestData.CreateUser(emailConfirmed: false);
        _deps.SetupUserByEmail(user);

        Result result = await CreateHandler().Handle(
            new ResendEmailConfirmationCommand(user.Email),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _deps.UserTokens.Verify(
            r => r.ConsumeAllAsync(user.Id, UserTokenPurpose.EmailConfirmation, TestData.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);

        UserToken token = Assert.Single(_deps.AddedUserTokens);
        Assert.Equal(UserTokenPurpose.EmailConfirmation, token.Purpose);
        Assert.Equal(user.Id, token.UserId);
        Assert.Equal(TestData.UtcNow.AddHours(24), token.ExpiresOnUtc);

        Assert.Equal(
            [nameof(IUserTokenRepository.ConsumeAllAsync), nameof(IUserTokenRepository.Add), nameof(IUnitOfWork.SaveChangesAsync)],
            _deps.CallOrder);
    }

    private ResendEmailConfirmationCommandHandler CreateHandler() =>
        new(
            _deps.Users.Object,
            _deps.UserTokens.Object,
            _deps.CreateUserTokenIssuer(),
            _deps.UnitOfWork.Object,
            _deps.Clock);
}
