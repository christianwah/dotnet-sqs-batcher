using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

public class SqsBatchSender
{
    private readonly AmazonSQSClient _sqsClient;
    private readonly string _queueUrl;
    private readonly ConcurrentBag<SendMessageRequest> _messageBuffer;
    private readonly int _batchSize;
    private readonly TimeSpan _sendInterval;
    private readonly Timer _timer;

    public SqsBatchSender(AmazonSQSClient sqsClient, string queueUrl, int batchSize, TimeSpan sendInterval)
    {
        _sqsClient = sqsClient;
        _queueUrl = queueUrl;
        _batchSize = batchSize;
        _sendInterval = sendInterval;
        _messageBuffer = new ConcurrentBag<SendMessageRequest>();

        _timer = new Timer(SendBatchMessages, null, sendInterval, sendInterval);
    }

    public async Task Send(SendMessageRequest messageRequest)
    {
        _messageBuffer.Add(messageRequest);
        if (_messageBuffer.Count >= _batchSize)
        {
            SendBatchMessages(null);
        }
    }

    private async void SendBatchMessages(object state)
    {
        try
        {

            List<SendMessageRequest> batchToSend = new List<SendMessageRequest>();
            while (batchToSend.Count < _batchSize && _messageBuffer.TryTake(out var message))
            {
                batchToSend.Add(message);
            }

            if (batchToSend.Count > 0)
            {
                var tasks = SeparateIntoBatches(batchToSend).Select(SendMessageBatch);
                var res = await Task.WhenAll(tasks);
                if (res.Any(x => x.Failed.Any()))
                {
                    throw new Exception("Some entries failed to send to SQS");

                }
            }
        }
        catch (Exception ex)
        {

        }

    }

    private List<List<SendMessageBatchRequestEntry>> SeparateIntoBatches(List<SendMessageRequest> messages)
    {
        var messageBatches = new List<List<SendMessageBatchRequestEntry>>();
        for (var pos = 0; pos < messages.Count; pos += messages.Count)
        {
            messageBatches.Add(messages.GetRange(pos, Math.Min(_batchSize, messages.Count))
            .Select(m => new SendMessageBatchRequestEntry
            {
                Id = Guid.NewGuid().ToString(),
                MessageAttributes = m.MessageAttributes,
                MessageBody = m.MessageBody,
                MessageDeduplicationId = m.MessageDeduplicationId,
                MessageGroupId = m.MessageGroupId,
                MessageSystemAttributes = m.MessageSystemAttributes,
            }).ToList());
        }
        return messageBatches;
    }

    private async Task<SendMessageBatchResponse> SendMessageBatch(List<SendMessageBatchRequestEntry> messageBatches)
    {
        var result = await _sqsClient.SendMessageBatchAsync(new SendMessageBatchRequest
        {
            QueueUrl = _queueUrl,
            Entries = messageBatches,
        });
        return result;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        SendBatchMessages(null);  // Send any remaining messages before disposal
    }
}
