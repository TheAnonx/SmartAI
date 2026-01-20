namespace SmartAI.Cognitive
{
    /// <summary>
    /// Representa a intenção detectada na entrada do usuário.
    /// </summary>
    public class Intent
    {
        public IntentType Type { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Relation { get; set; }
        public string? Object { get; set; }
        public double Confidence { get; set; }
        public string OriginalInput { get; set; } = string.Empty;
    }

    public enum IntentType
    {
        QUESTION,           // "O que é X?"
        TEACHING,           // "X é Y"
        CODE_REQUEST,       // "Analise este código"
        SEARCH_REQUEST,     // "Pesquise sobre X"
        VALIDATION_RESPONSE, // Resposta a prompt de validação
        UNKNOWN
    }
}