using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartAI.Services
{
    public class CodeAnalysisService
    {
        public CodeAnalysis AnalyzeCode(string code)
        {
            var analysis = new CodeAnalysis
            {
                Language = DetectLanguage(code),
                Complexity = CalculateComplexity(code)
            };

            // Análise específica por linguagem
            switch (analysis.Language.ToLower())
            {
                case "c#":
                case "csharp":
                    AnalyzeCSharp(code, analysis);
                    break;
                case "python":
                    AnalyzePython(code, analysis);
                    break;
                case "javascript":
                case "typescript":
                    AnalyzeJavaScript(code, analysis);
                    break;
                default:
                    AnalyzeGeneral(code, analysis);
                    break;
            }

            return analysis;
        }

        public string DetectLanguage(string code)
        {
            // C#
            if (Regex.IsMatch(code, @"\b(using\s+\w+;|namespace\s+\w+|class\s+\w+|void\s+\w+\()\b"))
                return "C#";

            // Python
            if (Regex.IsMatch(code, @"\b(def\s+\w+|import\s+\w+|from\s+\w+|class\s+\w+:)\b"))
                return "Python";

            // JavaScript/TypeScript
            if (Regex.IsMatch(code, @"\b(const\s+|let\s+|var\s+|function\s+|=>|import\s+.*from)\b"))
                return "JavaScript";

            // Java
            if (Regex.IsMatch(code, @"\b(public\s+class|private\s+class|static\s+void\s+main)\b"))
                return "Java";

            // SQL
            if (Regex.IsMatch(code, @"\b(SELECT|INSERT|UPDATE|DELETE|CREATE\s+TABLE)\b", RegexOptions.IgnoreCase))
                return "SQL";

            return "Desconhecida";
        }

        private void AnalyzeCSharp(string code, CodeAnalysis analysis)
        {
            // Verificar boas práticas C#

            // Falta de using statements
            if (!code.Contains("using") && code.Contains("namespace"))
            {
                analysis.Issues.Add("⚠️ Considere adicionar using statements necessários");
            }

            // Variáveis não inicializadas
            if (Regex.IsMatch(code, @"\b(int|string|bool|double)\s+\w+;"))
            {
                analysis.Suggestions.Add("💡 Inicialize variáveis ao declará-las para evitar valores nulos");
            }

            // Métodos muito longos
            var methods = Regex.Matches(code, @"(public|private|protected)\s+\w+\s+\w+\([^)]*\)\s*\{[^}]*\}");
            foreach (Match method in methods)
            {
                var lines = method.Value.Split('\n').Length;
                if (lines > 50)
                {
                    analysis.Issues.Add($"⚠️ Método detectado com {lines} linhas. Considere quebrar em métodos menores");
                }
            }

            // Falta de tratamento de exceção
            if (code.Contains("await") && !code.Contains("try"))
            {
                analysis.Suggestions.Add("💡 Considere adicionar try-catch para operações assíncronas");
            }

            // Uso de async sem await
            if (Regex.IsMatch(code, @"async\s+\w+\s+\w+\([^)]*\)") && !code.Contains("await"))
            {
                analysis.Issues.Add("⚠️ Método async sem await - considere remover async ou adicionar await");
            }

            // Strings concatenadas em loops
            if (Regex.IsMatch(code, @"(for|foreach|while).*\{[^}]*\+=.*[""']"))
            {
                analysis.Suggestions.Add("💡 Use StringBuilder para concatenação de strings em loops");
            }

            // Falta de null checking
            if (Regex.IsMatch(code, @"\.\w+\(") && !code.Contains("?") && !code.Contains("null"))
            {
                analysis.Suggestions.Add("💡 Considere usar null-conditional operator (?.) para segurança");
            }

            // LINQ mal utilizado
            if (code.Contains(".ToList()") && code.Contains(".Where("))
            {
                var toListCount = Regex.Matches(code, @"\.ToList\(\)").Count;
                if (toListCount > 2)
                {
                    analysis.Suggestions.Add("💡 Evite múltiplos .ToList() - execute a query apenas uma vez");
                }
            }

            // Sugestões de modernização
            if (Regex.IsMatch(code, @"new\s+\w+\<.*\>\(\)"))
            {
                analysis.Suggestions.Add("💡 Considere usar 'new()' (target-typed new) em C# 9+");
            }

            // Boas práticas
            analysis.Suggestions.Add("✅ Use nomes descritivos para variáveis e métodos");
            analysis.Suggestions.Add("✅ Adicione comentários XML para métodos públicos");
            analysis.Suggestions.Add("✅ Considere usar records para objetos imutáveis");
        }

        private void AnalyzePython(string code, CodeAnalysis analysis)
        {
            // PEP 8 - indentação
            if (code.Contains("\t"))
            {
                analysis.Issues.Add("⚠️ Use espaços em vez de tabs (PEP 8)");
            }

            // Imports não otimizados
            var imports = Regex.Matches(code, @"^import\s+.*", RegexOptions.Multiline);
            if (imports.Count > 5)
            {
                analysis.Suggestions.Add("💡 Organize imports: stdlib, third-party, local");
            }

            // Variáveis globais
            if (Regex.IsMatch(code, @"^\w+\s*=\s*", RegexOptions.Multiline) && !code.Contains("def "))
            {
                analysis.Suggestions.Add("💡 Evite variáveis globais, use classes ou funções");
            }

            // Type hints
            if (code.Contains("def ") && !code.Contains("->"))
            {
                analysis.Suggestions.Add("💡 Adicione type hints para melhor documentação");
            }

            analysis.Suggestions.Add("✅ Use list comprehensions quando apropriado");
            analysis.Suggestions.Add("✅ Docstrings para funções e classes");
        }

        private void AnalyzeJavaScript(string code, CodeAnalysis analysis)
        {
            // Uso de var
            if (code.Contains("var "))
            {
                analysis.Issues.Add("⚠️ Prefira 'const' ou 'let' em vez de 'var'");
            }

            // Comparação não estrita
            if (Regex.IsMatch(code, @"[^=!]=[^=]"))
            {
                analysis.Suggestions.Add("💡 Use === e !== para comparações estritas");
            }

            // Arrow functions
            if (code.Contains("function(") && !code.Contains("=>"))
            {
                analysis.Suggestions.Add("💡 Considere usar arrow functions para callbacks");
            }

            // Promises sem tratamento
            if (code.Contains(".then(") && !code.Contains(".catch("))
            {
                analysis.Issues.Add("⚠️ Adicione .catch() para tratar erros em Promises");
            }

            analysis.Suggestions.Add("✅ Use destructuring para objetos e arrays");
            analysis.Suggestions.Add("✅ Prefira async/await para código assíncrono");
        }

        private void AnalyzeGeneral(string code, CodeAnalysis analysis)
        {
            // Linhas muito longas
            var longLines = code.Split('\n').Where(l => l.Length > 120).ToList();
            if (longLines.Any())
            {
                analysis.Issues.Add($"⚠️ {longLines.Count} linha(s) excedem 120 caracteres");
            }

            // Código comentado
            var commentedCode = Regex.Matches(code, @"//.*\w+\(|/\*.*\w+\(.*\*/");
            if (commentedCode.Count > 3)
            {
                analysis.Suggestions.Add("💡 Remova código comentado - use controle de versão");
            }

            // Magic numbers
            var numbers = Regex.Matches(code, @"\b\d{2,}\b");
            if (numbers.Count > 3)
            {
                analysis.Suggestions.Add("💡 Substitua 'magic numbers' por constantes nomeadas");
            }

            // Duplicação de código
            var lines = code.Split('\n').Where(l => l.Trim().Length > 20).ToList();
            var duplicates = lines.GroupBy(l => l.Trim())
                                 .Where(g => g.Count() > 2)
                                 .ToList();

            if (duplicates.Any())
            {
                analysis.Issues.Add($"⚠️ Detectada possível duplicação de código");
                analysis.Suggestions.Add("💡 Considere extrair código duplicado para métodos");
            }
        }

        private string CalculateComplexity(string code)
        {
            int complexity = 1; // Complexidade base

            // Contar estruturas de controle
            complexity += Regex.Matches(code, @"\b(if|else if|for|foreach|while|switch|case)\b").Count;
            complexity += Regex.Matches(code, @"&&|\|\|").Count;
            complexity += Regex.Matches(code, @"\?.*:").Count; // Operador ternário

            if (complexity <= 5)
                return "Baixa ⭐";
            else if (complexity <= 10)
                return "Média ⭐⭐";
            else if (complexity <= 20)
                return "Alta ⭐⭐⭐";
            else
                return "Muito Alta ⭐⭐⭐⭐ (Refatoração recomendada)";
        }

        public string GenerateImprovedCode(string code, string language)
        {
            // Sugestão básica de código melhorado
            var improved = code;

            switch (language.ToLower())
            {
                case "c#":
                    improved = ImproveCodeCSharp(code);
                    break;
                case "python":
                    improved = ImproveCodePython(code);
                    break;
            }

            return improved;
        }

        private string ImproveCodeCSharp(string code)
        {
            var improved = code;

            // Adicionar null checking
            improved = Regex.Replace(improved,
                @"(\w+)\.(\w+)",
                "$1?.$2");

            // Sugerir async/await
            if (improved.Contains("Task") && !improved.Contains("await"))
            {
                improved = "// Considere usar await para métodos Task\n" + improved;
            }

            return improved;
        }

        private string ImproveCodePython(string code)
        {
            var improved = code;

            // Adicionar type hints básicos
            improved = Regex.Replace(improved,
                @"def\s+(\w+)\(([^)]*)\):",
                "def $1($2) -> None:");

            return improved;
        }
    }

    public class CodeAnalysis
    {
        public string Language { get; set; } = "Desconhecida";
        public string Complexity { get; set; } = "Média";
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Suggestions { get; set; } = new List<string>();
        public List<string> GoodPractices { get; set; } = new List<string>();
    }
}