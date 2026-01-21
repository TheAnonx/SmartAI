using Microsoft.EntityFrameworkCore;
using SmartAI.Data;
using SmartAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartAI.Services
{
    /// <summary>
    /// Gerencia sessões de validação humana.
    /// O usuário é a autoridade epistemológica final.
    /// </summary>
    public class ValidationService
    {
        private readonly AIContext _context;
        private readonly FactService _factService;

        public ValidationService(AIContext context)
        {
            _context = context;
            _factService = new FactService(context);
        }

        /// <summary>
        /// Inicia uma sessão de validação.
        /// </summary>
        public async Task<ValidationSession> StartValidationSession(
            string query,
            List<Fact> candidateFacts)
        {
            var session = new ValidationSession
            {
                Query = query,
                StartedAt = DateTime.Now,
                CandidatesPresented = candidateFacts.Count,
                WasCompleted = false
            };

            _context.ValidationSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        /// <summary>
        /// Processa as decisões do usuário sobre os candidatos.
        /// </summary>
        public async Task<ValidationResult> ProcessUserDecisions(
            int sessionId,
            List<FactDecision> decisions,
            string userId = "user")
        {
            var session = await _context.ValidationSessions.FindAsync(sessionId);

            if (session == null)
                throw new InvalidOperationException($"Session {sessionId} not found");

            var result = new ValidationResult
            {
                SessionId = sessionId,
                ProcessedAt = DateTime.Now
            };

            foreach (var decision in decisions)
            {
                try
                {
                    switch (decision.Action)
                    {
                        case ValidationAction.APPROVE:
                            // Primeiro, persistir o candidato se ainda não estiver no DB
                            Fact fact;

                            if (decision.Fact.Id == 0)
                            {
                                // Criar novo fato candidato
                                fact = await _factService.CreateCandidateFact(
                                    decision.Fact.Subject,
                                    decision.Fact.Relation,
                                    decision.Fact.Object,
                                    decision.Fact.Sources.First().Type,
                                    decision.Fact.Sources.First().Identifier,
                                    decision.Fact.Sources.First().URL);
                            }
                            else
                            {
                                fact = decision.Fact;
                            }

                            // Validar
                            await _factService.ValidateFact(
                                fact.Id,
                                userId,
                                decision.Confidence ?? 0.90);

                            result.Approved.Add(fact);
                            session.FactsApproved++;
                            break;

                        case ValidationAction.REJECT:
                            if (decision.Fact.Id > 0)
                            {
                                await _factService.RejectFact(
                                    decision.Fact.Id,
                                    userId,
                                    decision.Reason);
                            }

                            result.Rejected.Add(decision.Fact);
                            session.FactsRejected++;
                            break;

                        case ValidationAction.EDIT:
                            // Se o fato existe, editar
                            if (decision.Fact.Id > 0)
                            {
                                var edited = await _factService.EditCandidateFact(
                                    decision.Fact.Id,
                                    decision.EditedSubject,
                                    decision.EditedRelation,
                                    decision.EditedObject,
                                    userId);

                                // Depois validar com as edições
                                await _factService.ValidateFact(
                                    edited.Id,
                                    userId,
                                    decision.Confidence ?? 0.90);

                                result.Edited.Add(edited);
                                session.FactsEdited++;
                            }
                            else
                            {
                                // Criar novo com valores editados
                                var newFact = await _factService.CreateCandidateFact(
                                    decision.EditedSubject ?? decision.Fact.Subject,
                                    decision.EditedRelation ?? decision.Fact.Relation,
                                    decision.EditedObject ?? decision.Fact.Object,
                                    decision.Fact.Sources.First().Type,
                                    decision.Fact.Sources.First().Identifier,
                                    decision.Fact.Sources.First().URL);

                                await _factService.ValidateFact(
                                    newFact.Id,
                                    userId,
                                    decision.Confidence ?? 0.90);

                                result.Edited.Add(newFact);
                                session.FactsEdited++;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Erro ao processar fato '{decision.Fact.Subject}': {ex.Message}");
                }
            }

            // Finalizar sessão
            session.CompletedAt = DateTime.Now;
            session.WasCompleted = true;
            session.UserId = userId;

            await _context.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Obtém estatísticas de validação.
        /// </summary>
        public async Task<ValidationStats> GetValidationStats(string? userId = null)
        {
            var query = _context.ValidationSessions.AsQueryable();

            if (userId != null)
            {
                query = query.Where(s => s.UserId == userId);
            }

            var sessions = await query.ToListAsync();

            return new ValidationStats
            {
                TotalSessions = sessions.Count,
                CompletedSessions = sessions.Count(s => s.WasCompleted),
                TotalCandidatesPresented = sessions.Sum(s => s.CandidatesPresented),
                TotalApproved = sessions.Sum(s => s.FactsApproved),
                TotalRejected = sessions.Sum(s => s.FactsRejected),
                TotalEdited = sessions.Sum(s => s.FactsEdited),
                ApprovalRate = sessions.Sum(s => s.CandidatesPresented) > 0
                    ? (double)sessions.Sum(s => s.FactsApproved) / sessions.Sum(s => s.CandidatesPresented)
                    : 0.0
            };
        }
    }

    /// <summary>
    /// Decisão do usuário sobre um fato candidato.
    /// </summary>
    public class FactDecision
    {
        public Fact Fact { get; set; } = new Fact();
        public ValidationAction Action { get; set; }
        public double? Confidence { get; set; }
        public string? Reason { get; set; }

        // Para edições
        public string? EditedSubject { get; set; }
        public string? EditedRelation { get; set; }
        public string? EditedObject { get; set; }
    }

    public enum ValidationAction
    {
        APPROVE,
        REJECT,
        EDIT
    }

    /// <summary>
    /// Resultado de uma sessão de validação.
    /// </summary>
    public class ValidationResult
    {
        public int SessionId { get; set; }
        public DateTime ProcessedAt { get; set; }
        public List<Fact> Approved { get; set; } = new List<Fact>();
        public List<Fact> Rejected { get; set; } = new List<Fact>();
        public List<Fact> Edited { get; set; } = new List<Fact>();
        public List<string> Errors { get; set; } = new List<string>();

        public string GetSummary()
        {
            var summary = $"✅ Validação Concluída\n\n";
            summary += $"📊 Estatísticas:\n";
            summary += $"• Aprovados: {Approved.Count}\n";
            summary += $"• Rejeitados: {Rejected.Count}\n";
            summary += $"• Editados: {Edited.Count}\n";

            if (Errors.Any())
            {
                summary += $"\n⚠️ Erros: {Errors.Count}\n";
                foreach (var error in Errors)
                {
                    summary += $"• {error}\n";
                }
            }

            return summary;
        }
    }

    public class ValidationStats
    {
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int TotalCandidatesPresented { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalEdited { get; set; }
        public double ApprovalRate { get; set; }
    }
}