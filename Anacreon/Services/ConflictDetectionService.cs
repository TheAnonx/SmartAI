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
    /// Detecta e gerencia conflitos entre fatos.
    /// O sistema NÃO escolhe automaticamente - apresenta ao usuário.
    /// </summary>
    public class ConflictDetectionService
    {
        private readonly AIContext _context;
        private const double CONFLICT_THRESHOLD = 0.15; // Diferença mínima para considerar conflito

        public ConflictDetectionService(AIContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Detecta todos os conflitos não resolvidos.
        /// </summary>
        public async Task<List<FactConflict>> DetectConflicts()
        {
            var conflicts = new List<FactConflict>();

            // Buscar fatos validados agrupados por Subject + Relation
            var factGroups = await _context.Facts
                .Where(f => f.Status == FactStatus.VALIDATED)
                .Include(f => f.Sources)
                .GroupBy(f => new { f.Subject, f.Relation })
                .Where(g => g.Count() > 1) // Mais de um fato para mesma relação
                .ToListAsync();

            foreach (var group in factGroups)
            {
                var facts = group.OrderByDescending(f => f.Confidence).ToList();

                // Comparar cada par
                for (int i = 0; i < facts.Count - 1; i++)
                {
                    for (int j = i + 1; j < facts.Count; j++)
                    {
                        var factA = facts[i];
                        var factB = facts[j];

                        // Se os objetos são diferentes, há conflito
                        if (factA.Object.ToLower() != factB.Object.ToLower())
                        {
                            // Verificar se conflito já foi registrado
                            var existingConflict = await _context.FactConflicts
                                .AnyAsync(c =>
                                    (c.FactAId == factA.Id && c.FactBId == factB.Id) ||
                                    (c.FactAId == factB.Id && c.FactBId == factA.Id));

                            if (!existingConflict)
                            {
                                var conflict = new FactConflict
                                {
                                    Subject = group.Key.Subject,
                                    Relation = group.Key.Relation,
                                    FactAId = factA.Id,
                                    FactA = factA,
                                    FactBId = factB.Id,
                                    FactB = factB,
                                    ConfidenceDifference = Math.Abs(
                                        factA.Confidence - factB.Confidence),
                                    DetectedAt = DateTime.Now,
                                    IsResolved = false
                                };

                                _context.FactConflicts.Add(conflict);
                                conflicts.Add(conflict);
                            }
                        }
                    }
                }
            }

            if (conflicts.Any())
            {
                await _context.SaveChangesAsync();
            }

            return conflicts;
        }

        /// <summary>
        /// Obtém conflitos não resolvidos.
        /// </summary>
        public async Task<List<FactConflict>> GetUnresolvedConflicts()
        {
            return await _context.FactConflicts
                .Where(c => !c.IsResolved)
                .Include(c => c.FactA)
                    .ThenInclude(f => f.Sources)
                .Include(c => c.FactB)
                    .ThenInclude(f => f.Sources)
                .OrderByDescending(c => c.DetectedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Resolve um conflito baseado na decisão do usuário.
        /// </summary>
        public async Task ResolveConflict(
            int conflictId,
            ConflictResolution resolution,
            string resolvedBy,
            string? notes = null)
        {
            var conflict = await _context.FactConflicts
                .Include(c => c.FactA)
                .Include(c => c.FactB)
                .FirstOrDefaultAsync(c => c.Id == conflictId);

            if (conflict == null)
                throw new InvalidOperationException($"Conflict {conflictId} not found");

            if (conflict.IsResolved)
                throw new InvalidOperationException("Conflict already resolved");

            var factService = new FactService(_context);

            switch (resolution)
            {
                case ConflictResolution.KEEP_FACT_A:
                    // Deprecar Fact B
                    await factService.DeprecateFact(
                        conflict.FactBId,
                        resolvedBy,
                        $"Conflito resolvido - Fact {conflict.FactAId} mantido");
                    break;

                case ConflictResolution.KEEP_FACT_B:
                    // Deprecar Fact A
                    await factService.DeprecateFact(
                        conflict.FactAId,
                        resolvedBy,
                        $"Conflito resolvido - Fact {conflict.FactBId} mantido");
                    break;

                case ConflictResolution.KEEP_BOTH:
                    // Manter ambos - pode ser contextual
                    // Reduzir confiança de ambos
                    await factService.UpdateConfidence(
                        conflict.FactAId,
                        conflict.FactA.Confidence * 0.85,
                        resolvedBy,
                        "Conflito detectado - ambos mantidos com confiança reduzida");

                    await factService.UpdateConfidence(
                        conflict.FactBId,
                        conflict.FactB.Confidence * 0.85,
                        resolvedBy,
                        "Conflito detectado - ambos mantidos com confiança reduzida");
                    break;

                case ConflictResolution.DEPRECATE_BOTH:
                    // Deprecar ambos
                    await factService.DeprecateFact(
                        conflict.FactAId,
                        resolvedBy,
                        "Conflito resolvido - ambos fatos deprecados");

                    await factService.DeprecateFact(
                        conflict.FactBId,
                        resolvedBy,
                        "Conflito resolvido - ambos fatos deprecados");
                    break;

                case ConflictResolution.CREATE_NEW:
                    // Usuário criará novo fato manualmente
                    // Deprecar ambos
                    await factService.DeprecateFact(
                        conflict.FactAId,
                        resolvedBy,
                        "Conflito resolvido - novo fato será criado");

                    await factService.DeprecateFact(
                        conflict.FactBId,
                        resolvedBy,
                        "Conflito resolvido - novo fato será criado");
                    break;
            }

            // Marcar conflito como resolvido
            conflict.IsResolved = true;
            conflict.ResolvedAt = DateTime.Now;
            conflict.Resolution = resolution;
            conflict.ResolutionNotes = notes;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Formata conflito para apresentação ao usuário.
        /// </summary>
        public string FormatConflictForUser(FactConflict conflict)
        {
            var output = $"⚠️ CONFLITO DETECTADO\n\n";
            output += $"Assunto: {conflict.Subject}\n";
            output += $"Relação: {conflict.Relation}\n\n";

            output += $"═══════════════════════════════════\n";
            output += $"VERSÃO A (ID: {conflict.FactAId})\n";
            output += $"═══════════════════════════════════\n";
            output += $"Valor: {conflict.FactA.Object}\n";
            output += $"Confiança: {conflict.FactA.Confidence:P2}\n";
            output += $"Fonte: {conflict.FactA.Sources.First().Type}\n";
            output += $"Validado em: {conflict.FactA.ValidatedAt:dd/MM/yyyy HH:mm}\n";
            output += $"Aprovado por: {conflict.FactA.ApprovedBy}\n\n";

            output += $"═══════════════════════════════════\n";
            output += $"VERSÃO B (ID: {conflict.FactBId})\n";
            output += $"═══════════════════════════════════\n";
            output += $"Valor: {conflict.FactB.Object}\n";
            output += $"Confiança: {conflict.FactB.Confidence:P2}\n";
            output += $"Fonte: {conflict.FactB.Sources.First().Type}\n";
            output += $"Validado em: {conflict.FactB.ValidatedAt:dd/MM/yyyy HH:mm}\n";
            output += $"Aprovado por: {conflict.FactB.ApprovedBy}\n\n";

            output += $"Diferença de confiança: {conflict.ConfidenceDifference:P2}\n\n";

            output += $"OPÇÕES DE RESOLUÇÃO:\n";
            output += $"1. Manter apenas Versão A\n";
            output += $"2. Manter apenas Versão B\n";
            output += $"3. Manter ambas (contextos diferentes)\n";
            output += $"4. Deprecar ambas\n";
            output += $"5. Criar novo fato\n";

            return output;
        }
    }
}