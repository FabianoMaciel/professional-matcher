namespace ProfessionalMatcher.Domain.Interfaces;

public interface IBusProvider
{
    Task PublishAsync<T>(T? data, CancellationToken cancellationToken = default) where T : class;

    Task<T> InvokeAsync<T>(object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
