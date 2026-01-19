using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SmartAI.Services
{
    public class WebSearchService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        public WebSearchService()
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            }
        }

        /// <summary>
        /// Busca usando DuckDuckGo Instant Answer API (gratuita, sem necessidade de API key)
        /// </summary>
        public async Task<SearchResult> SearchDuckDuckGo(string query)
        {
            try
            {
                // Usar Uri.EscapeDataString ao invés de HttpUtility.UrlEncode
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"https://api.duckduckgo.com/?q={encodedQuery}&format=json&pretty=1";

                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var result = new SearchResult
                {
                    Query = query,
                    Success = true
                };

                // Abstract (resumo principal)
                var abstract_text = json["Abstract"]?.ToString();
                if (!string.IsNullOrEmpty(abstract_text))
                {
                    result.Summary = abstract_text;
                    result.Source = json["AbstractSource"]?.ToString() ?? "DuckDuckGo";
                    result.Url = json["AbstractURL"]?.ToString();
                }

                // Related Topics
                var relatedTopics = json["RelatedTopics"] as JArray;
                if (relatedTopics != null)
                {
                    result.RelatedInfo = new List<string>();
                    foreach (var topic in relatedTopics.Take(5))
                    {
                        var text = topic["Text"]?.ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            result.RelatedInfo.Add(text);
                        }
                    }
                }

                // Definition (se for uma definição)
                var definition = json["Definition"]?.ToString();
                if (!string.IsNullOrEmpty(definition))
                {
                    result.Definition = definition;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new SearchResult
                {
                    Query = query,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Busca na Wikipedia (API pública)
        /// </summary>
        public async Task<SearchResult> SearchWikipedia(string query)
        {
            try
            {
                // Usar Uri.EscapeDataString ao invés de HttpUtility.UrlEncode
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"https://pt.wikipedia.org/api/rest_v1/page/summary/{encodedQuery}";

                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                return new SearchResult
                {
                    Query = query,
                    Success = true,
                    Summary = json["extract"]?.ToString(),
                    Source = "Wikipedia",
                    Url = json["content_urls"]?["desktop"]?["page"]?.ToString(),
                    Title = json["title"]?.ToString()
                };
            }
            catch (Exception ex)
            {
                return new SearchResult
                {
                    Query = query,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Busca combinada - tenta múltiplas fontes
        /// </summary>
        public async Task<SearchResult> SmartSearch(string query)
        {
            // Tentar DuckDuckGo primeiro
            var ddgResult = await SearchDuckDuckGo(query);

            if (ddgResult.Success && !string.IsNullOrEmpty(ddgResult.Summary))
            {
                return ddgResult;
            }

            // Se DuckDuckGo não retornou nada, tentar Wikipedia
            var wikiResult = await SearchWikipedia(query);

            if (wikiResult.Success && !string.IsNullOrEmpty(wikiResult.Summary))
            {
                return wikiResult;
            }

            // Se ambos falharam, retornar resultado combinado
            return new SearchResult
            {
                Query = query,
                Success = false,
                Error = "Não foi possível encontrar informações relevantes nas fontes disponíveis.",
                Summary = "Tente reformular a busca ou usar termos mais específicos."
            };
        }

        /// <summary>
        /// Extrai fatos principais de um texto para facilitar aprendizado
        /// </summary>
        public List<string> ExtractFacts(string text)
        {
            var facts = new List<string>();

            if (string.IsNullOrEmpty(text))
                return facts;

            // Dividir em sentenças
            var sentences = Regex.Split(text, @"(?<=[\.!?])\s+");

            foreach (var sentence in sentences)
            {
                var trimmed = sentence.Trim();

                // Ignorar sentenças muito curtas ou muito longas
                if (trimmed.Length < 20 || trimmed.Length > 200)
                    continue;

                // Procurar padrões que indicam fatos
                if (ContainsFactPattern(trimmed))
                {
                    facts.Add(trimmed);
                }
            }

            return facts.Take(5).ToList(); // Limitar a 5 fatos principais
        }

        private bool ContainsFactPattern(string sentence)
        {
            var patterns = new[]
            {
                @"\b(é|são|foi|era|estava)\b",           // Verbo ser/estar
                @"\b(tem|possui|contém|inclui)\b",        // Verbos de posse
                @"\b(vive|habita|mora|reside)\b",         // Verbos de localização
                @"\b(nasceu|morreu|criou|descobriu)\b",   // Eventos históricos
                @"\b(conhecido|famoso|importante)\b"      // Características
            };

            return patterns.Any(pattern =>
                Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Converte resultados de busca em formato de ensino para a IA
        /// </summary>
        public List<string> ConvertToTeachingStatements(SearchResult result)
        {
            var statements = new List<string>();

            if (result == null || !result.Success)
                return statements;

            // Extrair fatos do resumo
            if (!string.IsNullOrEmpty(result.Summary))
            {
                var facts = ExtractFacts(result.Summary);
                statements.AddRange(facts);
            }

            // Adicionar definição se existir
            if (!string.IsNullOrEmpty(result.Definition))
            {
                statements.Add(result.Definition);
            }

            // Adicionar informações relacionadas
            if (result.RelatedInfo != null && result.RelatedInfo.Any())
            {
                statements.AddRange(result.RelatedInfo.Take(3));
            }

            return statements;
        }
    }

    public class SearchResult
    {
        public string Query { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Summary { get; set; }
        public string? Definition { get; set; }
        public string? Source { get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public List<string>? RelatedInfo { get; set; }
        public string? Error { get; set; }

        public override string ToString()
        {
            if (!Success)
            {
                return $"❌ Erro: {Error}";
            }

            var output = "";

            if (!string.IsNullOrEmpty(Title))
            {
                output += $"📖 {Title}\n\n";
            }

            if (!string.IsNullOrEmpty(Summary))
            {
                output += $"{Summary}\n\n";
            }

            if (!string.IsNullOrEmpty(Definition))
            {
                output += $"📝 Definição: {Definition}\n\n";
            }

            if (RelatedInfo != null && RelatedInfo.Any())
            {
                output += "🔗 Informações Relacionadas:\n";
                foreach (var info in RelatedInfo)
                {
                    output += $"• {info}\n";
                }
                output += "\n";
            }

            if (!string.IsNullOrEmpty(Source))
            {
                output += $"📚 Fonte: {Source}";
            }

            if (!string.IsNullOrEmpty(Url))
            {
                output += $"\n🔗 {Url}";
            }

            return output;
        }
    }
}