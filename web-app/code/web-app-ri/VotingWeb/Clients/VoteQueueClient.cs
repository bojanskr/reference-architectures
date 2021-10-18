// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using VotingWeb.Exceptions;
using VotingWeb.Interfaces;

namespace VotingWeb.Clients
{
    public class VoteQueueClient : IVoteQueueClient
    {
        private readonly ServiceBusSender queueClient;

        public VoteQueueClient(string connectionString, string queueName)
        {
            try
            {
                var serviceBusClient = new ServiceBusClient(connectionString);
                queueClient = serviceBusClient.CreateSender(queueName);
            }
            catch (Exception ex) when (ex is ArgumentException ||
                              ex is ServiceBusException ||
                              ex is UnauthorizedAccessException ||
                              ex is ArgumentNullException)
            {
                throw new VoteQueueException("Initialization Error for service bus", ex);
            }
        }

        public async Task SendVoteAsync(int id)
        {
            var messageBody = new { Id = id };

            try
            {
                await queueClient.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(messageBody))
                {
                    ContentType = "application/json"
                });
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                 ex is ServiceBusException ||
                                 ex is UnauthorizedAccessException)
            {
                throw new VoteQueueException("Service Bus Exception occurred with sending message to queue", ex);
            }
        }
    }
}