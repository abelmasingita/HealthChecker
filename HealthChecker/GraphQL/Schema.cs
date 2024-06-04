using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Utilities;
using HealthChecker.Models;

namespace HealthChecker.GraphQL
{
    public class ServerType : ObjectGraphType<Server>
    {
        public ServerType(IHttpClientFactory httpClientFactory)
        {
            Name = "Server";
            Description = "A server to monitor";

            Field(h => h.Id);
            Field(h => h.Name);
            Field(h => h.HealthCheckUri);
            Field<DateTimeGraphType>("lastTimeUp", resolve: context => context.Source.LastTimeUp);
            FieldAsync<StringGraphType>("status", resolve: async context => await CheckServerStatus(context, httpClientFactory));
            Field<ServerErrorType>("error", resolve: context => context.Source.Error);


            //Argument < NonNullType<ServerFilterInput>, "filter" > ("filter");

            // Modify the resolver to apply the filter
            //FieldAsync<ListGraphType<ServerType>>(
            //    "filteredServers",
            //    resolve: async context =>
            //    {
            //        var filter = context.Argument<ServerFilterInput>("filter");
            //        var servers = context.Source.Servers; // Assume Servers is a collection of Server objects

            //        // Apply the filter
            //        var filteredServers = servers.Where(s => s.Status == filter.Status).ToList();

            //        return filteredServers;
            //    }
            //);
        }

        private async Task<string> CheckServerStatus(IResolveFieldContext<Server> context, IHttpClientFactory httpClientFactory)
        {
            var server = context.Source;
            var client = httpClientFactory.CreateClient();

            try
            {
                var response = await client.GetAsync(server.HealthCheckUri);
                if (response.IsSuccessStatusCode)
                {
                    server.Status = "UP";
                    server.LastTimeUp = DateTime.UtcNow;
                    server.Error = null;
                }
                else
                {
                    server.Status = "DOWN";
                    server.Error = new ServerError
                    {
                        Status = (int)response.StatusCode,
                        Body = await response.Content.ReadAsStringAsync()
                    };
                }
            }
            catch (Exception ex)
            {
                server.Status = "DOWN";
                server.Error = new ServerError
                {
                    Status = 500,
                    Body = ex.Message
                };
            }

            return server.Status;
        }
    }

    public class ServerErrorType : ObjectGraphType<ServerError>
    {
        public ServerErrorType()
        {
            Name = "ServerError";
            Field(e => e.Status).Description("The HTTP status code of the error");
            Field(e => e.Body).Description("The response body of the error");
        }
    }

    public class HealthCheckerQuery : ObjectGraphType
    {
        private readonly List<Server> _servers;

        public HealthCheckerQuery()
        {
            Name = "Query";

            _servers = new List<Server>
            {
                new Server { Id = "1", Name = "stackworx.io", HealthCheckUri = "https://www.stackworx.io" },
                new Server { Id = "2", Name = "prima.run", HealthCheckUri = "https://prima.run" },
                new Server { Id = "3", Name = "google", HealthCheckUri = "https://www.google.com" }
            };

            Field<ListGraphType<ServerType>>(
                "servers",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "id", Description = "id of server" }
                ),
                resolve: context => _servers
            );

            Field<StringGraphType>(
                "hello",
                resolve: context => "world"
            );
        }
    }

    public class HealthCheckerSchema : Schema
    {
        public HealthCheckerSchema(IServiceProvider provider) : base(provider)
        {
            Query = provider.GetRequiredService<HealthCheckerQuery>();
        }
    }
}
