using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartAI.Cognitive
{
    /// <summary>
    /// Detecta a intenção do usuário a partir da entrada.
    /// </summary>
    public class IntentDetector
    {
        private static readonly string[] QuestionWords =
            { "o que", "qual", "quem", "onde", "quando", "como", "por que", "quanto" };

        public Intent Detect(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new Intent
                {
                    Type = IntentType.UNKNOWN,
                    OriginalInput = input,
                    Confidence = 0.0
                };
            }

            input = input.Trim();

            // Detectar código
            if (IsCodeRequest(input))
            {
                return new Intent
                {
                    Type = IntentType.CODE_REQUEST,
                    OriginalInput = input,
                    Confidence = 0.95
                };
            }

            // Detectar solicitação de busca
            if (IsSearchRequest(input))
            {
                var subject = ExtractSearchTerm(input);
                return new Intent
                {
                    Type = IntentType.SEARCH_REQUEST,
                    Subject = subject,
                    OriginalInput = input,
                    Confidence = 0.90
                };
            }

            // Detectar ensino (statement)
            var teachingMatch = Regex.Match(input,
                @"^(.+?)\s+(é|são|foi|era|tem|possui|contém)\s+(.+)$",
                RegexOptions.IgnoreCase);

            if (teachingMatch.Success)
            {
                return new Intent
                {
                    Type = IntentType.TEACHING,
                    Subject = CleanText(teachingMatch.Groups[1].Value),
                    Relation = CleanText(teachingMatch.Groups[2].Value),
                    Object = CleanText(teachingMatch.Groups[3].Value),
                    OriginalInput = input,
                    Confidence = 0.85
                };
            }

            // Detectar pergunta
            if (IsQuestion(input))
            {
                var subject = ExtractQuestionSubject(input);
                return new Intent
                {
                    Type = IntentType.QUESTION,
                    Subject = subject,
                    OriginalInput = input,
                    Confidence = 0.80
                };
            }

            return new Intent
            {
                Type = IntentType.UNKNOWN,
                OriginalInput = input,
                Confidence = 0.0
            };
        }

        private bool IsCodeRequest(string input)
        {
            var patterns = new[]
            {
                @"\b(analise|analyze|revise|review|corrija|fix|melhore|improve)\b.*\b(código|code|script)\b",
                @"\b(bug|erro|error|exception|problema)\b.*\b(código|code)\b",
                @"```[\s\S]*```"
            };

            return Array.Exists(patterns, pattern =>
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsSearchRequest(string input)
        {
            var patterns = new[]
            {
                @"\b(pesquis[ae]|search|busque|procure|investigue)\b",
                @"\b(o que [eé]|what is)\b.*\b(na internet|online|web)\b"
            };

            return Array.Exists(patterns, pattern =>
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsQuestion(string input)
        {
            input = input.ToLower();
            return input.EndsWith("?") ||
                   Array.Exists(QuestionWords, qw => input.StartsWith(qw));
        }

        private string ExtractQuestionSubject(string input)
        {
            input = input.ToLower().TrimEnd('?', '.', '!');

            foreach (var qw in QuestionWords)
            {
                if (input.StartsWith(qw))
                {
                    input = input.Substring(qw.Length).Trim();
                    break;
                }
            }

            // Remover palavras comuns
            input = Regex.Replace(input, @"\b(é|são|um|uma|o|a|de|da|do)\b", "",
                RegexOptions.IgnoreCase);

            return CleanText(input);
        }

        private string ExtractSearchTerm(string input)
        {
            // Remover palavras de comando
            var cleaned = Regex.Replace(input,
                @"\b(pesquis[ae]|search|busque|procure|sobre|about|na internet|online|web)\b",
                "",
                RegexOptions.IgnoreCase);

            return CleanText(cleaned);
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
}