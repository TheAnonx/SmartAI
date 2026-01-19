using SmartAI.Models;
using Microsoft.EntityFrameworkCore;
using SmartAI.Data;
using SmartAI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartAI.AI
{
    public class EnhancedIntelligenceEngine
    {
        private readonly AIContext _context;
        private readonly WebSearchService _searchService;
        private readonly CodeAnalysisService _codeAnalysisService;

        private static readonly Dictionary<string, string[]> Synonyms = new()
        {
            { "é", new[] { "é", "ser", "está", "significa", "representa" } },
            { "tem", new[] { "tem", "possui", "contém", "inclui" } },
            { "código", new[] { "código", "script", "programa", "implementação" } },
            { "erro", new[] { "erro", "bug", "problema", "falha", "exceção" } },
            { "corrigir", new[] { "corrigir", "consertar", "resolver", "fix" } }
        };

        private static readonly string[] QuestionWords =
            { "o que", "qual", "quem", "onde", "quando", "como", "por que", "quanto" };

        public EnhancedIntelligenceEngine(AIContext context)
        {
            _context = context;
            _searchService = new WebSearchService();
            _codeAnalysisService = new CodeAnalysisService();
        }

        public async Task<string> ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Por favor, envie uma mensagem válida.";

            input = input.Trim();
            await SaveConversation(input, "user");

            // Detectar solicitações relacionadas a código
            if (IsCodeRequest(input))
            {
                var response = await ProcessCodeRequest(input);
                await SaveConversation(response, "assistant");
                return response;
            }

            // Detectar perguntas que precisam de busca web
            if (NeedsWebSearch(input))
            {
                var response = await ProcessWithWebSearch(input);
                await SaveConversation(response, "assistant");
                return response;
            }

            // Processar como antes
            if (IsQuestion(input))
            {
                var answer = await ProcessQuestion(input);
                await SaveConversation(answer, "assistant");
                return answer;
            }
            else if (IsTeachingStatement(input))
            {
                var response = await ProcessTeaching(input);
                await SaveConversation(response, "assistant");
                return response;
            }
            else
            {
                var response = await ProcessGeneralStatement(input);
                await SaveConversation(response, "assistant");
                return response;
            }
        }

        private bool IsCodeRequest(string input)
        {
            var codePatterns = new[]
            {
                @"\b(analise|analyze|revise|review|corrija|fix|melhore|improve)\b.*\b(código|code|script)\b",
                @"\b(bug|erro|error|exception|problema)\b.*\b(código|code)\b",
                @"\b(como|how).*(programar|program|code|implementar|implement)\b",
                @"\b(sugest[aã]o|suggestion|recomenda[çc][aã]o)\b.*\b(código|code)\b",
                @"```[\s\S]*```" // Detectar blocos de código
            };

            return codePatterns.Any(pattern =>
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private bool NeedsWebSearch(string input)
        {
            var searchIndicators = new[]
            {
                @"\b(pesquis[ae]|search|busque|procure)\b",
                @"\b(o que [eé]|what is|explique|explain)\b.*\b(na internet|online|web)\b",
                @"\b(como funciona|how does|como fazer|how to)\b",
                @"\b(tutorial|documenta[çc][aã]o|documentation|guia|guide)\b",
                @"\b(aprenda|learn|ensine-me|teach me)\b.*\b(sobre|about)\b"
            };

            return searchIndicators.Any(pattern =>
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private async Task<string> ProcessCodeRequest(string input)
        {
            // Extrair código se presente
            var codeBlock = ExtractCodeBlock(input);

            if (!string.IsNullOrEmpty(codeBlock))
            {
                // Analisar código
                var analysis = _codeAnalysisService.AnalyzeCode(codeBlock);

                // Buscar conhecimento sobre linguagem/framework
                var language = _codeAnalysisService.DetectLanguage(codeBlock);
                var webKnowledge = await SearchCodeKnowledge(language, input);

                return FormatCodeResponse(analysis, webKnowledge);
            }

            // Se não há código, buscar conhecimento na web
            var searchQuery = ExtractCodeSearchQuery(input);
            var searchResult = await _searchService.SmartSearch(searchQuery);

            if (searchResult.Success)
            {
                // Armazenar conhecimento obtido
                await LearnFromWebContent(searchResult);

                return $"🔍 Pesquisei sobre: {searchQuery}\n\n" +
                       $"{searchResult.Summary}\n\n" +
                       $"💡 Apliquei esse conhecimento à minha base de dados!\n\n" +
                       $"Fonte: {searchResult.Source}";
            }

            return "Não consegui encontrar informações relevantes. Pode reformular?";
        }

        private async Task<string> ProcessWithWebSearch(string input)
        {
            var searchTerm = ExtractMainTerm(input);
            var result = await _searchService.SmartSearch(searchTerm);

            if (result.Success && !string.IsNullOrEmpty(result.Summary))
            {
                // Aprender automaticamente com conteúdo da web
                await LearnFromWebContent(result);

                return $"🌐 Pesquisei e aprendi sobre: {searchTerm}\n\n" +
                       $"{result.Summary}\n\n" +
                       $"✅ Conhecimento adicionado à minha base!\n\n" +
                       $"📚 Fonte: {result.Source}\n" +
                       $"🔗 {result.Url}";
            }

            return $"Não encontrei informações sobre '{searchTerm}'.";
        }

        private async Task LearnFromWebContent(SearchResult searchResult)
        {
            var facts = _searchService.ExtractFacts(searchResult.Summary ?? "");

            foreach (var fact in facts)
            {
                try
                {
                    // Processar cada fato como ensinamento
                    await ProcessTeachingFromWeb(fact, searchResult.Source ?? "Web");
                }
                catch
                {
                    // Ignorar erros individuais
                }
            }
        }

        private async Task ProcessTeachingFromWeb(string fact, string source)
        {
            // Padrão: "X é Y"
            var isMatch = Regex.Match(fact, @"(.+?)\s+(é|são|foi|era)\s+(.+)", RegexOptions.IgnoreCase);
            if (isMatch.Success)
            {
                var subject = CleanText(isMatch.Groups[1].Value);
                var predicate = CleanText(isMatch.Groups[3].Value);
                await LearnIsRelation(subject, predicate, source);
                return;
            }

            // Padrão: "X tem Y"
            var hasMatch = Regex.Match(fact, @"(.+?)\s+(tem|possui|contém|inclui)\s+(.+)", RegexOptions.IgnoreCase);
            if (hasMatch.Success)
            {
                var subject = CleanText(hasMatch.Groups[1].Value);
                var property = CleanText(hasMatch.Groups[3].Value);
                await LearnHasRelation(subject, property, source);
            }
        }

        private string ExtractCodeBlock(string input)
        {
            var match = Regex.Match(input, @"```[\w]*\s*([\s\S]*?)\s*```");
            return match.Success ? match.Groups[1].Value : "";
        }

        private string ExtractCodeSearchQuery(string input)
        {
            // Remover palavras comuns e focar no essencial
            var cleaned = Regex.Replace(input, @"\b(me|ajude|help|por favor|please|como|how)\b", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }

        private string FormatCodeResponse(CodeAnalysis analysis, string webKnowledge)
        {
            var response = "🔍 **Análise do Código**\n\n";

            response += $"**Linguagem**: {analysis.Language}\n";
            response += $"**Complexidade**: {analysis.Complexity}\n\n";

            if (analysis.Issues.Any())
            {
                response += "⚠️ **Problemas Encontrados**:\n";
                foreach (var issue in analysis.Issues)
                {
                    response += $"• {issue}\n";
                }
                response += "\n";
            }

            if (analysis.Suggestions.Any())
            {
                response += "💡 **Sugestões de Melhoria**:\n";
                foreach (var suggestion in analysis.Suggestions)
                {
                    response += $"• {suggestion}\n";
                }
                response += "\n";
            }

            if (!string.IsNullOrEmpty(webKnowledge))
            {
                response += "📚 **Conhecimento da Web**:\n";
                response += webKnowledge;
            }

            return response;
        }

        private async Task<string> SearchCodeKnowledge(string language, string context)
        {
            var query = $"{language} best practices {context}";
            var result = await _searchService.SmartSearch(query);

            return result.Success ? result.Summary ?? "" : "";
        }

        // Métodos existentes adaptados
        private bool IsQuestion(string input)
        {
            input = input.ToLower();
            return input.EndsWith("?") || QuestionWords.Any(qw => input.StartsWith(qw));
        }

        private bool IsTeachingStatement(string input)
        {
            var teachingPatterns = new[]
            {
                @"(.+?)\s+(é|são)\s+(.+)",
                @"(.+?)\s+(tem|possui|contém)\s+(.+)",
                @"(.+?)\s+(vive|mora|habita)\s+(.+)",
                @"(.+?)\s+(faz|produz|cria)\s+(.+)"
            };

            return teachingPatterns.Any(pattern =>
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private async Task<string> ProcessQuestion(string input)
        {
            var term = ExtractMainTerm(input);
            if (string.IsNullOrEmpty(term))
                return "Não consegui entender sua pergunta. Pode reformular?";

            var concept = await FindConcept(term);
            if (concept != null)
                return await BuildConceptAnswer(concept);

            var instance = await FindInstance(term);
            if (instance != null)
                return await BuildInstanceAnswer(instance);

            return $"❌ Não sei sobre '{term}'.\n💡 Posso buscar na web para você?";
        }

        private async Task<string> ProcessTeaching(string input)
        {
            var isMatch = Regex.Match(input, @"(.+?)\s+(é|são)\s+(.+)", RegexOptions.IgnoreCase);
            if (isMatch.Success)
            {
                var subject = CleanText(isMatch.Groups[1].Value);
                var predicate = CleanText(isMatch.Groups[3].Value);
                return await LearnIsRelation(subject, predicate);
            }

            return "Entendi que quer me ensinar algo. Use: 'X é Y', 'X tem Y'.";
        }

        private async Task<string> ProcessGeneralStatement(string input)
        {
            return "Posso ajudar com:\n" +
                   "• Análise de código\n" +
                   "• Busca de informações\n" +
                   "• Aprendizado de conceitos\n" +
                   "Que tal perguntar algo?";
        }

        private async Task<string> LearnIsRelation(string subject, string predicate, string source = "user")
        {
            var parentConcept = await GetOrCreateConcept(predicate);
            var instance = new Instance
            {
                Name = subject,
                ConceptId = parentConcept.Id,
                CreatedAt = DateTime.Now,
                Confidence = source == "user" ? 1.0 : 0.8
            };

            _context.Instances.Add(instance);
            await _context.SaveChangesAsync();

            return $"✅ Aprendi! '{subject}' é '{predicate}'.\n(Fonte: {source})";
        }

        private async Task<string> LearnHasRelation(string subject, string property, string source = "user")
        {
            var instance = await GetOrCreateInstance(subject);
            var instanceProp = new InstanceProperty
            {
                InstanceId = instance.Id,
                PropertyName = "tem",
                PropertyValue = property,
                CreatedAt = DateTime.Now
            };

            _context.InstanceProperties.Add(instanceProp);
            await _context.SaveChangesAsync();

            return $"✅ Aprendi! '{subject}' tem '{property}'.\n(Fonte: {source})";
        }

        private async Task<string> BuildConceptAnswer(Concept concept)
        {
            var answer = $"📖 {concept.Name}:\n";

            if (concept.ParentConceptId.HasValue)
            {
                var parent = await _context.Concepts.FindAsync(concept.ParentConceptId);
                answer += $"• É um tipo de: {parent?.Name}\n";
            }

            var instances = await _context.Instances
                .Where(i => i.ConceptId == concept.Id)
                .Take(5)
                .ToListAsync();

            if (instances.Any())
                answer += $"• Exemplos: {string.Join(", ", instances.Select(i => i.Name))}\n";

            return answer;
        }

        private async Task<string> BuildInstanceAnswer(Instance instance)
        {
            var answer = $"📦 {instance.Name}:\n";

            var concept = await _context.Concepts.FindAsync(instance.ConceptId);
            if (concept != null)
                answer += $"• É: {concept.Name}\n";

            var properties = await _context.InstanceProperties
                .Where(p => p.InstanceId == instance.Id)
                .ToListAsync();

            if (properties.Any())
            {
                answer += "• Características:\n";
                foreach (var prop in properties)
                    answer += $"  - {prop.PropertyName}: {prop.PropertyValue}\n";
            }

            return answer;
        }

        private string ExtractMainTerm(string question)
        {
            question = question.ToLower().TrimEnd('?', '.', '!');
            foreach (var qw in QuestionWords)
            {
                if (question.StartsWith(qw))
                {
                    question = question.Substring(qw.Length).Trim();
                    break;
                }
            }
            question = Regex.Replace(question, @"\b(é|são|um|uma|o|a)\b", "", RegexOptions.IgnoreCase);
            return CleanText(question);
        }

        private async Task<Concept?> FindConcept(string name)
        {
            return await _context.Concepts
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        private async Task<Instance?> FindInstance(string name)
        {
            return await _context.Instances
                .Include(i => i.Concept)
                .FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());
        }

        private async Task<Concept> GetOrCreateConcept(string name)
        {
            name = CleanText(name);
            var concept = await FindConcept(name);

            if (concept == null)
            {
                concept = new Concept { Name = name, CreatedAt = DateTime.Now };
                _context.Concepts.Add(concept);
                await _context.SaveChangesAsync();
            }

            return concept;
        }

        private async Task<Instance> GetOrCreateInstance(string name)
        {
            name = CleanText(name);
            var instance = await FindInstance(name);

            if (instance == null)
            {
                var genericConcept = await GetOrCreateConcept("Entidade");
                instance = new Instance
                {
                    Name = name,
                    ConceptId = genericConcept.Id,
                    CreatedAt = DateTime.Now,
                    Confidence = 0.5
                };
                _context.Instances.Add(instance);
                await _context.SaveChangesAsync();
            }

            return instance;
        }

        private string CleanText(string text)
        {
            text = text.Trim();
            text = Regex.Replace(text, @"\s+", " ");
            if (text.Length > 0)
                text = char.ToUpper(text[0]) + text.Substring(1).ToLower();
            return text;
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

        public async Task<Dictionary<string, int>> GetStatistics()
        {
            return new Dictionary<string, int>
            {
                { "Concepts", await _context.Concepts.CountAsync() },
                { "Instances", await _context.Instances.CountAsync() },
                { "ConceptProperties", await _context.ConceptProperties.CountAsync() },
                { "InstanceProperties", await _context.InstanceProperties.CountAsync() },
                { "Conversations", await _context.Conversations.CountAsync() }
            };
        }

        public async Task<List<string>> GetRecentLearning(int count = 5)
        {
            var recentInstances = await _context.Instances
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .Include(i => i.Concept)
                .ToListAsync();

            return recentInstances
                .Select(i => $"{i.Name} → {i.Concept?.Name}")
                .ToList();
        }
    }
}