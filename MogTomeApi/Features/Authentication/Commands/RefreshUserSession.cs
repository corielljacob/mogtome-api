using MediatR;
using MogTomeApi.Features.Authentication.Data;
using MogTomeApi.Shared;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Authentication.Commands
{
    public class RefreshUserSession
    {
        public class Command : IRequest<Result<RefreshResult>>
        {
            public string SessionId { get; set; }
        }

        public class RefreshResult
        {
            public string Jwt { get; set; }
            public string SessionId { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<RefreshResult>>
        {
            private readonly IConfiguration _config;
            private readonly IMongoDatabase _mongoDatabase;
            private readonly ILogger<Handler> _logger;

            public Handler(IConfiguration config, IMongoDatabase mongoDatabase, ILogger<Handler> logger)
            {
                _config = config;
                _mongoDatabase = mongoDatabase;
                _logger = logger;
            }

            public async Task<Result<RefreshResult>> Handle(Command request, CancellationToken cancellationToken)
            {
                var tokenCollection = _mongoDatabase.GetCollection<MemberToken>("tokens");
                var filter = Builders<MemberToken>.Filter.Eq(token => token.SessionId, request.SessionId);

                var memberToken = await tokenCollection
                    .Find(filter)
                    .SingleOrDefaultAsync(cancellationToken);

                if (memberToken == null || memberToken.RefreshTokenRevoked || memberToken.RefreshTokenExpiresAt <= DateTime.Now)
                {
                    _logger.LogWarning("Invalid or expired refresh token for session {SessionId}", request.SessionId);
                    await RevokeSession(request.SessionId, cancellationToken);
                    return new Result<RefreshResult>(null, HttpStatusCode.Unauthorized, false, "Invalid or expired session.");
                }

                var freeCompanyMember = await GetFreeCompanyMemberByDiscordId(memberToken.DiscordId);

                var jwtTokenIssuer = _config["Authentication:Host"];
                var jwtTokenAudience = _config["Authentication:Audience"];
                var newJwt = JwtHelper.CreateAccessToken(freeCompanyMember, jwtTokenIssuer, jwtTokenAudience);
                var newRefreshToken = JwtHelper.CreateRefreshToken();
                var newSessionId = SessionHelper.GenerateSessionId();

                await UpsertMemberToken(memberToken.DiscordId, newRefreshToken, newSessionId, cancellationToken);

                _logger.LogWarning("Refreshed session {SessionId} with new id {newSessionId}", request.SessionId, newSessionId);
                await RevokeSession(request.SessionId, cancellationToken);

                var refreshResult = new RefreshResult
                {
                    Jwt = newJwt,
                    SessionId = newSessionId
                };

                return new Result<RefreshResult>(refreshResult, HttpStatusCode.OK, true, null);
            }

            private async Task<FreeCompanyMember> GetFreeCompanyMemberByDiscordId(string discordId)
            {
                var memberCollection = _mongoDatabase.GetCollection<FreeCompanyMember>("members");

                var filter = Builders<FreeCompanyMember>.Filter.And(
                    Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId),
                    Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true)
                );

                var member = await memberCollection.Find(m => m.DiscordId == discordId).FirstOrDefaultAsync();
                return member;
            }

            private async Task UpsertMemberToken(string discordId, JwtHelper.RefreshToken refreshToken, string sessionId, CancellationToken cancellationToken)
            {
                var memberToken = new MemberToken
                {
                    DiscordId = discordId,
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                    RefreshTokenRevoked = refreshToken.Revoked,
                    SessionId = sessionId
                };

                var tokenCollection = _mongoDatabase.GetCollection<MemberToken>("tokens");
                var filter = Builders<MemberToken>.Filter.And(
                    Builders<MemberToken>.Filter.Eq(token => token.DiscordId, discordId),
                    Builders<MemberToken>.Filter.Eq(token => token.SessionId, sessionId)
                );

                await tokenCollection.ReplaceOneAsync(filter, memberToken, new ReplaceOptions { IsUpsert = true }, cancellationToken);
            }

            private async Task RevokeSession(string sessionId, CancellationToken cancellationToken)
            {
                _logger.LogWarning("Deleting session id {sessionId}", sessionId);
                var tokenCollection = _mongoDatabase.GetCollection<MemberToken>("tokens");
                var filter = Builders<MemberToken>.Filter.Eq(token => token.SessionId, sessionId);
                await tokenCollection.DeleteOneAsync(filter, cancellationToken);
            }
        }
    }
}
