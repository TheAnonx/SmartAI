using SmartAI.Models;
using Microsoft.EntityFrameworkCore;
using SmartAI.Data;
using SmartAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartAI.AI
{
    public class IntelligenceEngine
    {
        private readonly AIContext _context;
        private static readonly Dictionary<string, string[]> Synonyms = new()
        {
            { "é", new[] { "é", "ser", "está", "significa", "representa" } },
            { "tem", new[] { "tem", "possui", "contém", "inclui" } },
            { "vive", new[] { "vive", "habita", "mora", "reside" } },
            { "faz", new[] { "faz", "realiza", "executa", "produz" } },
            { "usa", new[] { "usa", "utiliza", "emprega", "aplica" } }
        };

        private static readonly string[] QuestionWords =
            { "o que", "qual", "quem", "onde", "quando", "como", "por que", "quanto" };

        public IntelligenceEngine(AIContext context)
        {
            _context = context;
        }

        public async Task<string> ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Por favor, envie uma mensagem válida.";

            input = input.Trim();

            // Salvar conversa
            await SaveConversation(input, "user");

            // Detectar tipo de entrada
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

        private bool IsQuestion(string input)
        {
            input = input.ToLower();
            return input.EndsWith("?") ||
                   QuestionWords.Any(qw => input.StartsWith(qw));
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
            // Extrair termo principal da pergunta
            var term = ExtractMainTerm(input);

            if (string.IsNullOrEmpty(term))
                return "Não consegui entender sua pergunta. Pode reformular?";

            // Buscar na ontologia
            var concept = await FindConcept(term);
            if (concept != null)
            {
                return await BuildConceptAnswer(concept);
            }

            var instance = await FindInstance(term);
            if (instance != null)
            {
                return await BuildInstanceAnswer(instance);
            }

            // Buscar termos relacionados (sinônimos, variações)
            var relatedResults = await SearchRelatedTerms(term);
            if (relatedResults.Any())
            {
                return $"Não encontrei exatamente '{term}', mas encontrei:\n{string.Join("\n", relatedResults)}";
            }

            return $"❌ Não sei nada sobre '{term}'.\n\n" +
                   $"💡 Sugestão: Me ensine dizendo: '{term} é um/uma ___'";
        }

        private async Task<string> ProcessTeaching(string input)
        {
            // Padrão: "X é Y"
            var isMatch = Regex.Match(input, @"(.+?)\s+(é|são)\s+(.+)", RegexOptions.IgnoreCase);
            if (isMatch.Success)
            {
                var subject = CleanText(isMatch.Groups[1].Value);
                var predicate = CleanText(isMatch.Groups[3].Value);

                return await LearnIsRelation(subject, predicate);
            }

            // Padrão: "X tem Y"
            var hasMatch = Regex.Match(input, @"(.+?)\s+(tem|possui|contém)\s+(.+)", RegexOptions.IgnoreCase);
            if (hasMatch.Success)
            {
                var subject = CleanText(hasMatch.Groups[1].Value);
                var property = CleanText(hasMatch.Groups[3].Value);

                return await LearnHasRelation(subject, property);
            }

            // Padrão: "X vive em Y"
            var livesMatch = Regex.Match(input, @"(.+?)\s+(vive|mora|habita)\s+(em|no|na)?\s*(.+)", RegexOptions.IgnoreCase);
            if (livesMatch.Success)
            {
                var subject = CleanText(livesMatch.Groups[1].Value);
                var location = CleanText(livesMatch.Groups[4].Value);

                return await LearnPropertyRelation(subject, "habitat", location);
            }

            return "Entendi que você quer me ensinar algo, mas não consegui identificar o padrão. " +
                   "Tente usar: 'X é Y', 'X tem Y' ou 'X vive em Y'.";
        }

        private async Task<string> ProcessGeneralStatement(string input)
        {
            // Tentar extrair conhecimento implícito
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 3)
            {
                return "Entendi. Pode me fazer uma pergunta ou me ensinar algo?";
            }

            return "Interessante! Para eu aprender melhor, pode reformular como:\n" +
                   "• 'X é Y' (para definições)\n" +
                   "• 'X tem Y' (para propriedades)\n" +
                   "• 'O que é X?' (para perguntas)";
        }

        private async Task<string> LearnIsRelation(string subject, string predicate)
        {
            // Criar ou buscar conceito pai
            var parentConcept = await GetOrCreateConcept(predicate);

            // Criar ou buscar instância/conceito filho
            var childConcept = await FindConcept(subject);
            if (childConcept != null)
            {
                // Atualizar hierarquia de conceitos
                childConcept.ParentConceptId = parentConcept.Id;
                await _context.SaveChangesAsync();

                return $"✅ Aprendi! '{subject}' é um tipo de '{predicate}'.\n" +
                       $"Agora sei a hierarquia: {subject} → {predicate}";
            }

            // Criar nova instância
            var instance = new Instance
            {
                Name = subject,
                ConceptId = parentConcept.Id,
                CreatedAt = DateTime.Now,
                Confidence = 1.0
            };

            _context.Instances.Add(instance);
            await _context.SaveChangesAsync();

            return $"✅ Aprendi! '{subject}' é '{predicate}'.\n" +
                   $"Obrigado por me ensinar! Agora tenho {await _context.Instances.CountAsync()} instâncias.";
        }

        private async Task<string> LearnHasRelation(string subject, string property)
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

            return $"✅ Aprendi mais sobre '{subject}'!\n" +
                   $"Agora sei que '{subject}' tem '{property}'.";
        }

        private async Task<string> LearnPropertyRelation(string subject, string propertyName, string propertyValue)
        {
            var instance = await GetOrCreateInstance(subject);

            var instanceProp = new InstanceProperty
            {
                InstanceId = instance.Id,
                PropertyName = propertyName,
                PropertyValue = propertyValue,
                CreatedAt = DateTime.Now
            };

            _context.InstanceProperties.Add(instanceProp);
            await _context.SaveChangesAsync();

            return $"✅ Aprendi! '{subject}' {propertyName}: '{propertyValue}'.\n" +
                   $"Conhecimento expandido com sucesso!";
        }

        private async Task<string> BuildConceptAnswer(Concept concept)
        {
            var answer = $"📖 {concept.Name}:\n";

            if (concept.ParentConceptId.HasValue)
            {
                var parent = await _context.Concepts.FindAsync(concept.ParentConceptId);
                answer += $"• É um tipo de: {parent?.Name}\n";
            }

            var properties = await _context.ConceptProperties
                .Where(p => p.ConceptId == concept.Id)
                .ToListAsync();

            if (properties.Any())
            {
                answer += "• Propriedades:\n";
                foreach (var prop in properties)
                {
                    answer += $"  - {prop.PropertyName}: {prop.PropertyValue}\n";
                }
            }

            var instances = await _context.Instances
                .Where(i => i.ConceptId == concept.Id)
                .Take(5)
                .ToListAsync();

            if (instances.Any())
            {
                answer += $"• Exemplos: {string.Join(", ", instances.Select(i => i.Name))}\n";
            }

            return answer;
        }

        private async Task<string> BuildInstanceAnswer(Instance instance)
        {
            var answer = $"📦 {instance.Name}:\n";

            var concept = await _context.Concepts.FindAsync(instance.ConceptId);
            if (concept != null)
            {
                answer += $"• É: {concept.Name}\n";
            }

            var properties = await _context.InstanceProperties
                .Where(p => p.InstanceId == instance.Id)
                .ToListAsync();

            if (properties.Any())
            {
                answer += "• Características:\n";
                foreach (var prop in properties)
                {
                    answer += $"  - {prop.PropertyName}: {prop.PropertyValue}\n";
                }
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

            question = Regex.Replace(question, @"\b(é|são|um|uma|o|a|os|as)\b", "", RegexOptions.IgnoreCase);

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

        private async Task<List<string>> SearchRelatedTerms(string term)
        {
            var results = new List<string>();

            var concepts = await _context.Concepts
                .Where(c => c.Name.ToLower().Contains(term.ToLower()))
                .Take(3)
                .ToListAsync();

            results.AddRange(concepts.Select(c => $"• Conceito: {c.Name}"));

            var instances = await _context.Instances
                .Include(i => i.Concept)
                .Where(i => i.Name.ToLower().Contains(term.ToLower()))
                .Take(3)
                .ToListAsync();

            results.AddRange(instances.Select(i => $"• {i.Name} (é {i.Concept?.Name})"));

            return results;
        }

        private async Task<Concept> GetOrCreateConcept(string name)
        {
            name = CleanText(name);
            var concept = await FindConcept(name);

            if (concept == null)
            {
                concept = new Concept
                {
                    Name = name,
                    CreatedAt = DateTime.Now
                };
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