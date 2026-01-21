// Arquivo completo - substituir todo o conteúdo
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using SmartAI.Cognitive;
using SmartAI.Data;
using SmartAI.Models;
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
        private readonly CognitiveEngine _cognitiveEngine;
        private readonly ValidationService _validationService;
        private readonly ConflictDetectionService _conflictService;
        private readonly FactService _factService;

        private ObservableCollection<ChatMessage> _messages;
        private ObservableCollection<string> _recentLearning;

        public MainWindow()
        {
            InitializeComponent();

            var optionsBuilder = new DbContextOptionsBuilder<AIContext>();
            optionsBuilder.UseSqlite("Data Source=smartai.db");

            _context = new AIContext(optionsBuilder.Options);
            _context.Database.EnsureCreated();

            // USAR SISTEMA EPISTÊMICO
            _cognitiveEngine = new CognitiveEngine(_context);
            _validationService = new ValidationService(_context);
            _conflictService = new ConflictDetectionService(_context);
            _factService = new FactService(_context);

            _messages = new ObservableCollection<ChatMessage>();
            _recentLearning = new ObservableCollection<string>();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateStatistics();
            AddSystemMessage("🚀 Sistema Epistêmico Inicializado!\n\n" +
                           "Características:\n" +
                           "• Validação humana obrigatória\n" +
                           "• Rastreamento completo de proveniência\n" +
                           "• Detecção automática de conflitos\n" +
                           "• Confiança sempre < 100%\n\n" +
                           "Digite sua pergunta ou ensine-me algo!");
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
                // USAR COGNITIVE ENGINE
                var response = await _cognitiveEngine.Process(input);

                // Atualizar indicador de modo
                ModeIndicator.Text = $"Modo: {response.Mode}";
                CurrentModeText.Text = response.Mode.ToString();

                // Se requer validação E tem candidatos, apresentar dialog
                if (response.RequiresAction && 
                    response.CandidateFacts != null && 
                    response.CandidateFacts.Any())
                {
                    AddAssistantMessage(response.Text);
                    await PresentValidationDialog(response);
                }
                // Se sugeriu investigação como opção
                else if (response.RequiresAction && 
                         response.SuggestedMode == CognitiveMode.INVESTIGATION)
                {
                    // Oferecer investigação
                    AddAssistantMessage(response.Text);

                    var result = MessageBox.Show(
                        "Deseja que eu investigue na web?",
                        "Investigação Web",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Extrair o subject da resposta ou usar o input original
                        var subject = response.Facts?.FirstOrDefault()?.Subject ?? input;
                        await InvestigateWeb(subject);
                    }
                }
                else
                {
                    AddAssistantMessage(response.Text);
                }

                await UpdateStatistics();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
                SendButton.IsEnabled = true;
                InputTextBox.Focus();
            }
        }

        private async Task PresentValidationDialog(CognitiveResponse response)
        {
            StatusText.Text = "Aguardando validação...";

            // Criar sessão de validação
            var session = await _validationService.StartValidationSession(
                response.InvestigationResult?.Query ?? "direct input",
                response.CandidateFacts ?? new System.Collections.Generic.List<Fact>()
            );

            // Mostrar dialog
            var dialog = new ValidationDialog(response.CandidateFacts ?? new System.Collections.Generic.List<Fact>())
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                StatusText.Text = "Processando validação...";

                // Processar decisões do usuário
                var result = await _validationService.ProcessUserDecisions(
                    session.Id,
                    dialog.Decisions,
                    "user"
                );

                AddSystemMessage(result.GetSummary());
                await UpdateStatistics();

                // Verificar conflitos após aprendizado
                await CheckAndPresentConflicts();
            }
            else
            {
                AddSystemMessage("Validação cancelada. Nenhum fato foi persistido.");
            }

            StatusText.Text = "Sistema pronto";
        }

        private async Task InvestigateWeb(string query)
        {
            StatusText.Text = "Investigando na web...";

            try
            {
                // Chamar diretamente o serviço de investigação
                var investigationService = new InvestigationService(_context);
                var investigationResult = await investigationService.Investigate(query);

                if (investigationResult.Success && investigationResult.CandidateFacts.Any())
                {
                    // Formatar resposta da investigação
                    var responseText = $"🔍 **Investigação Concluída**\n\n";
                    responseText += $"Encontrei {investigationResult.CandidateFacts.Count} possíveis fatos sobre '{query}':\n\n";

                    for (int i = 0; i < investigationResult.CandidateFacts.Count; i++)
                    {
                        var fact = investigationResult.CandidateFacts[i];
                        responseText += $"{i + 1}. {fact.Subject} {fact.Relation} {fact.Object}\n";
                    }

                    responseText += $"\n📚 Fonte: {investigationResult.SourceName}\n";
                    responseText += $"🔗 {investigationResult.SourceUrl}\n\n";
                    responseText += $"⚠️ **IMPORTANTE**: Estes são CANDIDATOS, não fatos validados.\n";
                    responseText += $"Você pode validá-los usando o menu 'Conhecimento' → 'Fatos Candidatos'.";

                    AddAssistantMessage(responseText);

                    // Salvar os fatos candidatos no banco de dados para validação futura
                    var savedFactsCount = 0;
                    foreach (var candidate in investigationResult.CandidateFacts)
                    {
                        try
                        {
                            // Validar dados antes de salvar
                            var subject = string.IsNullOrWhiteSpace(candidate.Subject) ? "Desconhecido" : candidate.Subject.Trim();
                            var relation = string.IsNullOrWhiteSpace(candidate.Relation) ? "tem" : candidate.Relation.Trim();
                            var obj = string.IsNullOrWhiteSpace(candidate.Object) ? "informação desconhecida" : candidate.Object.Trim();

                            // Criar fato candidato real no banco de dados
                            var fact = new Fact
                            {
                                Subject = subject,
                                Relation = relation,
                                Object = obj,
                                Confidence = 0.0,
                                Status = FactStatus.CANDIDATE,
                                CreatedAt = DateTime.Now,
                                Version = 1
                            };

                            _context.Facts.Add(fact);
                            await _context.SaveChangesAsync(); // Salvar para obter o ID

                            // Adicionar fonte
                            var source = new FactSource
                            {
                                FactId = fact.Id,
                                Type = SmartAI.Models.SourceType.WEB, // Especificar namespace completo
                                Identifier = investigationResult.SourceName ?? "Web Search",
                                URL = investigationResult.SourceUrl,
                                TrustWeight = 0.5,
                                CollectedAt = DateTime.Now
                            };
                            _context.FactSources.Add(source);

                            // Adicionar histórico
                            var history = new FactHistory
                            {
                                FactId = fact.Id,
                                Version = 1,
                                NewSubject = subject,
                                NewRelation = relation,
                                NewObject = obj,
                                NewConfidence = 0.0,
                                NewStatus = FactStatus.CANDIDATE,
                                ChangedBy = "system",
                                ChangedAt = DateTime.Now,
                                Reason = "Candidate fact from web search",
                                ChangeType = ChangeType.CREATED
                            };
                            _context.FactHistory.Add(history);

                            await _context.SaveChangesAsync();
                            savedFactsCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao salvar fato candidato: {ex.Message}");
                            AddSystemMessage($"⚠️ Erro ao salvar fato: {ex.Message}");
                        }
                    }

                    AddSystemMessage($"💾 {savedFactsCount} fatos candidatos salvos para validação futura.");
                }
                else
                {
                    var responseText = $"🔍 Pesquisei sobre '{query}', mas não encontrei fatos estruturados.\n\n";
                    responseText += $"Resumo da busca:\n{investigationResult.RawText}\n\n";
                    responseText += $"Fonte: {investigationResult.SourceName}";

                    AddAssistantMessage(responseText);
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro na investigação: {ex.Message}");
                Console.WriteLine($"Detalhes do erro: {ex.InnerException?.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
            }
        }


        private async Task CheckAndPresentConflicts()
        {
            var conflicts = await _conflictService.DetectConflicts();

            if (conflicts.Any())
            {
                ConflictIndicator.Text = $"Conflitos: {conflicts.Count}";
                ConflictsCountText.Text = conflicts.Count.ToString();

                var result = MessageBox.Show(
                    $"⚠️ Detectei {conflicts.Count} conflito(s) no conhecimento!\n\n" +
                    $"Deseja revisar agora?",
                    "Conflitos Detectados",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await ShowConflictResolutionDialog(conflicts);
                }
            }
        }

        private async Task ShowConflictResolutionDialog(System.Collections.Generic.List<FactConflict> conflicts)
        {
            var dialog = new ConflictResolutionDialog(conflicts)
            {
                Owner = this
            };

            dialog.ShowDialog();
            await UpdateStatistics();
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
                await InvestigateWeb(query);
            }
        }

        private async void ShowValidatedFacts_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Carregando fatos validados...";

            try
            {
                var facts = await _context.Facts
                    .Where(f => f.Status == FactStatus.VALIDATED)
                    .Include(f => f.Sources)
                    .Include(f => f.History)
                    .OrderByDescending(f => f.ValidatedAt)
                    .ToListAsync();

                var viewer = new FactsViewerDialog(facts, "Fatos Validados")
                {
                    Owner = this
                };

                viewer.ShowDialog();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro ao carregar fatos: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
            }
        }

        private async void ShowCandidateFacts_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Carregando fatos candidatos...";

            try
            {
                var candidates = await _factService.GetCandidateFacts();

                if (!candidates.Any())
                {
                    MessageBox.Show(
                        "Não há fatos candidatos aguardando validação.",
                        "Nenhum Candidato",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var viewer = new FactsViewerDialog(candidates, "Fatos Candidatos (Não Validados)")
                {
                    Owner = this
                };

                viewer.ShowDialog();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro ao carregar candidatos: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
            }
        }

        private async void CheckConflicts_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Detectando conflitos...";

            try
            {
                var conflicts = await _conflictService.DetectConflicts();

                if (!conflicts.Any())
                {
                    AddSystemMessage("✅ Nenhum conflito detectado no conhecimento!");
                    MessageBox.Show(
                        "Não há conflitos no conhecimento validado.",
                        "Sem Conflitos",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                AddSystemMessage($"⚠️ Detectados {conflicts.Count} conflito(s)!");
                await ShowConflictResolutionDialog(conflicts);
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro ao detectar conflitos: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
            }
        }

        private async void ShowLearningHistory_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Carregando histórico...";

            try
            {
                var history = await _context.FactHistory
                    .Include(h => h.Fact)
                    .OrderByDescending(h => h.ChangedAt)
                    .Take(100)
                    .ToListAsync();

                var viewer = new HistoryViewerDialog(history)
                {
                    Owner = this
                };

                viewer.ShowDialog();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro ao carregar histórico: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
            }
        }

        private async void ShowStats_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Calculando estatísticas...";

            try
            {
                var stats = await CalculateDetailedStats();
                var statsDialog = new StatsDialog(stats)
                {
                    Owner = this
                };

                statsDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro ao calcular estatísticas: {ex.Message}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
            }
        }

        private async Task<DetailedStats> CalculateDetailedStats()
        {
            return new DetailedStats
            {
                TotalValidated = await _context.Facts.CountAsync(f => f.Status == FactStatus.VALIDATED),
                TotalCandidates = await _context.Facts.CountAsync(f => f.Status == FactStatus.CANDIDATE),
                TotalRejected = await _context.Facts.CountAsync(f => f.Status == FactStatus.REJECTED),
                TotalDeprecated = await _context.Facts.CountAsync(f => f.Status == FactStatus.DEPRECATED),
                TotalConflicts = await _context.FactConflicts.CountAsync(c => !c.IsResolved),
                AverageConfidence = await _context.Facts
                    .Where(f => f.Status == FactStatus.VALIDATED)
                    .AverageAsync(f => (double?)f.Confidence) ?? 0.0,
                ValidationStats = await _validationService.GetValidationStats(),
                SourceDistribution = await _context.FactSources
                    .GroupBy(s => s.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type.ToString(), x => x.Count)
            };
        }

        private async void RunTests_Click(object sender, RoutedEventArgs e)
        {
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
            testWindow.Show();

            var originalOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                textBox.Text = "Iniciando testes...\n\n";
                await Task.Run(async () => await SmartAI.Tests.EpistemicSystemTests.RunAllTests());

                textBox.Text = writer.ToString();
                scrollViewer.ScrollToEnd();
            }
            catch (Exception ex)
            {
                textBox.Text += $"\n\n❌ ERRO AO EXECUTAR TESTES:\n{ex.Message}\n\n{ex.StackTrace}";
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Tem certeza que deseja limpar o chat?\n(O conhecimento será preservado)",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ChatPanel.Children.Clear();
                AddSystemMessage("Chat limpo. Conhecimento preservado.");
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "🧠 Anacreon - Sistema Cognitivo Epistêmico\n\n" +
                "Versão: 2.0 (Refatoração Epistêmica)\n\n" +
                "Características:\n" +
                "• Conhecimento sempre rastreável\n" +
                "• Validação humana obrigatória\n" +
                "• Confiança sempre < 100%\n" +
                "• Detecção automática de conflitos\n" +
                "• Histórico imutável de mudanças\n" +
                "• Separação código vs conhecimento factual\n\n" +
                "Desenvolvido como experimento em IA epistêmica.",
                "Sobre o Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowCommands_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage(
                "📖 COMANDOS DISPONÍVEIS\n\n" +
                "🔍 CONSULTA:\n" +
                "• 'O que é X?' - Buscar conhecimento validado\n" +
                "• 'Pesquisar: termo' - Investigar na web\n\n" +
                "📚 ENSINO:\n" +
                "• 'X é Y' - Propor novo fato (requer validação)\n" +
                "• 'X tem Y' - Adicionar propriedade\n\n" +
                "⚙️ SISTEMA:\n" +
                "• 'verificar conflitos' - Detectar contradições\n" +
                "• 'estatísticas' - Ver métricas do sistema\n\n" +
                "💡 DICA: Todo conhecimento passa por validação humana!");
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
                Text = "Anacreon",
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
            try
            {
                var validatedCount = await _context.Facts.CountAsync(f => f.Status == FactStatus.VALIDATED);
                var candidateCount = await _context.Facts.CountAsync(f => f.Status == FactStatus.CANDIDATE);
                var conflictCount = await _context.FactConflicts.CountAsync(c => !c.IsResolved);

                FactCount.Text = $"Fatos: {validatedCount}";
                ValidatedFactsCountText.Text = validatedCount.ToString();
                CandidatesCountText.Text = candidateCount.ToString();
                ConflictIndicator.Text = $"Conflitos: {conflictCount}";
                ConflictsCountText.Text = conflictCount.ToString();

                // Atualizar aprendizado recente
                RecentLearningPanel.Children.Clear();
                var recentFacts = await _context.Facts
                    .Where(f => f.Status == FactStatus.VALIDATED)
                    .OrderByDescending(f => f.ValidatedAt)
                    .Take(5)
                    .ToListAsync();

                if (recentFacts.Any())
                {
                    foreach (var fact in recentFacts)
                    {
                        var textBlock = new TextBlock
                        {
                            Text = $"• {fact.Subject} {fact.Relation} {fact.Object}",
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
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar estatísticas: {ex.Message}");
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

    public class DetailedStats
    {
        public int TotalValidated { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalRejected { get; set; }
        public int TotalDeprecated { get; set; }
        public int TotalConflicts { get; set; }
        public double AverageConfidence { get; set; }
        public ValidationStats? ValidationStats { get; set; }
        public System.Collections.Generic.Dictionary<string, int>? SourceDistribution { get; set; }
    }
}
