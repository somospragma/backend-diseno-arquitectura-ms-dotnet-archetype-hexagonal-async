using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Pragma.Accounts.Domain.Model;
using Pragma.Accounts.Domain.Model.Exceptions;
using Pragma.Accounts.Domain.Ports;

namespace Pragma.Accounts.Infrastructure.Messaging;

public sealed class SqsOptions
{
    public const string Section = "Messaging:Sqs";

    /// <summary>URL de la cola destino de los eventos de dominio.</summary>
    public string QueueUrl { get; set; } = string.Empty;

    /// <summary>Endpoint alterno (LocalStack). Vacio usa el endpoint regional de AWS.</summary>
    public string? ServiceUrl { get; set; }
}

/// <summary>Publica eventos de dominio en SQS como JSON, con el tipo como atributo del mensaje.</summary>
internal sealed class SqsEventPublisher(IAmazonSQS sqs, SqsOptions options) : IEventPublisher
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(AccountEvent accountEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountEvent);
        var request = new SendMessageRequest
        {
            QueueUrl = options.QueueUrl,
            MessageBody = JsonSerializer.Serialize(accountEvent, Json),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["eventType"] = new() { DataType = "String", StringValue = accountEvent.Type },
                ["eventId"] = new() { DataType = "String", StringValue = accountEvent.EventId },
            },
        };

        try
        {
            await sqs.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonSQSException ex)
        {
            throw new TechnicalException(ErrorCatalog.EventPublisherUnavailable, "Event could not be published", ex);
        }
    }
}

public static class MessagingServiceCollectionExtensions
{
    /// <summary>Registra SQS como publicador de eventos de dominio.</summary>
    public static IServiceCollection AddSqsEventPublisher(this IServiceCollection services, SqsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.QueueUrl);
        services.AddSingleton(options);
        services.AddSingleton<IAmazonSQS>(_ => string.IsNullOrWhiteSpace(options.ServiceUrl)
            ? new AmazonSQSClient()
            : new AmazonSQSClient(new AmazonSQSConfig { ServiceURL = options.ServiceUrl }));
        services.AddSingleton<IEventPublisher, SqsEventPublisher>();
        return services;
    }
}
