namespace states.Services.LeadService;

// Константы «печатает» перед AI-ответом. Длина текста заранее неизвестна (LLM ещё не вызывался
// на момент планирования), поэтому длительность фиксированная, без расчёта от длины ответа.
public static class TypingDefaults
{
    // За сколько секунд до запланированного AiReply стартует «печатает»:
    // тайпинг-таск ставится на ReadDelay + max(0, ReplyDelay - LeadSeconds).
    public const int LeadSeconds = 5;

    // Сколько tgengine держит индикатор: LeadSeconds + запас на опрос воркера (до 1с),
    // ожидание слота и генерацию LLM (~2-4с). Реальная отправка сообщения гасит индикатор
    // раньше дедлайна (cancel-on-send в tgengine), поэтому запас безопасен.
    public const int DurationMs = 12_000;
}
