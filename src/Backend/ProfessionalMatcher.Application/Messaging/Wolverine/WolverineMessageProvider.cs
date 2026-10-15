using System.Diagnostics.CodeAnalysis;
using ProfessionalMatcher.Domain.Interfaces;
using Wolverine;

namespace ProfessionalMatcher.Application.Messaging.Wolverine;

[ExcludeFromCodeCoverage]
internal class WolverineMessageProvider(IMessageBus bus) : IBusProvider
{
    public Task<T> InvokeAsync<T>(object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        bus.InvokeAsync<T>(message, cancellationToken, timeout);

    public Task PublishAsync<T>(T? data, CancellationToken cancellationToken = default) where T : class =>
        bus.PublishAsync(data).AsTask();
}
