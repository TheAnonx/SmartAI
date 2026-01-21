namespace SmartAI.Cognitive
{
    /// <summary>
    /// Modos cognitivos do sistema.
    /// O sistema opera SEMPRE em um modo explícito.
    /// </summary>
    public enum CognitiveMode
    {
        /// <summary>
        /// Responder com conhecimento validado de alta confiança.
        /// </summary>
        ANSWER,

        /// <summary>
        /// Investigar fontes externas (web).
        /// NÃO persiste automaticamente.
        /// </summary>
        INVESTIGATION,

        /// <summary>
        /// Apresentar candidatos para validação humana.
        /// </summary>
        VALIDATION,

        /// <summary>
        /// Persistir fatos após validação humana.
        /// NUNCA executa sem aprovação prévia.
        /// </summary>
        LEARNING,

        /// <summary>
        /// Analisar código (domínio separado).
        /// </summary>
        CODE_ANALYSIS
    }
}