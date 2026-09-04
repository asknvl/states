namespace states.Services.LeadService;

// Параметры «печатает» перед отправкой. Длина ответа на момент планирования неизвестна
// (LLM ещё не вызывался), поэтому время печатания не от текста, а случайное в диапазоне —
// чтобы паузы не выглядели механически одинаковыми от сообщения к сообщению.
public static class TypingDefaults
{
    // Диапазон: за сколько секунд до запланированной отправки стартует «печатает»
    public const int MinLeadSeconds = 5;
    public const int MaxLeadSeconds = 10;

    // Если до отправки остаётся меньше этого окна — индикатор показать не успеем, пропускаем
    public const int MinVisibleSeconds = 2;

    // Насколько позже scheduledAt тайпинг ещё имеет смысл показывать. Дальше — молча
    // пропускаем: очередь воркера отстала, ответ уже рядом (или уже ушёл), и «печатает»
    // после сообщения выглядит хуже, чем его отсутствие. Штатный путь укладывается в ~1с
    // опроса, а ретраи (минимум 30с) отсекаются целиком.
    public const int StaleAfterSeconds = 3;

    // Запас поверх lead: опрос воркера (до 1с), ожидание слота, генерация LLM (~2-4с).
    // Реальная отправка гасит индикатор раньше дедлайна (cancel-on-send в tgengine),
    // поэтому запас безопасен.
    public const int CushionMs = 4_000;

    // Фолбэк для тасков без durationMs (созданных кодом до появления поля)
    public const int FallbackDurationMs = 12_000;

    // Случайный lead, обрезанный по доступному окну ожидания: тайпинг всегда целиком
    // укладывается до запланированной отправки. Окно меньше MinVisibleSeconds — не показываем.
    public static int? DrawLeadSeconds(double availableWindowSeconds)
    {
        if (availableWindowSeconds < MinVisibleSeconds)
            return null;

        var drawn = Random.Shared.Next(MinLeadSeconds, MaxLeadSeconds + 1);
        return (int)Math.Min(drawn, Math.Floor(availableWindowSeconds));
    }

    public static int DurationMsForLead(int leadSeconds) => leadSeconds * 1000 + CushionMs;
}
