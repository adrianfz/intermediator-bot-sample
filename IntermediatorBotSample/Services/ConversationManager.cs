﻿using FizzWare.NBuilder;
using IntermediatorBotSample.Contracts;
using IntermediatorBotSample.Models;
using IntermediatorBotSample.Settings;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Underscore.Bot.MessageRouting.DataStore;

namespace IntermediatorBotSample.Services
{
    public class ConversationManager : IConversationManager
    {
        private readonly IExceptionHandler   _exceptionHandler;
        private readonly IRoutingDataStore   _routingDataStore;
        private readonly HandoffHelper       _handoffHelper;

        public ConversationManager(IExceptionHandler exceptionHandler, IRoutingDataStore routingDataStore, BotSettings botSettings)
        {
            _exceptionHandler   = exceptionHandler;
            _routingDataStore   = routingDataStore;
            _handoffHelper      = new HandoffHelper(botSettings); 
        }


        public void DeleteConversation(string channelId, string conversationId)
        {
            // Get all connections
            var allConnections        = _exceptionHandler.Get(() => _routingDataStore.GetConnections());

            // Find the first matching connection that contains channelId and conversationId
            var connection            = allConnections?.FirstOrDefault(c => 
            (  c.ConversationReference1.ChannelId == channelId && c.ConversationReference1.Conversation.Id == conversationId)
            || c.ConversationReference2.ChannelId == channelId && c.ConversationReference2.Conversation.Id == conversationId);


            var conversationReference1 = connection?.ConversationReference1;
            var conversationReference2 = connection?.ConversationReference2;

            // Finally delete both, if found
            if (conversationReference1 != null)
            {
                _exceptionHandler.Execute(() => _handoffHelper.MessageRouter.Disconnect(conversationReference1));
            }

            if(conversationReference2 != null)
            {
                _exceptionHandler.Execute(() => _handoffHelper.MessageRouter.Disconnect(conversationReference2));
            }
        }


        public IEnumerable<Conversation> GetConversations(int top)
        {
            if (top <= 0 || top > 50)
                return null;

            var channels           = new[] { "facebook", "skype", "skype for business", "directline" };
            var random             = new RandomGenerator();
            
            var connectionRequests = _exceptionHandler.Get(() => _routingDataStore.GetConnectionRequests());

            return Builder<Conversation>.CreateListOfSize(top)
                .All()
                    .With(o => o.ConversationInformation = Builder<ConversationInformation>.CreateNew()
                    .With(ci => ci.MessagesCount = random.Next(2, 30))
                    .With(ci => ci.SentimentScore = random.Next(0.0d, 1.0d))
                    .Build())
                    .With(o => o.ConversationReference = Builder<ConversationReference>.CreateNew()
                    .With(cr => cr.ChannelId = channels[random.Next(0, channels.Count())])
                    .With(cr => cr.User = Builder<ChannelAccount>.CreateNew().Build())
                    .With(cr => cr.Bot  = Builder<ChannelAccount>.CreateNew().Build())
                    .With(cr => cr.Conversation = Builder<ConversationAccount>.CreateNew().Build())
                    .Build())
                    .With(o => o.UserInformation = Builder<SampleUserInformation>.CreateNew()
                    .Build())
                .Build()
                .ToList();
        }


        /// <summary>
        /// Sample UserInformation override class, used for demo purposes only
        /// </summary>
        private class SampleUserInformation : UserInformation
        {
            [JsonProperty(PropertyName = "isvip")]
            public bool IsVip { get; set; }


            [JsonProperty(PropertyName = "department")]
            public string Department { get; set; }
        }

    }
}
