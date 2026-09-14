using System.Text.Json;
using KmOcr.Application.Contracts.Messaging;
using KmOcr.Application.Documents;
using KmOcr.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace KmOcr.Worker;

/// <summary>
/// Background service that consumes OCR jobs from RabbitMQ.
/// </summary>
public sealed class OcrWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OcrWorker> _logger;
    private IChannel? _channel;

    /// <summary>
    /// Creates the worker with scoped service access, RabbitMQ options, and logging.
    /// </summary>
    public OcrWorker(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options, ILogger<OcrWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Connects to RabbitMQ and starts consuming OCR jobs until shutdown.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = CreateConnectionFactory();
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _channel = channel;

        await channel.QueueDeclareAsync(
            queue: _options.OcrQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += ProcessMessageAsync;
        await channel.BasicConsumeAsync(
            queue: _options.OcrQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("OCR worker is consuming queue {QueueName}.", _options.OcrQueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    /// <summary>
    /// Processes one RabbitMQ delivery and acknowledges or rejects it.
    /// </summary>
    private async Task ProcessMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var message = JsonSerializer.Deserialize<OcrJobMessage>(eventArgs.Body.Span)
                ?? throw new InvalidOperationException("OCR job message could not be deserialized.");

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IOcrProcessor>();
            var documents = scope.ServiceProvider.GetRequiredService<IDocumentModule>();
            var result = await processor.ExtractAsync(message.StoragePath, message.ContentType, CancellationToken.None);
            await documents.CompleteOcrAsync(
                new CompleteOcrCommand(message.DocumentId, result.Text, result.ConfidenceScore, result.Engine),
                CancellationToken.None);
            await AcknowledgeAsync(eventArgs.DeliveryTag);
            _logger.LogInformation("OCR completed for document {DocumentId}.", message.DocumentId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "OCR job failed.");
            await RejectAsync(eventArgs.DeliveryTag);
        }
    }

    /// <summary>
    /// Acknowledges a processed message.
    /// </summary>
    private async Task AcknowledgeAsync(ulong deliveryTag)
    {
        if (_channel is not null)
        {
            await _channel.BasicAckAsync(deliveryTag, multiple: false);
        }
    }

    /// <summary>
    /// Rejects a failed message without requeueing to avoid poison-message loops.
    /// </summary>
    private async Task RejectAsync(ulong deliveryTag)
    {
        if (_channel is not null)
        {
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
    }

    /// <summary>
    /// Builds a RabbitMQ connection factory from configured options.
    /// </summary>
    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };
    }
}
