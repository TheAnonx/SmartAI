using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using SmartAI.AI;
using SmartAI.Data;
using SmartAI.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartAI
{
    public partial class MainWindow : Window
    {
        private readonly AIContext _context;
        private readonly EnhancedIntelligenceEngine _engine;
        private ObservableCollection<ChatMessage> _messages;
        private ObservableCollection<string> _recentLearning;

        public MainWindow()
        {
            InitializeComponent();

            var optionsBuilder = new DbContextOptionsBuilder<AIContext>();
            optionsBuilder.UseSqlite("Data Source=smartai.db");

            _context = new AIContext(optionsBuilder.Options);
            _context.Database.EnsureCreated();

            _engine = new EnhancedIntelligenceEngine(_context);

            _messages = new ObservableCollection<ChatMessage>();
            _recentLearning = new ObservableCollection<string>();

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

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                await ProcessUserInput();
            }
        }

        private async Task ProcessUserInput()
        {
            var input = InputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            AddUserMessage(input);
            InputTextBox.Clear();

            StatusText.Text = "Processando...";
            SendButton.IsEnabled = false;

            try
            {
                var response = await _engine.ProcessInput(input);
                AddAssistantMessage(response);
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
                InputTextBox.Focus();
            }
        }

        private async void WebSearch_Click(object sender, RoutedEventArgs e)
        {
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
                    var searchService = new WebSearchService();
                    var result = await searchService.SmartSearch(query);

                    if (result.Success && !string.IsNullOrEmpty(result.Summary))
                    {
                        AddAssistantMessage(result.ToString());
                        await Task.Delay(300);
                        ScrollToBottom();

                        var facts = searchService.ExtractFacts(result.Summary);

                        if (facts.Any())
                        {
                            AddSystemMessage($"💡 Encontrei {facts.Count} fatos que posso aprender!");
                            await Task.Delay(300);
                            ScrollToBottom();

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
                        if (trimmed.Length > 5)
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
                ChatPanel.Children.Clear();
                AddSystemMessage("Chat limpo. Conhecimento preservado.");
            }
        }

        private async void RunTests_Click(object sender, RoutedEventArgs e)
        {
            // Criar janela para mostrar os resultados dos testes
            var testWindow = new Window
            {
                Title = "Testes do Sistema Epistêmico",
                Width = 900,
                Height = 700,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };

            var textBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10)
            };

            scrollViewer.Content = textBox;
            testWindow.Content = scrollViewer;

            // Mostrar janela
            testWindow.Show();

            // Redirecionar Console.WriteLine para a TextBox
            var originalOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                // Executar os testes
                textBox.Text = "Iniciando testes...\n\n";
                await Task.Run(async () => await SmartAI.Tests.EpistemicSystemTests.RunAllTests());

                // Mostrar resultados
                textBox.Text = writer.ToString();

                // Auto-scroll para o final
                scrollViewer.ScrollToEnd();
            }
            catch (Exception ex)
            {
                textBox.Text += $"\n\n❌ ERRO AO EXECUTAR TESTES:\n{ex.Message}\n\n{ex.StackTrace}";
            }
            finally
            {
                // Restaurar Console original
                Console.SetOut(originalOut);
            }
        }

        // Métodos para os novos eventos do XAML (implementação básica)
        private void ShowStats_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage("📊 Exibindo estatísticas do sistema...");
            // Implementação futura: mostrar janela detalhada de estatísticas
        }

        private void CheckConflicts_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage("⚠️ Verificando conflitos no conhecimento...");
            // Implementação futura: verificação de conflitos
        }

        private void ShowValidatedFacts_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage("📚 Exibindo fatos validados...");
            // Implementação futura: mostrar lista de fatos validados
        }

        private void ShowCandidateFacts_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage("⏳ Exibindo fatos candidatos...");
            // Implementação futura: mostrar lista de candidatos
        }

        private void ShowLearningHistory_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage("🗂️ Exibindo histórico de aprendizado...");
            // Implementação futura: mostrar histórico
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage("ℹ️ Sistema Epistêmico SmartAI v1.0\n" +
                            "Sistema cognitivo com validação epistêmica e aprendizado contínuo.");
        }

        private void ShowCommands_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage("📖 Comandos disponíveis:\n" +
                            "• aprender: [fato] - Ensina um novo fato\n" +
                            "• pesquisar: [termo] - Busca na web\n" +
                            "• estatísticas - Mostra estatísticas\n" +
                            "• verificar conflitos - Verifica inconsistências\n" +
                            "• limpar - Limpa o chat");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Deseja realmente sair do sistema?",
                "Confirmar Saída",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void AddUserMessage(string message)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 0, 122, 204)),
                CornerRadius = new CornerRadius(12, 12, 12, 4),
                Margin = new Thickness(0, 5, 60, 5),
                Padding = new Thickness(12),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var stackPanel = new StackPanel();

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            headerStack.Children.Add(new TextBlock
            {
                Text = "Você",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.LightBlue,
                FontSize = 12,
                Margin = new Thickness(0, 0, 10, 2)
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Foreground = Brushes.Gray,
                FontSize = 10
            });

            stackPanel.Children.Add(headerStack);
            stackPanel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            });

            border.Child = stackPanel;
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        private void AddAssistantMessage(string message)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 78, 201, 176)),
                CornerRadius = new CornerRadius(12, 12, 4, 12),
                Margin = new Thickness(60, 5, 0, 5),
                Padding = new Thickness(12)
            };

            var stackPanel = new StackPanel();

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            headerStack.Children.Add(new TextBlock
            {
                Text = "Smart AI",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(78, 201, 176)),
                FontSize = 12,
                Margin = new Thickness(0, 0, 10, 2)
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Foreground = Brushes.Gray,
                FontSize = 10
            });

            stackPanel.Children.Add(headerStack);
            stackPanel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            });

            border.Child = stackPanel;
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        private void AddSystemMessage(string message)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(40, 5, 40, 5),
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(new TextBlock
            {
                Text = "⚡ ",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Yellow,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                FontStyle = FontStyles.Italic
            });

            border.Child = stackPanel;
            ChatPanel.Children.Add(border);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        private async Task UpdateStatistics()
        {
            var stats = await _engine.GetStatistics();

            // Atualizar estatísticas na barra de status
            ModeIndicator.Text = "Modo: ANSWER";
            FactCount.Text = $"Fatos: {stats["Concepts"]}";

            // Atualizar estatísticas no painel lateral
            CurrentModeText.Text = "ANSWER";
            ValidatedFactsCountText.Text = stats["Concepts"].ToString();
            CandidatesCountText.Text = stats["Instances"].ToString();

            var conflicts = 0; // Implementar contagem de conflitos futuramente
            ConflictIndicator.Text = $"Conflitos: {conflicts}";
            ConflictsCountText.Text = conflicts.ToString();

            // Atualizar aprendizado recente
            RecentLearningPanel.Children.Clear();
            var recent = await _engine.GetRecentLearning(5);

            if (recent.Any())
            {
                foreach (var item in recent)
                {
                    var textBlock = new TextBlock
                    {
                        Text = $"• {item}",
                        Foreground = Brushes.LightGray,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    RecentLearningPanel.Children.Add(textBlock);
                }
            }
            else
            {
                RecentLearningPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhum aprendizado recente",
                    Foreground = Brushes.Gray,
                    FontSize = 12
                });
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