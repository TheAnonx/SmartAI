using SmartAI.Data;
using SmartAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartAI.Services
{
    /// <summary>
    /// Serviço de investigação web.
    /// Objetivo: Coletar POSSÍVEIS descrições da realidade, não verdades.
    /// NUNCA persiste automaticamente.
    /// </summary>
    public class InvestigationService
    {
        private readonly WebSearchService _webSearch;
        private readonly FactService _factService;

        public InvestigationService(AIContext context)
        {
            _webSearch = new WebSearchService();
            _factService = new FactService(context);
        }

        /// <summary>
        /// Investiga um tópico e retorna candidatos (NÃO persistidos).
        /// </summary>
        public async Task<InvestigationResult> Investigate(string query)
        {
            var result = new InvestigationResult
            {
                Query = query,
                StartedAt = DateTime.Now
            };

            // Buscar na web
            var searchResult = await _webSearch.SmartSearch(query);

            if (!searchResult.Success)
            {
                result.Success = false;
                result.Error = searchResult.Error;
                return result;
            }

            result.RawText = searchResult.Summary;
            result.SourceUrl = searchResult.Url;
            result.SourceName = searchResult.Source ?? "Unknown";

            // Extrair fatos (mas NÃO persistir)
            var extractedFacts = _webSearch.ExtractFacts(searchResult.Summary ?? "");

            foreach (var factText in extractedFacts)
            {
                var parsed = ParseFactText(factText);

                if (parsed != null)
                {
                    // Criar objeto Fact mas NÃO adicionar ao context
                    var candidateFact = new Fact
                    {
                        Subject = parsed.Subject,
                        Relation = parsed.Relation,
                        Object = parsed.Object,
                        Confidence = 0.0, // SEMPRE 0.0
                        Status = FactStatus.CANDIDATE,
                        CreatedAt = DateTime.Now
                    };

                    // Adicionar fonte
                    candidateFact.Sources.Add(new FactSource
                    {
                        Type = SourceType.WEB,
                        Identifier = result.SourceName,
                        URL = result.SourceUrl,
                        TrustWeight = 0.5,
                        RawContent = factText
                    });

                    result.CandidateFacts.Add(candidateFact);
                }
            }

            result.Success = true;
            result.CompletedAt = DateTime.Now;

            return result;
        }

        /// <summary>
        /// Parse de texto de fato em estrutura.
        /// </summary>
        private ParsedFact? ParseFactText(string text)
        {
            // Padrão: "X é Y"
            var isMatch = Regex.Match(
                text,
                @"^(.+?)\s+(é|são|foi|era)\s+(.+)$",
                RegexOptions.IgnoreCase);

            if (isMatch.Success)
            {
                return new ParsedFact
                {
                    Subject = CleanText(isMatch.Groups[1].Value),
                    Relation = CleanText(isMatch.Groups[2].Value),
                    Object = CleanText(isMatch.Groups[3].Value)
                };
            }

            // Padrão: "X tem Y"
            var hasMatch = Regex.Match(
                text,
                @"^(.+?)\s+(tem|possui|contém|inclui)\s+(.+)$",
                RegexOptions.IgnoreCase);

            if (hasMatch.Success)
            {
                return new ParsedFact
                {
                    Subject = CleanText(hasMatch.Groups[1].Value),
                    Relation = CleanText(hasMatch.Groups[2].Value),
                    Object = CleanText(hasMatch.Groups[3].Value)
                };
            }

            // Padrão: "X vive em Y"
            var locationMatch = Regex.Match(
                text,
                @"^(.+?)\s+(vive|mora|habita|reside)\s+(em|no|na)\s+(.+)$",
                RegexOptions.IgnoreCase);

            if (locationMatch.Success)
            {
                return new ParsedFact
                {
                    Subject = CleanText(locationMatch.Groups[1].Value),
                    Relation = "vive em",
                    Object = CleanText(locationMatch.Groups[4].Value)
                };
            }

            return null;
        }

        private string CleanText(string text)
        {
            text = text.Trim();
            text = Regex.Replace(text, @"\s+", " ");

            if (text.Length > 0)
            {
                text = char.ToUpper(text[0]) + text.Substring(1).ToLower();
            }

            return text;
        }
    }

    /// <summary>
    /// Resultado de uma investigação.
    /// NÃO contém fatos validados - apenas candidatos.
    /// </summary>
    public class InvestigationResult
    {
        public string Query { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? RawText { get; set; }
        public string? SourceUrl { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public List<Fact> CandidateFacts { get; set; } = new List<Fact>();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Classe auxiliar para parsing.
    /// </summary>
    internal class ParsedFact
    {
        public string Subject { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
    }
}