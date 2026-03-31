using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MogTomeApi.Features.Authentication.Data;
using MogTomeApi.Shared;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MogTomeApi.Features.Authentication
{
    public class JwtHelper
    {
        public static string CreateAccessToken(
            FreeCompanyMember freeCompanyMember,
            string tokenIssuer,
            string tokenAudience)
        {
            var claims = new List<Claim>
            {
                new ("discordId", freeCompanyMember.DiscordId),
                new ("memberName", freeCompanyMember.Name),
                new ("memberRank", freeCompanyMember.FreeCompanyRank),
                new ("memberPortraitUrl", freeCompanyMember.AvatarLink),
                new ("firstMogTomeLoginDate", freeCompanyMember.FirstMogTomeLogin.ToString()),
                new ("hasKnighthood", (freeCompanyMember.FreeCompanyRank == Constants.MoogleKnight || freeCompanyMember.FreeCompanyRank == Constants.MoogleGuardian) ? "true" : "false")
            };

            if(freeCompanyMember.FreeCompanyRank == Constants.MoogleKnight || freeCompanyMember.FreeCompanyRank == Constants.MoogleGuardian || freeCompanyMember.HasTemporaryKnighthood)
                claims.Add(new Claim(ClaimTypes.Role, Constants.MoogleKnight));

            if(freeCompanyMember.FreeCompanyRank == Constants.PaissaTrainer)
                claims.Add(new Claim(ClaimTypes.Role, Constants.PaissaTrainer));
            
            var secretKey = Environment.GetEnvironmentVariable("MogTomeApiSigningSecret", EnvironmentVariableTarget.Process);
            var signingkey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = tokenIssuer,
                Audience = tokenAudience,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(signingkey, SecurityAlgorithms.HmacSha256Signature)
            };

            var jwtHandler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
            var tokenString = jwtHandler.CreateToken(descriptor);
            return tokenString;
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

        public class RefreshToken
        {
            public string Token { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool Revoked { get; set; }
        }
    }
}
