using MediatR;
using MogTomeApi.Shared;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;

namespace MogTomeApi.Features.Chronicle.Queries
{
    public class GetEvents
    {
        public class Query : IRequest<Result<PaginatedEventsResponse>>
        {
            public string Cursor { get; set; }
            public int Limit { get; set; }
            public string QueryString { get; set; }
            public string EventTypeFilter { get; set; }
        }

        public class EventDto
        {
            public ObjectId Id { get; set; }
            public string Text { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Type { get; set; }
        }

        public class PaginatedEventsResponse
        {
            public IEnumerable<EventDto> Events { get; set; }
            public string NextCursor { get; set; }
            public bool HasMore { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<PaginatedEventsResponse>>
        {
            private readonly IMongoDatabase _mongoDatabase;

            public Handler(IMongoDatabase mongoDatabase)
            {
                _mongoDatabase = mongoDatabase;
            }

            public async Task<Result<PaginatedEventsResponse>> Handle(Query request, CancellationToken cancellationToken)
            {
                var events = await GetChronicleEvents(request.Cursor, request.Limit, request.QueryString, request.EventTypeFilter);

                string nextCursor = null;
                var hasMore = events.Count > request.Limit;

                if (hasMore)
                {
                    var lastEvent = events[^2];
                    nextCursor = CursorHelper.CalculateNextCursor(events.Count, request.Limit, lastEvent.CreatedAt, lastEvent.Id);
                }

                events = events.Take(request.Limit).ToList();

                var response = new PaginatedEventsResponse
                {
                    Events = events.Select(e => new EventDto
                    {
                        Id = e.Id,
                        Text = e.Text,
                        CreatedAt = e.CreatedAt,
                        Type = e.Type
                    }),
                    NextCursor = nextCursor,
                    HasMore = hasMore
                };

                return new Result<PaginatedEventsResponse>(response, HttpStatusCode.OK, true, null);
            }

            private async Task<List<Event>> GetChronicleEvents(string cursor, int limit, string eventTextQuery, string eventTypeFilter)
            {
                var eventsCollection = _mongoDatabase.GetCollection<Event>("events");
                var filters = BuildFilters(cursor, eventTypeFilter);

                var eventsSearch = BuildTextSearch(eventsCollection, eventTextQuery);
                var events = await eventsSearch
                    .Match(filters)
                    .SortByDescending(e => e.CreatedAt)
                    .ThenByDescending(e => e.Id)
                    .Limit(limit + 1)
                    .ToListAsync();

                return events.ToList();
            }

            private static FilterDefinition<Event> BuildFilters(string cursor, string eventTypeFilter)
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

            private static IAggregateFluent<Event> BuildTextSearch(IMongoCollection<Event> eventsCollection, string eventTextQuery)
            {
                var eventsSearch = eventsCollection.Aggregate();

                if (string.IsNullOrEmpty(eventTextQuery) == false)
                {
                    eventsSearch = eventsSearch.Search(Builders<Event>.Search.Autocomplete(g => g.Text, eventTextQuery), indexName: "event-index");
                }

                return eventsSearch;
            }
        }
    }
}
