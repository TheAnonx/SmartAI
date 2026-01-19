using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using SmartAI.AI;
using SmartAI.Data;
using SmartAI.Services;

namespace SmartAI
{
    public partial class MainWindow : Window
    {
        private readonly AIContext _context;
        private readonly IntelligenceEngine _engine;
        private ObservableCollection<ChatMessage> _messages;
        private ObservableCollection<string> _recentLearning;

        public MainWindow()
        {
            InitializeComponent();

            _context = new AIContext();
            _context.Database.EnsureCreated();

            _engine = new IntelligenceEngine(_context);

            _messages = new ObservableCollection<ChatMessage>();
            _recentLearning = new ObservableCollection<string>();

            ChatMessages.ItemsSource = _messages;
            RecentLearning.ItemsSource = _recentLearning;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateStatistics();
            AddSystemMessage("Sistema inicializado! Estou pronto para aprender e responder perguntas. 🚀");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessUserInput();
        }

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(InputBox.Text))
            {
                await ProcessUserInput();
            }
        }

        private async Task ProcessUserInput()
        {
            var input = InputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            // Adicionar mensagem do usuário
            AddUserMessage(input);
            InputBox.Clear();

            // Mostrar que está processando
            StatusText.Text = "Processando...";
            SendButton.IsEnabled = false;

            try
            {
                // Processar com a IA
                var response = await _engine.ProcessInput(input);

                // Adicionar resposta da IA
                AddAssistantMessage(response);

                // Atualizar estatísticas
                await UpdateStatistics();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto. Ensine-me ou faça perguntas!";
                SendButton.IsEnabled = true;
                InputBox.Focus();
            }
        }

        private async void SearchWeb_Click(object sender, RoutedEventArgs e)
        {
            // Usar o novo SearchDialog moderno
            var dialog = new SearchDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                var query = dialog.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return;

                AddUserMessage($"🌐 Buscar: {query}");

                StatusText.Text = "Buscando na web...";
                SendButton.IsEnabled = false;

                try
                {
                    var searchService = new Services.WebSearchService();
                    var result = await searchService.SmartSearch(query);

                    if (result.Success && !string.IsNullOrEmpty(result.Summary))
                    {
                        // PRIMEIRO: Mostrar os resultados
                        AddAssistantMessage(result.ToString());

                        // Rolar até o final para ver tudo
                        await Task.Delay(300);
                        ScrollToBottom();

                        // DEPOIS: Extrair fatos
                        var facts = searchService.ExtractFacts(result.Summary);

                        if (facts.Any())
                        {
                            // Mostrar mensagem que encontrou fatos
                            AddSystemMessage($"💡 Encontrei {facts.Count} fatos que posso aprender!");
                            await Task.Delay(300);
                            ScrollToBottom();

                            // Abrir janela moderna de seleção
                            var learnDialog = new LearningDialog(facts)
                            {
                                Owner = this
                            };

                            if (learnDialog.ShowDialog() == true && learnDialog.SelectedFacts.Any())
                            {
                                var selectedFacts = learnDialog.SelectedFacts;
                                int learned = 0;

                                AddSystemMessage($"🧠 Processando {selectedFacts.Count} fatos selecionados...");

                                foreach (var fact in selectedFacts)
                                {
                                    try
                                    {
                                        // Remover numeração (ex: "1. Python é..." -> "Python é...")
                                        var cleanFact = System.Text.RegularExpressions.Regex.Replace(
                                            fact, @"^\d+\.\s*", "");

                                        var response = await _engine.ProcessInput(cleanFact);
                                        if (response.Contains("✅"))
                                        {
                                            learned++;
                                        }
                                    }
                                    catch { }
                                }

                                AddSystemMessage($"✅ Aprendi {learned} de {selectedFacts.Count} conceitos da busca web!");
                                await UpdateStatistics();
                            }
                            else
                            {
                                AddSystemMessage("Ok, não vou aprender esses fatos. Você pode me ensinar manualmente quando quiser!");
                            }
                        }
                        else
                        {
                            AddSystemMessage("ℹ️ Não consegui extrair fatos estruturados desse resultado, mas você pode me ensinar manualmente!");
                        }
                    }
                    else
                    {
                        AddAssistantMessage($"❌ Não encontrei resultados para '{query}'.\n\n" +
                            $"Erro: {result.Error ?? "Sem informações disponíveis"}");
                    }
                }
                catch (Exception ex)
                {
                    AddSystemMessage($"❌ Erro na busca: {ex.Message}");
                }
                finally
                {
                    StatusText.Text = "Sistema pronto.";
                    SendButton.IsEnabled = true;
                }
            }
        }

        private async void ImportFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Arquivos de Texto|*.txt|Todos os Arquivos|*.*",
                Title = "Importar Conhecimento"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusText.Text = "Importando arquivo...";
                    var content = await File.ReadAllTextAsync(dialog.FileName);
                    var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    int learned = 0;
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.Length > 5) // Ignorar linhas muito curtas
                        {
                            await _engine.ProcessInput(trimmed);
                            learned++;
                        }
                    }

                    AddSystemMessage($"✅ Arquivo importado! Aprendi {learned} novos conceitos.");
                    await UpdateStatistics();
                }
                catch (Exception ex)
                {
                    AddSystemMessage($"❌ Erro ao importar: {ex.Message}");
                }
                finally
                {
                    StatusText.Text = "Sistema pronto.";
                }
            }
        }

        private async void ExportKnowledge_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON|*.json|Texto|*.txt",
                Title = "Exportar Conhecimento",
                FileName = $"conhecimento_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusText.Text = "Exportando conhecimento...";

                    var knowledge = new
                    {
                        ExportedAt = DateTime.Now,
                        Statistics = await _engine.GetStatistics(),
                        Concepts = _context.Concepts.Select(c => new { c.Name, c.CreatedAt }).ToList(),
                        Instances = _context.Instances.Select(i => new { i.Name, ConceptId = i.ConceptId }).ToList(),
                        Properties = _context.InstanceProperties.Select(p => new {
                            p.PropertyName,
                            p.PropertyValue
                        }).ToList()
                    };

                    var json = JsonConvert.SerializeObject(knowledge, Formatting.Indented);
                    await File.WriteAllTextAsync(dialog.FileName, json);

                    AddSystemMessage($"✅ Conhecimento exportado para: {Path.GetFileName(dialog.FileName)}");
                }
                catch (Exception ex)
                {
                    AddSystemMessage($"❌ Erro ao exportar: {ex.Message}");
                }
                finally
                {
                    StatusText.Text = "Sistema pronto.";
                }
            }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Tem certeza que deseja limpar o chat? (O conhecimento será preservado)",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _messages.Clear();
                AddSystemMessage("Chat limpo. Conhecimento preservado.");
            }
        }

        private void AddUserMessage(string message)
        {
            _messages.Add(new ChatMessage
            {
                Message = message,
                Sender = "Você",
                IsUser = true,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            });
            ScrollToBottom();
        }

        private void AddAssistantMessage(string message)
        {
            _messages.Add(new ChatMessage
            {
                Message = message,
                Sender = "Smart AI",
                IsUser = false,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            });
            ScrollToBottom();
        }

        private void AddSystemMessage(string message)
        {
            _messages.Add(new ChatMessage
            {
                Message = message,
                Sender = "Sistema",
                IsUser = false,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            });
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            ChatScroll.ScrollToEnd();
        }

        private async Task UpdateStatistics()
        {
            var stats = await _engine.GetStatistics();

            ConceptCount.Text = stats["Concepts"].ToString();
            InstanceCount.Text = stats["Instances"].ToString();
            RelationCount.Text = (stats["ConceptProperties"] + stats["InstanceProperties"]).ToString();
            ConversationCount.Text = stats["Conversations"].ToString();

            // Atualizar aprendizado recente
            _recentLearning.Clear();
            var recent = await _engine.GetRecentLearning(5);
            foreach (var item in recent)
            {
                _recentLearning.Add(item);
            }
        }
    }

    public class ChatMessage
    {
        public string Message { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }
}