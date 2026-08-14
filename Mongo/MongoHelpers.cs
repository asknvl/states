using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Runtime.CompilerServices;

namespace states.Mongo
{
    public static class MongoHelpers
    {
        // Multi-документные операции (UpdateMany/DeleteMany) не покрываются retryable writes
        // драйвера: обрыв соединения из пула роняет их с MongoConnectionException без повтора,
        // а выше по стеку ошибка приводит к потере операции (коммит оффсета в консьюмере,
        // Fail таски в воркере). Оборачивать можно только идемпотентные операции.
        public static async Task<T> RetryOnConnectionLoss<T>(
            Func<Task<T>> op,
            ILogger logger,
            [CallerMemberName] string operation = "",
            int attempts = 3)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return await op();
                }
                catch (MongoConnectionException ex) when (attempt < attempts)
                {
                    logger.LogError(ex,
                        "Mongo operation {Operation} failed with connection loss (attempt {Attempt}/{Attempts}), retrying",
                        operation, attempt, attempts);

                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
                }
            }
        }
    }
}
