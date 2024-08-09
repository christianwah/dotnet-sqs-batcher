
using Amazon.SQS.Model;

public interface ISqsBatchSender : IDisposable
{
    /// <summary>
    /// Queues a message to be sent to the SQS queue.
    /// </summary>
    /// <param name="messageRequest">The message request to be queued.</param>
    void QueueMessage(SendMessageRequest messageRequest);
}