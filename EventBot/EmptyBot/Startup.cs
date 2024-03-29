﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using EventBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventBot
{
    /// <summary>
    /// The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        /// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
        /// <seealso cref="IStatePropertyAccessor{T}"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
        public void ConfigureServices(IServiceCollection services)
        {
            var secretKey = Configuration.GetSection("botFileSecret")?.Value;

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            var botConfig = BotConfiguration.Load(@".\EventBot.bot", secretKey);
            services.AddSingleton(sp => botConfig);

            // Initialize Bot Connected Services clients.
            var connectedServices = new BotServices(botConfig);
            services.AddSingleton(sp => connectedServices);
            services.AddSingleton(sp => botConfig);

            services.AddBot<EventBot>(options =>
            {
                // Retrieve current endpoint.
                var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == "development").FirstOrDefault();
                if (!(service is EndpointService endpointService))
                {
                    throw new InvalidOperationException($"The .bot file does not contain a development endpoint.");
                }

                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                // Catches any errors that occur during a conversation turn and logs them.
                options.OnTurnError = async (context, exception) =>
                {
                    await context.SendActivityAsync("Sorry, it looks like something went wrong.");
                };
            });

            // Add storage
            IStorage dataStore = new MemoryStorage();
            ConversationState conversationState = new ConversationState(dataStore);
            //UserState userState = new UserState(dataStore);

            services.AddSingleton<FindEventDialog>();
            services.AddSingleton<EventService>();

            services.AddSingleton<EventBotAccessors>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the state accessors");
                }

                var cosmosSettings = Configuration.GetSection("CosmosDB");
                IStorage storage = new CosmosDbStorage(
                    new CosmosDbStorageOptions
                    {
                        DatabaseId = cosmosSettings["DatabaseID"],
                        CollectionId = cosmosSettings["CollectionID"],
                        CosmosDBEndpoint = new Uri(cosmosSettings["EndpointUri"]),
                        AuthKey = cosmosSettings["AuthenticationKey"],
                    });
                options.State.Add(new ConversationState(storage));
                options.State.Add(new UserState(storage));

                //ConversationState conversationState = new ConversationState(storage);
                UserState userState = new UserState(storage);

                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                var accessors = new EventBotAccessors(conversationState, userState)
                {
                    DialogState = conversationState.CreateProperty<DialogState>(EventBotAccessors.DialogStateAccessorKey),
                    EventParamState = conversationState.CreateProperty<EventParams>(EventBotAccessors.EventParamStateAccessorKey),
                    UserProfileState = userState.CreateProperty<UserProfile>(EventBotAccessors.UserProfileName),
                };

                return accessors;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
