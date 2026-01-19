using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartAI.Data;
using SmartAI.Models;

namespace SmartAI.AI
{
    public class IntelligenceEngine
    {
        private readonly AIContext _context;

        public IntelligenceEngine(AIContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Processa pergunta do usuário e retorna resposta
        /// </summary>
        public AIResponse ProcessQuestion(string question)
        {
            question = question.Trim();

            // 1. Tenta entender a pergunta
            var analysis = AnalyzeQuestion(question);

            // 2. Busca conhecimento
            if (analysis.IsQuestion)
            {
                return AnswerQuestion(analysis);
            }

            // 3. Se é afirmação, aprende
            if (analysis.IsStatement)
            {
                return LearnFromStatement(question, analysis);
            }

            // 4. Não entendeu
            return new AIResponse
            {
                Message = "Não entendi. Pode reformular? Ou me ensinar algo novo?",
                NeedsMoreInfo = true,
                Confidence = 0
            };
        }

        /// <summary>
        /// Analisa estrutura da pergunta
        /// </summary>
        private QuestionAnalysis AnalyzeQuestion(string text)
        {
            var analysis = new QuestionAnalysis();
            text = text.ToLower();

            // Detecta perguntas
            var questionWords = new[] { "o que é", "quem é", "onde fica", "quando", "como", "por que", "qual" };
            analysis.IsQuestion = text.Contains("?") || questionWords.Any(q => text.Contains(q));

            // Detecta afirmações (ensino)
            var statementPatterns = new[]
            {
                @"(.+) é (um|uma) (.+)",
                @"(.+) tem (.+)",
                @"(.+) vive em (.+)",
                @"(.+) foi (.+)"
            };

            foreach (var pattern in statementPatterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    analysis.IsStatement = true;
                    analysis.Subject = match.Groups[1].Value.Trim();
                    analysis.Predicate = match.Groups.Count > 2 ? match.Groups[2].Value.Trim() : "";
                    analysis.Object = match.Groups.Count > 3 ? match.Groups[3].Value.Trim() : "";
                    break;
                }
            }

            // Extrai entidade principal
            if (analysis.IsQuestion)
            {
                foreach (var word in questionWords)
                {
                    if (text.Contains(word))
                    {
                        var parts = text.Replace("?", "").Split(new[] { word }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            analysis.Subject = parts[1].Trim();
                        }
                        break;
                    }
                }
            }

            return analysis;
        }

        /// <summary>
        /// Tenta responder pergunta com conhecimento existente
        /// </summary>
        private AIResponse AnswerQuestion(QuestionAnalysis analysis)
        {
            if (string.IsNullOrWhiteSpace(analysis.Subject))
            {
                return new AIResponse
                {
                    Message = "Sobre o que você quer saber?",
                    NeedsMoreInfo = true,
                    Confidence = 0
                };
            }

            // Busca na base de conhecimento
            var instance = _context.Instances
                .Include(i => i.Concept)
                .Include(i => i.Properties)
                .FirstOrDefault(i => i.Name.ToLower().Contains(analysis.Subject.ToLower()));

            if (instance == null)
            {
                return new AIResponse
                {
                    Message = $"Não sei nada sobre '{analysis.Subject}'. Pode me ensinar?",
                    NeedsMoreInfo = true,
                    Confidence = 0,
                    SuggestedLearning = $"Me diga: {analysis.Subject} é um/uma ___"
                };
            }

            // Constrói resposta com o que sabe
            var info = $"{instance.Name} é {instance.Concept.Name}.";

            if (!string.IsNullOrWhiteSpace(instance.Description))
            {
                info += $" {instance.Description}.";
            }

            // Adiciona propriedades conhecidas
            var properties = GetAllProperties(instance);
            if (properties.Any())
            {
                info += " " + string.Join(", ", properties.Select(p =>
                    $"{p.PropertyName} {p.PropertyValue}"));
            }

            return new AIResponse
            {
                Message = info,
                Confidence = 0.8,
                NeedsMoreInfo = false
            };
        }

        /// <summary>
        /// Aprende com afirmação do usuário
        /// </summary>
        private AIResponse LearnFromStatement(string statement, QuestionAnalysis analysis)
        {
            try
            {
                // Verifica se já conhece essa entidade
                var instance = _context.Instances
                    .Include(i => i.Concept)
                    .FirstOrDefault(i => i.Name.ToLower() == analysis.Subject.ToLower());

                if (instance == null)
                {
                    // Tenta encontrar ou criar conceito
                    var concept = FindOrCreateConcept(analysis.Object);

                    // Cria nova instância
                    instance = new Instance
                    {
                        Name = CapitalizeFirst(analysis.Subject),
                        ConceptId = concept.Id,
                        Source = "Ensinado pelo usuário",
                        LearnedAt = DateTime.UtcNow
                    };

                    _context.Instances.Add(instance);
                    _context.SaveChanges();

                    return new AIResponse
                    {
                        Message = $"✓ Aprendi! {instance.Name} é {concept.Name}. Obrigado por me ensinar!",
                        Confidence = 1.0,
                        Learned = true
                    };
                }
                else
                {
                    // Adiciona propriedade à instância existente
                    var prop = new InstanceProperty
                    {
                        InstanceId = instance.Id,
                        PropertyName = analysis.Predicate,
                        PropertyValue = analysis.Object,
                        Source = "Ensinado pelo usuário"
                    };

                    _context.InstanceProperties.Add(prop);
                    _context.SaveChanges();

                    return new AIResponse
                    {
                        Message = $"✓ Aprendi mais sobre {instance.Name}! Agora sei que {analysis.Predicate} {analysis.Object}.",
                        Confidence = 1.0,
                        Learned = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new AIResponse
                {
                    Message = $"Tive dificuldade em aprender isso. Pode reformular? ({ex.Message})",
                    NeedsMoreInfo = true,
                    Confidence = 0
                };
            }
        }

        /// <summary>
        /// Encontra ou cria conceito
        /// </summary>
        private Concept FindOrCreateConcept(string name)
        {
            name = CapitalizeFirst(name);

            var concept = _context.Concepts
                .FirstOrDefault(c => c.Name.ToLower() == name.ToLower());

            if (concept == null)
            {
                concept = new Concept
                {
                    Name = name,
                    ParentConceptId = 1 // Conceito "Coisa"
                };
                _context.Concepts.Add(concept);
                _context.SaveChanges();
            }

            return concept;
        }

        /// <summary>
        /// Obtém todas as propriedades (herdadas + específicas)
        /// </summary>
        private System.Collections.Generic.List<InstanceProperty> GetAllProperties(Instance instance)
        {
            return _context.InstanceProperties
                .Where(p => p.InstanceId == instance.Id)
                .ToList();
        }

        /// <summary>
        /// Capitaliza primeira letra
        /// </summary>
        private string CapitalizeFirst(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
        }
    }

    /// <summary>
    /// Análise da pergunta
    /// </summary>
    public class QuestionAnalysis
    {
        public bool IsQuestion { get; set; }
        public bool IsStatement { get; set; }
        public string Subject { get; set; }
        public string Predicate { get; set; }
        public string Object { get; set; }
    }

    /// <summary>
    /// Resposta da IA
    /// </summary>
    public class AIResponse
    {
        public string Message { get; set; }
        public double Confidence { get; set; }
        public bool NeedsMoreInfo { get; set; }
        public bool Learned { get; set; }
        public string SuggestedLearning { get; set; }
    }
}