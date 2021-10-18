// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Data.SqlClient;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public static class VoteCounter
    {
        [FunctionName("VoteCounter")]
        public static async Task Run(
            [ServiceBusTrigger("sbq-voting", Connection = "SERVICEBUS_CONNECTION_STRING")]string myQueueItem,
            CancellationToken cancellationToken,
            ILogger log)
        {
            var vote = JsonSerializer.Deserialize<Vote>(myQueueItem);

            try
            {
                using var conn = new SqlConnection(Environment.GetEnvironmentVariable("sqldb_connection"));
                await conn.OpenAsync(cancellationToken);

                using var cmd = new SqlCommand("UPDATE dbo.Counts SET Count = Count + 1 WHERE ID = @ID;", conn);
                cmd.Parameters.AddWithValue("@ID", vote.Id);

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    log.LogError("Entry not found on the database for ID: {id}", vote.Id);
                }
            }
            catch (Exception ex) when (ex is ArgumentNullException ||
                                       ex is SecurityException ||
                                       ex is SqlException)
            {
                log.LogError(ex, "Sql Exception");
            }
        }

        private class Vote
        {
            public int Id { get; set; }
        }
    }
}
