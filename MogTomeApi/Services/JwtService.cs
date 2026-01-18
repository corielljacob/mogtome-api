using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace MogTomeApi.Services
{
    public class JwtService
    {
        private readonly MongoService _mongoService;
        private readonly IConfiguration _config;

        public JwtService(MongoService mongoService, IConfiguration config)
        {
            _mongoService = mongoService;
            _config = config;
        }

        public static RefreshToken CreateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                Revoked = false
            };

            return refreshToken;
        }

        public async Task<string> CreateAccessToken(string discordUserId)
        {
            var freeCompanyMember = await _mongoService.GetFreeCompanyMemberByDiscordId(discordUserId);

            var claims = new Dictionary<string, object>
            {
                ["discordId"] = discordUserId,
                ["memberName"] = freeCompanyMember.Name,
                ["memberRank"] = freeCompanyMember.FreeCompanyRank,
                ["hasKnighthood"] = freeCompanyMember.FreeCompanyRank == Constants.MoogleKnight || freeCompanyMember.FreeCompanyRank == Constants.MoogleGuardian,
                ["hasTemporaryKnighthood"] = freeCompanyMember.HasTemporaryKnighthood,
                ["memberPortraitUrl"] = freeCompanyMember.AvatarLink
            };

            var secretKey = Environment.GetEnvironmentVariable("MogTomeApiSigningSecret", EnvironmentVariableTarget.Process);
            var signingkey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _config["Authentication:Host"],
                Audience = _config["Authentication:Audience"],
                Claims = claims,
                IssuedAt = null,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(signingkey, SecurityAlgorithms.HmacSha256Signature)
            };

            var jwtHandler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };

            if(freeCompanyMember.FirstMogTomeLogin == null)
            {
                var loginDate = DateTime.UtcNow;
                await _mongoService.SetFirstTimeMogTomeLogin(discordUserId, loginDate);
                descriptor.Claims.Add("firstMogTomeLoginDate", loginDate);
            }
            else
            {
                descriptor.Claims.Add("firstMogTomeLoginDate", freeCompanyMember.FirstMogTomeLogin);
            }

            var tokenString = jwtHandler.CreateToken(descriptor);
            return tokenString;
        }

        public class RefreshToken
        {
            public string Token { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool Revoked { get; set; }
        }
    }
}
