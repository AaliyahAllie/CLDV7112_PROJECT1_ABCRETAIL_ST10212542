
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT1_ABCRETAIL_ST10212542.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(string connectionString)
        {
            // We use standard option to base64 encode/decode messages automatically in modern Azure SDK,
            // or do it manually. We'll handle encoding manually to be safe and compatible with all client setups.
            _queueClient = new QueueClient(connectionString, "order-processing-queue");
            _queueClient.CreateIfNotExists();
        }

        public async Task SendMessageAsync(string messageText)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(messageText);
            var base64Message = Convert.ToBase64String(bytes);
            await _queueClient.SendMessageAsync(base64Message);
        }

        public async Task<List<QueueMessageModel>> GetMessagesAsync(int maxMessages = 20)
        {
            var result = new List<QueueMessageModel>();

            // PeekMessages doesn't change visibility, which is ideal for a read-only list on a dashboard
            var response = await _queueClient.PeekMessagesAsync(maxMessages);

            foreach (var msg in response.Value)
            {
                string decodedText;
                try
                {
                    var bytes = Convert.FromBase64String(msg.MessageText);
                    decodedText = System.Text.Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    decodedText = msg.MessageText; // Fallback
                }

                result.Add(new QueueMessageModel
                {
                    MessageId = msg.MessageId,
                    MessageText = decodedText,
                    InsertionTime = msg.InsertedOn,
                    ExpirationTime = msg.ExpiresOn
                });
            }

            return result;
        }

        public async Task<QueueMessageModel> DequeueMessageAsync()
        {
            // Dequeue actually receives the message and hides it from other consumers
            var response = await _queueClient.ReceiveMessagesAsync(1);
            if (response.Value.Length > 0)
            {
                var msg = response.Value[0];
                string decodedText;
                try
                {
                    var bytes = Convert.FromBase64String(msg.MessageText);
                    decodedText = System.Text.Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    decodedText = msg.MessageText;
                }

                // Delete it immediately as we've processed it
                await _queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);

                return new QueueMessageModel
                {
                    MessageId = msg.MessageId,
                    MessageText = decodedText,
                    InsertionTime = msg.InsertedOn
                };
            }
            return null;
        }

        public async Task ClearQueueAsync()
        {
            await _queueClient.ClearMessagesAsync();
        }
    }
}
