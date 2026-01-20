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
    /// Serviço para gerenciar Facts seguindo princípios epistêmicos.
    /// </summary>
    public class FactService
    {
        private readonly AIContext _context;
        private const double ASSERTION_THRESHOLD = 0.85;
        private const double MAX_CONFIDENCE = 0.99999;

        public FactService(AIContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria um fato candidato (NÃO validado).
        /// </summary>
        public async Task<Fact> CreateCandidateFact(
            string subject,
            string relation,
            string obj,
            SourceType sourceType,
            string sourceIdentifier,
            string? sourceUrl = null)
        {
            var fact = new Fact
            {
                Subject = subject,
                Relation = relation,
                Object = obj,
                Confidence = 0.0, // SEMPRE 0.0 para candidatos
                Status = FactStatus.CANDIDATE,
                CreatedAt = DateTime.Now,
                Version = 1
            };

            _context.Facts.Add(fact);
            await _context.SaveChangesAsync();

            // Adicionar fonte
            var source = new FactSource
            {
                FactId = fact.Id,
                Type = sourceType,
                Identifier = sourceIdentifier,
                URL = sourceUrl,
                TrustWeight = GetDefaultTrustWeight(sourceType),
                CollectedAt = DateTime.Now
            };

            _context.FactSources.Add(source);

            // Criar histórico
            var history = new FactHistory
            {
                FactId = fact.Id,
                Version = 1,
                NewSubject = subject,
                NewRelation = relation,
                NewObject = obj,
                NewConfidence = 0.0,
                NewStatus = FactStatus.CANDIDATE,
                ChangedBy = "system",
                ChangedAt = DateTime.Now,
                Reason = "Candidate fact created",
                ChangeType = ChangeType.CREATED
            };

            _context.FactHistory.Add(history);
            await _context.SaveChangesAsync();

            return fact;
        }

        /// <summary>
        /// Valida um fato candidato (requer aprovação humana).
        /// </summary>
        public async Task<Fact> ValidateFact(
            int factId,
            string approvedBy,
            double confidence = 0.90)
        {
            var fact = await _context.Facts
                .Include(f => f.Sources)
                .FirstOrDefaultAsync(f => f.Id == factId);

            if (fact == null)
                throw new InvalidOperationException($"Fact {factId} not found");

            if (fact.Status != FactStatus.CANDIDATE)
                throw new InvalidOperationException(
                    $"Only CANDIDATE facts can be validated. Current status: {fact.Status}");

            // Validar confiança
            if (confidence >= 1.0)
                throw new InvalidOperationException(
                    $"Confidence cannot be 1.0. Maximum: {MAX_CONFIDENCE}");

            if (confidence < 0.0)
                throw new InvalidOperationException("Confidence cannot be negative");

            // Criar histórico ANTES de modificar
            var history = new FactHistory
            {
                FactId = fact.Id,
                Version = fact.Version,
                PreviousSubject = fact.Subject,
                PreviousRelation = fact.Relation,
                PreviousObject = fact.Object,
                PreviousConfidence = fact.Confidence,
                PreviousStatus = fact.Status,
                NewSubject = fact.Subject,
                NewRelation = fact.Relation,
                NewObject = fact.Object,
                NewConfidence = confidence,
                NewStatus = FactStatus.VALIDATED,
                ChangedBy = approvedBy,
                ChangedAt = DateTime.Now,
                Reason = "Fact validated by user",
                ChangeType = ChangeType.VALIDATED
            };

            _context.FactHistory.Add(history);

            // Atualizar fato
            fact.Status = FactStatus.VALIDATED;
            fact.Confidence = confidence;
            fact.ApprovedBy = approvedBy;
            fact.ValidatedAt = DateTime.Now;
            fact.Version++;

            await _context.SaveChangesAsync();

            return fact;
        }

        /// <summary>
        /// Rejeita um fato candidato.
        /// </summary>
        public async Task<Fact> RejectFact(int factId, string rejectedBy, string? reason = null)
        {
            var fact = await _context.Facts.FindAsync(factId);

            if (fact == null)
                throw new InvalidOperationException($"Fact {factId} not found");

            // Criar histórico
            var history = new FactHistory
            {
                FactId = fact.Id,
                Version = fact.Version,
                PreviousStatus = fact.Status,
                NewStatus = FactStatus.REJECTED,
                ChangedBy = rejectedBy,
                ChangedAt = DateTime.Now,
                Reason = reason ?? "Fact rejected by user",
                ChangeType = ChangeType.REJECTED
            };

            _context.FactHistory.Add(history);

            // Atualizar fato
            fact.Status = FactStatus.REJECTED;
            fact.Version++;

            await _context.SaveChangesAsync();

            return fact;
        }

        /// <summary>
        /// Depreca um fato (NÃO deleta, apenas marca como obsoleto).
        /// </summary>
        public async Task<Fact> DeprecateFact(
            int factId,
            string deprecatedBy,
            string reason)
        {
            var fact = await _context.Facts.FindAsync(factId);

            if (fact == null)
                throw new InvalidOperationException($"Fact {factId} not found");

            // Criar histórico
            var history = new FactHistory
            {
                FactId = fact.Id,
                Version = fact.Version,
                PreviousStatus = fact.Status,
                PreviousConfidence = fact.Confidence,
                NewStatus = FactStatus.DEPRECATED,
                NewConfidence = 0.0,
                ChangedBy = deprecatedBy,
                ChangedAt = DateTime.Now,
                Reason = reason,
                ChangeType = ChangeType.DEPRECATED
            };

            _context.FactHistory.Add(history);

            // Atualizar fato
            fact.Status = FactStatus.DEPRECATED;
            fact.Confidence = 0.0;
            fact.DeprecatedAt = DateTime.Now;
            fact.DeprecationReason = reason;
            fact.Version++;

            await _context.SaveChangesAsync();

            return fact;
        }

        /// <summary>
        /// Busca fatos validados sobre um subject.
        /// </summary>
        public async Task<List<Fact>> FindValidatedFacts(string subject)
        {
            return await _context.Facts
                .Where(f => f.Subject.ToLower() == subject.ToLower()
                         && f.Status == FactStatus.VALIDATED
                         && f.Confidence >= ASSERTION_THRESHOLD)
                .Include(f => f.Sources)
                .OrderByDescending(f => f.Confidence)
                .ToListAsync();
        }

        /// <summary>
        /// Busca fatos candidatos (para validação).
        /// </summary>
        public async Task<List<Fact>> GetCandidateFacts()
        {
            return await _context.Facts
                .Where(f => f.Status == FactStatus.CANDIDATE)
                .Include(f => f.Sources)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Verifica se há conhecimento confiável sobre um subject.
        /// </summary>
        public async Task<bool> HasReliableKnowledge(string subject)
        {
            return await _context.Facts
                .AnyAsync(f => f.Subject.ToLower() == subject.ToLower()
                            && f.Status == FactStatus.VALIDATED
                            && f.Confidence >= ASSERTION_THRESHOLD);
        }

        /// <summary>
        /// Atualiza a confiança de um fato.
        /// </summary>
        public async Task UpdateConfidence(
            int factId,
            double newConfidence,
            string updatedBy,
            string reason)
        {
            var fact = await _context.Facts.FindAsync(factId);

            if (fact == null)
                throw new InvalidOperationException($"Fact {factId} not found");

            if (newConfidence >= 1.0 || newConfidence < 0.0)
                throw new InvalidOperationException(
                    $"Invalid confidence: {newConfidence}. Must be 0.0 < x < 1.0");

            // Criar histórico
            var history = new FactHistory
            {
                FactId = fact.Id,
                Version = fact.Version,
                PreviousConfidence = fact.Confidence,
                NewConfidence = newConfidence,
                ChangedBy = updatedBy,
                ChangedAt = DateTime.Now,
                Reason = reason,
                ChangeType = ChangeType.CONFIDENCE_UPDATED
            };

            _context.FactHistory.Add(history);

            // Atualizar
            fact.Confidence = newConfidence;
            fact.Version++;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Retorna peso de confiança padrão por tipo de fonte.
        /// APENAS INFORMATIVO - nunca é decisivo.
        /// </summary>
        private double GetDefaultTrustWeight(SourceType type)
        {
            return type switch
            {
                SourceType.USER => 0.95,
                SourceType.ACADEMIC => 0.85,
                SourceType.DOCUMENTATION => 0.80,
                SourceType.WEB => 0.50,
                SourceType.FORUM => 0.40,
                SourceType.CODEBASE => 0.60,
                _ => 0.50
            };
        }

        /// <summary>
        /// Obtém histórico completo de um fato.
        /// </summary>
        public async Task<List<FactHistory>> GetFactHistory(int factId)
        {
            return await _context.FactHistory
                .Where(h => h.FactId == factId)
                .OrderBy(h => h.Version)
                .ToListAsync();
        }

        /// <summary>
        /// Edita o conteúdo de um fato candidato.
        /// </summary>
        public async Task<Fact> EditCandidateFact(
            int factId,
            string? newSubject,
            string? newRelation,
            string? newObject,
            string editedBy)
        {
            var fact = await _context.Facts.FindAsync(factId);

            if (fact == null)
                throw new InvalidOperationException($"Fact {factId} not found");

            if (fact.Status != FactStatus.CANDIDATE)
                throw new InvalidOperationException(
                    "Only CANDIDATE facts can be edited before validation");

            // Criar histórico
            var history = new FactHistory
            {
                FactId = fact.Id,
                Version = fact.Version,
                PreviousSubject = fact.Subject,
                PreviousRelation = fact.Relation,
                PreviousObject = fact.Object,
                NewSubject = newSubject ?? fact.Subject,
                NewRelation = newRelation ?? fact.Relation,
                NewObject = newObject ?? fact.Object,
                ChangedBy = editedBy,
                ChangedAt = DateTime.Now,
                Reason = "Fact edited before validation",
                ChangeType = ChangeType.CONTENT_EDITED
            };

            _context.FactHistory.Add(history);

            // Atualizar
            if (newSubject != null) fact.Subject = newSubject;
            if (newRelation != null) fact.Relation = newRelation;
            if (newObject != null) fact.Object = newObject;
            fact.Version++;

            await _context.SaveChangesAsync();

            return fact;
        }
    }
}