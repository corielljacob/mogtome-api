using F23.StringSimilarity;
using MogTomeApi.Controllers;
using MogTomeApi.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using static MogTomeApi.Controllers.EventsController;
using static MogTomeApi.Services.JwtService;

namespace MogTomeApi.Services
{
    public class MongoService
    {
        private readonly MongoClient _client;
        private readonly ILogger _logger;

        public MongoService(ILogger<MongoService> logger)
        {
            var connectionString = Environment.GetEnvironmentVariable(Constants.ConnectionStringId, EnvironmentVariableTarget.Machine);

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = Environment.GetEnvironmentVariable(Constants.ConnectionStringId);

            _client = new MongoClient(connectionString);
            _logger = logger;
        }

        public async Task<List<FreeCompanyMember>> GetFreeCompanyMembers()
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var filter = Builders<FreeCompanyMember>.Filter.Empty;
            var freeCompanyMembers = await memberCollection
                .Find(filter)
                .ToListAsync();

            var activeMembers = freeCompanyMembers.Where(member => member.ActiveMember).ToList();
            return activeMembers;
        }

        public async Task<List<FreeCompanyStaffMember>> GetFreeCompanyStaff()
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyStaffMember>("members");

            var filter = Builders<FreeCompanyStaffMember>.Filter.And(
                Builders<FreeCompanyStaffMember>.Filter.Or(
                    Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.MoogleKnight),
                    Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.PaissaTrainer),
                    Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.FreeCompanyRank, Constants.MoogleGuardian)
                ),
                Builders<FreeCompanyStaffMember>.Filter.Eq(member => member.ActiveMember, true),
                Builders<FreeCompanyStaffMember>.Filter.Ne(member => member.Name, Constants.PassiveToast)
            );

            var freeCompanyMembers = await memberCollection
                .Find(filter)
                .ToListAsync();

            var activeMembers = freeCompanyMembers.Where(member => member.ActiveMember).ToList();
            return activeMembers;
        }

        public async Task<FreeCompanyMember> GetFreeCompanyMemberByDiscordId(string discordId)
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var filter = Builders<FreeCompanyMember>.Filter.And(
                Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId), 
                Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true)
            );

            var freeCompanyMember = await memberCollection
                .Find(filter)
                .SingleAsync();

            return freeCompanyMember;
        }

        public async Task<PaginatedEventsResponse> GetFreeCompanyEvents(string cursor, int limit, string eventTextQuery, string eventTypeFilter)
        {
            var eventsCollection = _client.GetDatabase("kupo-life").GetCollection<Event>("events");

            var filters = BuildFilters(cursor, eventTypeFilter);
            
            var eventsSearch = BuildTextSearch(eventsCollection, eventTextQuery);
            var events = await eventsSearch
                .Match(filters)
                .SortByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id)
                .Limit(limit + 1)
                .ToListAsync();

            var nextCursor = CalculateNextCursor(events, limit);

            var hasMore = events.Count > limit;
            events = events.Take(limit).ToList();

            var paginatedResponse = new PaginatedEventsResponse
            {
                Events = events,
                NextCursor = nextCursor,
                HasMore = hasMore
            };

            return paginatedResponse;
        }

        private FilterDefinition<Event> BuildFilters(string cursor, string eventTypeFilter)
        {
            var filters = Builders<Event>.Filter.Empty;

            var decodedCursor = CursorHelper.DecodeCursor(cursor);
            if (decodedCursor is not null)
            {
                var createdAtFilter = decodedCursor.CreatedAt;
                var idFilter = ObjectId.Parse(decodedCursor.Id);

                filters = Builders<Event>.Filter.Or(
                    Builders<Event>.Filter.Lt(e => e.CreatedAt, createdAtFilter),

                    Builders<Event>.Filter.And(
                        Builders<Event>.Filter.Eq(e => e.CreatedAt, createdAtFilter),
                        Builders<Event>.Filter.Lt(e => e.Id, idFilter)
                    )
                );
            }

            if (string.IsNullOrEmpty(eventTypeFilter) == false)
            {
                var typeFilter = Builders<Event>.Filter.Eq(e => e.Type, eventTypeFilter);
                filters = Builders<Event>.Filter.And(filters, typeFilter);
            }

            return filters;
        }

        private string CalculateNextCursor(List<Event> events, int limit)
        {
            bool hasMore = events.Count > limit;

            string nextCursor = null;
            if (hasMore)
            {
                var lastEvent = events[^2];
                nextCursor = CursorHelper.EncodeCursor(new CursorHelper.EventCursor(lastEvent.CreatedAt, lastEvent.Id.ToString()));
            }

            return nextCursor;
        }

        private IAggregateFluent<Event> BuildTextSearch(IMongoCollection<Event> eventsCollection, string eventTextQuery)
        {
            var eventsSearch = eventsCollection.Aggregate();

            if (string.IsNullOrEmpty(eventTextQuery) == false)
            {
                eventsSearch = eventsSearch.Search(Builders<Event>.Search.Autocomplete(g => g.Text, eventTextQuery), indexName: "event-index");
            }

            return eventsSearch;
        }

        public async Task<MemberToken> GetMemberRefreshTokenInfo(string refreshToken)
        {
            var tokenCollection = _client.GetDatabase("kupo-life").GetCollection<MemberToken>("tokens");

            var filter = Builders<MemberToken>.Filter.Eq(member => member.CustomRefreshToken, refreshToken);

            var memberToken = await tokenCollection
                .Find(filter)
                .SingleAsync();

            return memberToken;
        }

        public async Task<MemberToken> GetMemberTokenByDiscordId(string discordId)
        {
            var tokenCollection = _client.GetDatabase("kupo-life").GetCollection<MemberToken>("tokens");

            var filter = Builders<MemberToken>.Filter.Eq(member => member.DiscordId, discordId);

            var memberToken = await tokenCollection
                .Find(filter)
                .SingleOrDefaultAsync();

            return memberToken;
        }

        public async Task UpsertMemberToken(string discordId, RefreshToken refreshToken, DiscordToken discordToken)
        {
            var memberToken = await GetMemberTokenByDiscordId(discordId);

            if (memberToken == null)
            {
                memberToken = new MemberToken();
            }

            memberToken.DiscordId = discordId;
            memberToken.CustomRefreshToken = refreshToken.Token;
            memberToken.CustomRefreshTokenRevoked = refreshToken.Revoked;
            memberToken.CustomRefreshTokenExpiresAt = refreshToken.ExpiresAt;
            memberToken.DiscordToken = discordToken.AccessToken;
            memberToken.DiscordRefreshToken = discordToken.RefreshToken;

            var tokenCollection = _client.GetDatabase("kupo-life").GetCollection<MemberToken>("tokens");
            var filter = Builders<MemberToken>.Filter.Eq(member => member.DiscordId, memberToken.DiscordId);
            await tokenCollection.ReplaceOneAsync(filter, memberToken, new ReplaceOptions { IsUpsert = true });
        }

        public async Task UpdateMemberDiscordToken(string discordId, DiscordToken discordToken)
        {
            var memberToken = await GetMemberTokenByDiscordId(discordId);

            memberToken.DiscordToken = discordToken.AccessToken;
            memberToken.DiscordRefreshToken = discordToken.RefreshToken;

            var tokenCollection = _client.GetDatabase("kupo-life").GetCollection<MemberToken>("tokens");
            var filter = Builders<MemberToken>.Filter.Eq(member => member.DiscordId, memberToken.DiscordId);
            await tokenCollection.ReplaceOneAsync(filter, memberToken, new ReplaceOptions { IsUpsert = false });
        }

        public async Task UpdateMemberRefreshToken(string discordId, RefreshToken refreshToken)
        {
            var memberToken = await GetMemberTokenByDiscordId(discordId);

            memberToken.CustomRefreshToken = refreshToken.Token;
            memberToken.CustomRefreshTokenRevoked = refreshToken.Revoked;
            memberToken.CustomRefreshTokenExpiresAt = refreshToken.ExpiresAt;

            var tokenCollection = _client.GetDatabase("kupo-life").GetCollection<MemberToken>("tokens");
            var filter = Builders<MemberToken>.Filter.Eq(member => member.DiscordId, memberToken.DiscordId);
            await tokenCollection.ReplaceOneAsync(filter, memberToken, new ReplaceOptions { IsUpsert = false });
        }

        public async Task SetUserBiography(string discordId, string biography)
        {
            var membersCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var filter = Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId);

            var update = Builders<FreeCompanyMember>.Update
                .Set(member => member.Biography, biography);

            await membersCollection.UpdateOneAsync(filter, update);
        }

        public async Task CreateBiographySubmission(string discordId, string biography)
        {
            var membersCollection = _client.GetDatabase("kupo-life").GetCollection<BiographySubmission>("biography-submissions");

            var submission = new BiographySubmission
            {
                SubmissionId = Guid.NewGuid(),
                SubmittedByDiscordId = discordId,
                Biography = biography,
                SubmittedAt = DateTime.UtcNow,
                Status = "Pending"
            };

            await membersCollection.InsertOneAsync(submission);
        }

        public async Task<List<BiographySubmission>> GetPendingBiographySubmissions()
        {
            var membersCollection = _client.GetDatabase("kupo-life").GetCollection<BiographySubmission>("biography-submissions");
            var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.Status, "Pending");

            var submissions = await membersCollection
                .Find(filter)
                .ToListAsync();
            
            return submissions;
        }

        public async Task<HttpStatusCode> ApproveSubmission(Guid submissionId, string approvedBy)
        {
            try
            {
                var membersCollection = _client.GetDatabase("kupo-life").GetCollection<BiographySubmission>("biography-submissions");
                var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmissionId, submissionId);

                var submission = await membersCollection
                    .Find(filter)
                    .SingleOrDefaultAsync();

                if (submission == null)
                {
                    return HttpStatusCode.NotFound;
                }

                await SetUserBiography(submission.SubmittedByDiscordId, submission.Biography);

                var update = Builders<BiographySubmission>.Update
                    .Set(submission => submission.Status, "Approved")
                    .Set(submission => submission.ApprovedByDiscordId, approvedBy);

                await membersCollection.UpdateOneAsync(filter, update);

                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to approve biography submission {SubmissionId}: {Message}\nStack Trace:{Trace}", submissionId, ex.Message, ex.StackTrace);
                return HttpStatusCode.InternalServerError;
            }
        }

        public async Task<HttpStatusCode> RejectSubmission(Guid submissionId, string rejectedBy)
        {
            try
            {
                var membersCollection = _client.GetDatabase("kupo-life").GetCollection<BiographySubmission>("biography-submissions");
                var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmissionId, submissionId);

                var submission = await membersCollection
                    .Find(filter)
                    .SingleOrDefaultAsync();

                if (submission == null)
                {
                    return HttpStatusCode.NotFound;
                }

                var update = Builders<BiographySubmission>.Update
                    .Set(submission => submission.Status, "Rejected")
                    .Set(submission => submission.RejectedByDiscordId, rejectedBy);

                await membersCollection.UpdateOneAsync(filter, update);

                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to reject biography submission {SubmissionId}: {Message}\nStack Trace:{Trace}", submissionId, ex.Message, ex.StackTrace);
                return HttpStatusCode.InternalServerError;
            }
        }

        public async Task SetFirstTimeMogTomeLogin(string discordId, DateTime loginDate)
        {
            var membersCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var filter = Builders<FreeCompanyMember>.Filter.Eq(member => member.DiscordId, discordId);

            var update = Builders<FreeCompanyMember>.Update
                .Set(member => member.FirstMogTomeLogin, loginDate);

            await membersCollection.UpdateOneAsync(filter, update);
        }

        public async Task<BiographySubmission> GetUserSubmissionInfo(string discordId)
        {
            var submissionsCollection = _client.GetDatabase("kupo-life").GetCollection<BiographySubmission>("biography-submissions");
            var filter = Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmittedByDiscordId, discordId);

            var submissions = await submissionsCollection
                .Find(filter)
                .ToListAsync();

            if(submissions.Any(submission => submission.Status == "Pending") == false && submissions.Any(submission => submission.Status == "Approved") == false)
            {
                return null;
            }

            if(submissions.Any(submission => submission.Status == "Pending") == false)
            {
                var submission = submissions
                    .Where(submission => submission.Status == "Approved")
                    .OrderByDescending(submission => submission.SubmittedAt)
                    .FirstOrDefault();

                return submission;
            }

            var lastPendingSubmission = submissions
                .Where(submission => submission.Status == "Pending")
                .OrderByDescending(submission => submission.SubmittedAt)
                .FirstOrDefault();

            return lastPendingSubmission;
        }

        public async Task<HttpStatusCode> EditSubmission(Guid submissionId, string biography)
        {
            try
            {
                var submissionCollection = _client.GetDatabase("kupo-life").GetCollection<BiographySubmission>("biography-submissions");
                var filter = Builders<BiographySubmission>.Filter.And(
                    Builders<BiographySubmission>.Filter.Eq(submission => submission.SubmissionId, submissionId),
                    Builders<BiographySubmission>.Filter.Eq(submission => submission.Status, "Pending")
                );

                var submission = await submissionCollection
                    .Find(filter)
                    .SingleOrDefaultAsync();

                if (submission == null)
                {
                    return HttpStatusCode.NotFound;
                }

                var update = Builders<BiographySubmission>.Update
                    .Set(member => member.Biography, biography);

                await submissionCollection.UpdateOneAsync(filter, update);

                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to edit biography submission {SubmissionId}: {Message}\nStack Trace:{Trace}", submissionId, ex.Message, ex.StackTrace);
                return HttpStatusCode.InternalServerError;
            }
        }

        public async Task<IEnumerable<UnmappedCharacter>> GetUnmappedCharacters()
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var filter = Builders<FreeCompanyMember>.Filter.And(
                    Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true),
                    Builders<FreeCompanyMember>.Filter.Where(member => string.IsNullOrEmpty(member.DiscordId))
            );

            var unmappedCharacters = await memberCollection
                .Find(filter)
                .ToListAsync();

            var unmappedCharactersResponse = unmappedCharacters
                .OrderBy(character => character.Name)
                .Select(unmappedCharacters => new UnmappedCharacter
                {
                    CharacterId = unmappedCharacters.CharacterId,
                    Name = unmappedCharacters.Name
                });

            return unmappedCharactersResponse;
        }

        public async Task<GetUnmappedDiscordUsersResponse> GetUnmappedDiscordUsersForCharacter(string characterName)
        {
            var discordMembers = await GetAllDiscordMembers();
            var mappedMemberIds = await GetMappedMemberDiscordIds();

            List<UnmappedDiscordUser> unmappedDiscordUsers = discordMembers
                .Select(member => new UnmappedDiscordUser
                {
                    DiscordId = member.DiscordId,
                    ServerNickName = member.Name
                })
                .Where(member => mappedMemberIds.Contains(member.DiscordId) == false)
                .ToList();

            List<UnmappedDiscordUser> suggestedDiscordUsers = [];
            PopulateSuggestedDiscordMembers(discordMembers, suggestedDiscordUsers, characterName);

            var unmappedDiscordUsersResponse = new GetUnmappedDiscordUsersResponse
            {
                SuggestedDiscordUsers = suggestedDiscordUsers.OrderBy(user => user.ServerNickName),
                UnmappedDiscordUsers = unmappedDiscordUsers.OrderBy(user => user.ServerNickName)
            };

            return unmappedDiscordUsersResponse;
        }

        private async Task<IEnumerable<string>> GetMappedMemberDiscordIds()
        {
            var memberCollection = _client.GetDatabase("kupo-life").GetCollection<FreeCompanyMember>("members");
            var mappedMemberFilter = Builders<FreeCompanyMember>.Filter.And(
                Builders<FreeCompanyMember>.Filter.Where(member => string.IsNullOrEmpty(member.DiscordId) == false),
                Builders<FreeCompanyMember>.Filter.Eq(member => member.ActiveMember, true)
            );

            var mappedMembers = await memberCollection
                .Find(mappedMemberFilter)
                .ToListAsync();

            var mappedMemberIds = mappedMembers.Select(member => member.DiscordId);
            return mappedMemberIds;
        }

        private async Task<IEnumerable<DiscordMember>> GetAllDiscordMembers()
        {
            var discordMemberCollection = _client.GetDatabase("kupo-life").GetCollection<DiscordMember>("discord-members");
            var discordMemberFilter = Builders<DiscordMember>.Filter.Empty;

            var discordMembers = await discordMemberCollection
                .Find(discordMemberFilter)
                .ToListAsync();

            return discordMembers;
        }

        private static void PopulateSuggestedDiscordMembers(IEnumerable<DiscordMember> discordMembers, List<UnmappedDiscordUser> suggestedDiscordUsers, string characterName)
        {
            var stringComparer = new NormalizedLevenshtein();

            foreach (var member in discordMembers)
            {
                var characterNameSplit = characterName.Split(" ");
                var firstName = characterNameSplit[0];
                var similarity = stringComparer.Distance(member.Name, characterName);
                if (similarity <= 0.25 || member.Name.Contains(firstName, StringComparison.OrdinalIgnoreCase))
                {
                    var suggestion = new UnmappedDiscordUser
                    {
                        DiscordId = member.DiscordId,
                        ServerNickName = member.Name
                    };

                    suggestedDiscordUsers.Add(suggestion);
                }
            }
        }
    }
}
