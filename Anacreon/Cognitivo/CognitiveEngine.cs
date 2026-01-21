using Microsoft.EntityFrameworkCore;
using SmartAI.Data;
using SmartAI.Models;
using SmartAI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartAI.Cognitive
{
    /// <summary>
    /// Motor cognitivo principal do sistema.
    /// Implementa o pipeline epistêmico completo.
    /// </summary>
    public class CognitiveEngine
    {
        private readonly AIContext _context;
        private readonly IntentDetector _intentDetector;
        private readonly FactService _factService;
        private readonly InvestigationService _investigationService;
        private readonly ValidationService _validationService;
        private readonly ConflictDetectionService _conflictService;
        private readonly CodeAnalysisService _codeAnalysisService;

        private CognitiveMode _currentMode;
        private const double ASSERTION_THRESHOLD = 0.85;

        public CognitiveEngine(AIContext context)
        {
            _context = context;
            _intentDetector = new IntentDetector();
            _factService = new FactService(context);
            _investigationService = new InvestigationService(context);
            _validationService = new ValidationService(context);
            _conflictService = new ConflictDetectionService(context);
            _codeAnalysisService = new CodeAnalysisService();
        }

        /// <summary>
        /// Processa entrada do usuário seguindo o pipeline cognitivo.
        /// </summary>
        public async Task<CognitiveResponse> Process(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new CognitiveResponse
                {
                    Text = "Por favor, envie uma mensagem válida.",
                    Mode = CognitiveMode.ANSWER,
                    Success = false
                };
            }

            // 1. INTENT DETECTION
            var intent = _intentDetector.Detect(input);

            // 2. CONFIDENCE CHECK
            var hasReliableKnowledge = await CheckKnowledgeConfidence(intent);

            // 3. MODE SELECTION
            _currentMode = SelectCognitiveMode(intent, hasReliableKnowledge);

            // 4. VALIDATE PERMISSIONS
            // (já validado implicitamente pelo SelectCognitiveMode)

            // 5. EXECUTE
            CognitiveResponse response;

            try
            {
                response = _currentMode switch
                {
                    CognitiveMode.ANSWER => await ExecuteAnswerMode(intent, hasReliableKnowledge),
                    CognitiveMode.INVESTIGATION => await ExecuteInvestigationMode(intent),
                    CognitiveMode.CODE_ANALYSIS => await ExecuteCodeAnalysisMode(input),
                    CognitiveMode.VALIDATION => await ExecuteValidationMode(intent),
                    _ => new CognitiveResponse
                    {
                        Text = "Não entendi sua solicitação. Pode reformular?",
                        Mode = _currentMode,
                        Success = false
                    }
                };
            }
            catch (Exception ex)
            {
                response = new CognitiveResponse
                {
                    Text = $"❌ Erro ao processar: {ex.Message}",
                    Mode = _currentMode,
                    Success = false
                };
            }

            // Salvar conversação
            await SaveConversation(input, "user");
            await SaveConversation(response.Text, "assistant");

            return response;
        }

        /// <summary>
        /// Modo ANSWER: Responder com conhecimento validado.
        /// </summary>
        private async Task<CognitiveResponse> ExecuteAnswerMode(
            Intent intent,
            bool hasReliableKnowledge)
        {
            // Validar permissão
            CognitivePermissions.ValidateAction(_currentMode, CognitiveAction.Assert);

            if (!hasReliableKnowledge)
            {
                // Oferecer investigação
                return new CognitiveResponse
                {
                    Text = $"❓ Não tenho conhecimento confiável sobre '{intent.Subject}'.\n\n" +
                           $"Opções:\n" +
                           $"1️⃣ Posso investigar na web\n" +
                           $"2️⃣ Você pode me ensinar diretamente\n\n" +
                           $"O que prefere?",
                    Mode = CognitiveMode.ANSWER,
                    Success = true,
                    RequiresAction = true,
                    SuggestedMode = CognitiveMode.INVESTIGATION
                };
            }

            // Buscar fatos validados
            var facts = await _factService.FindValidatedFacts(intent.Subject);

            if (!facts.Any())
            {
                return new CognitiveResponse
                {
                    Text = $"❓ Não encontrei fatos validados sobre '{intent.Subject}'.",
                    Mode = CognitiveMode.ANSWER,
                    Success = false
                };
            }

            // Construir resposta com confiança
            var avgConfidence = facts.Average(f => f.Confidence);
            var answer = $"📖 **{intent.Subject}** (confiança média: {avgConfidence:P0})\n\n";

            foreach (var fact in facts.OrderByDescending(f => f.Confidence).Take(5))
            {
                answer += $"• **{fact.Relation}**: {fact.Object}\n";
                answer += $"  └─ Confiança: {fact.Confidence:P0} | ";
                answer += $"Fonte: {fact.Sources.First().Type} | ";
                answer += $"Validado: {fact.ValidatedAt:dd/MM/yyyy}\n\n";
            }

            return new CognitiveResponse
            {
                Text = answer,
                Mode = CognitiveMode.ANSWER,
                Success = true,
                Confidence = avgConfidence,
                Facts = facts
            };
        }

        /// <summary>
        /// Modo INVESTIGATION: Buscar informações externas.
        /// </summary>
        private async Task<CognitiveResponse> ExecuteInvestigationMode(Intent intent)
        {
            // Validar permissão
            CognitivePermissions.ValidateAction(_currentMode, CognitiveAction.SearchWeb);

            var investigationResult = await _investigationService.Investigate(intent.Subject);

            if (!investigationResult.Success)
            {
                return new CognitiveResponse
                {
                    Text = $"❌ Falha na investigação: {investigationResult.Error}",
                    Mode = CognitiveMode.INVESTIGATION,
                    Success = false
                };
            }

            if (!investigationResult.CandidateFacts.Any())
            {
                return new CognitiveResponse
                {
                    Text = $"🔍 Investiguei sobre '{intent.Subject}', mas não encontrei fatos estruturados.\n\n" +
                           $"Resumo da busca:\n{investigationResult.RawText}\n\n" +
                           $"Fonte: {investigationResult.SourceName}",
                    Mode = CognitiveMode.INVESTIGATION,
                    Success = true
                };
            }

            // Preparar para validação
            var response = $"🔍 **Investigação Concluída**\n\n";
            response += $"Encontrei {investigationResult.CandidateFacts.Count} possíveis fatos sobre '{intent.Subject}':\n\n";

            for (int i = 0; i < investigationResult.CandidateFacts.Count; i++)
            {
                var fact = investigationResult.CandidateFacts[i];
                response += $"{i + 1}. {fact.Subject} {fact.Relation} {fact.Object}\n";
            }

            response += $"\n📚 Fonte: {investigationResult.SourceName}\n";
            response += $"🔗 {investigationResult.SourceUrl}\n\n";
            response += $"⚠️ **IMPORTANTE**: Estes são CANDIDATOS, não fatos validados.\n";
            response += $"Deseja validá-los?";

            return new CognitiveResponse
            {
                Text = response,
                Mode = CognitiveMode.INVESTIGATION,
                Success = true,
                RequiresAction = true,
                SuggestedMode = CognitiveMode.VALIDATION,
                CandidateFacts = investigationResult.CandidateFacts,
                InvestigationResult = investigationResult
            };
        }

        /// <summary>
        /// Modo CODE_ANALYSIS: Analisar código (domínio separado).
        /// </summary>
        private async Task<CognitiveResponse> ExecuteCodeAnalysisMode(string input)
        {
            // Validar permissão
            CognitivePermissions.ValidateAction(_currentMode, CognitiveAction.SearchWeb);

            var codeBlock = ExtractCodeBlock(input);

            if (string.IsNullOrEmpty(codeBlock))
            {
                return new CognitiveResponse
                {
                    Text = "❌ Não encontrei código para analisar. Use blocos ```código```.",
                    Mode = CognitiveMode.CODE_ANALYSIS,
                    Success = false
                };
            }

            var analysis = _codeAnalysisService.AnalyzeCode(codeBlock);

            // Criar CodeInsight (NÃO Fact)
            var insight = new CodeInsight
            {
                Language = analysis.Language,
                Context = "Análise de código fornecido pelo usuário",
                Observation = $"Complexidade: {analysis.Complexity}",
                CodeSnippet = codeBlock.Length > 1000 ? codeBlock.Substring(0, 1000) : codeBlock,
                CreatedAt = DateTime.Now
            };

            // Adicionar sugestões
            if (analysis.Issues.Any())
            {
                insight.Suggestion = string.Join("; ", analysis.Issues);
                insight.Severity = InsightSeverity.WARNING;
            }

            // Salvar insight
            _context.CodeInsights.Add(insight);
            await _context.SaveChangesAsync();

            // Formatar resposta
            var response = $"🔍 **Análise de Código**\n\n";
            response += $"**Linguagem**: {analysis.Language}\n";
            response += $"**Complexidade**: {analysis.Complexity}\n\n";

            if (analysis.Issues.Any())
            {
                response += $"⚠️ **Problemas Encontrados** ({analysis.Issues.Count}):\n";
                foreach (var issue in analysis.Issues)
                {
                    response += $"• {issue}\n";
                }
                response += "\n";
            }

            if (analysis.Suggestions.Any())
            {
                response += $"💡 **Sugestões** ({analysis.Suggestions.Count}):\n";
                foreach (var suggestion in analysis.Suggestions.Take(5))
                {
                    response += $"• {suggestion}\n";
                }
            }

            response += $"\n📝 **Nota**: Esta análise foi salva como CodeInsight #{insight.Id}";

            return new CognitiveResponse
            {
                Text = response,
                Mode = CognitiveMode.CODE_ANALYSIS,
                Success = true,
                CodeInsight = insight
            };
        }

        /// <summary>
        /// Modo VALIDATION: Processar validação humana.
        /// </summary>
        private async Task<CognitiveResponse> ExecuteValidationMode(Intent intent)
        {
            // Este modo é acionado pela UI de validação
            return new CognitiveResponse
            {
                Text = "Modo de validação - use a interface gráfica.",
                Mode = CognitiveMode.VALIDATION,
                Success = true
            };
        }

        /// <summary>
        /// Verifica se há conhecimento confiável sobre o subject.
        /// </summary>
        private async Task<bool> CheckKnowledgeConfidence(Intent intent)
        {
            if (string.IsNullOrEmpty(intent.Subject))
                return false;

            return await _factService.HasReliableKnowledge(intent.Subject);
        }

        /// <summary>
        /// Seleciona o modo cognitivo apropriado.
        /// </summary>
        private CognitiveMode SelectCognitiveMode(Intent intent, bool hasReliableKnowledge)
        {
            switch (intent.Type)
            {
                case IntentType.CODE_REQUEST:
                    return CognitiveMode.CODE_ANALYSIS;

                case IntentType.SEARCH_REQUEST:
                    return CognitiveMode.INVESTIGATION;

                case IntentType.TEACHING:
                    return CognitiveMode.VALIDATION; // Ensino requer validação

                case IntentType.QUESTION:
                    // Se tem conhecimento confiável, responder
                    // Senão, oferecer investigação
                    return hasReliableKnowledge
                        ? CognitiveMode.ANSWER
                        : CognitiveMode.INVESTIGATION;

                case IntentType.VALIDATION_RESPONSE:
                    return CognitiveMode.VALIDATION;

                default:
                    return CognitiveMode.ANSWER;
            }
        }

        private string ExtractCodeBlock(string input)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                input,
                @"```[\w]*\s*([\s\S]*?)\s*```");

            return match.Success ? match.Groups[1].Value : "";
        }

        private async Task SaveConversation(string message, string role)
        {
            var conversation = new Conversation
            {
                Message = message,
                Role = role,
                Timestamp = DateTime.Now
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Resposta do motor cognitivo.
    /// </summary>
    public class CognitiveResponse
    {
        public string Text { get; set; } = string.Empty;
        public CognitiveMode Mode { get; set; }
        public bool Success { get; set; }
        public double Confidence { get; set; }
        public bool RequiresAction { get; set; }
        public CognitiveMode? SuggestedMode { get; set; }

        // Dados opcionais
        public List<Fact>? Facts { get; set; }
        public List<Fact>? CandidateFacts { get; set; }
        public InvestigationResult? InvestigationResult { get; set; }
        public CodeInsight? CodeInsight { get; set; }
    }
}