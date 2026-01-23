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
        private enum PendingChoice
        {
            None,
            System,
            Investigation
        }

        private PendingChoice _pendingChoice = PendingChoice.None;
        private Func<int, Task>? _choiceHandler;
        private bool _inputLocked = false;



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
            if (_inputLocked && _pendingChoice == PendingChoice.None)
            {
                InputTextBox.Clear();
                return;
            }



            if (_pendingChoice != PendingChoice.None)
            {
                AddUserMessage(input);

                if (int.TryParse(input, out int choice) && (choice == 1 || choice == 2))
                {
                    var handler = _choiceHandler;

                    _pendingChoice = PendingChoice.None;
                    _choiceHandler = null;
                    _inputLocked = false;

                    if (handler != null)
                        await handler(choice);
                }
                else
                {
                    AddSystemMessage("Escolha inválida. Digite 1 ou 2.");
                }

                InputTextBox.Clear();
                return;
            }




            AddUserMessage(input);
            InputTextBox.Clear();

            StatusText.Text = "Processando...";
            SendButton.IsEnabled = false;

            try
            {
                // USAR COGNITIVE ENGINE
                var response = await _cognitiveEngine.Process(input);

                if (
    response.RequiresAction &&
    response.SuggestedMode == CognitiveMode.INVESTIGATION &&
    _pendingChoice == PendingChoice.None
)
                {
                    AddAssistantMessage(response.Text);

                    _pendingChoice = PendingChoice.Investigation;
                    _inputLocked = true;

                    _choiceHandler = async (choice) =>
                    {
                        try
                        {
                            if (choice == 1)
                            {
                                var dialog = new SearchDialog { Owner = this };
                                if (dialog.ShowDialog() == true &&
                                    !string.IsNullOrWhiteSpace(dialog.SearchQuery))
                                {
                                    AddUserMessage(dialog.SearchQuery);
                                    await InvestigateWeb(dialog.SearchQuery);
                                }
                            }
                            else
                            {
                                AddAssistantMessage("Pode ensinar.");
                            }
                        }
                        finally
                        {
                            _inputLocked = false;
                        }
                    };



                    return;
                }


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
                    if (!string.IsNullOrEmpty(investigationResult.SourceUrl))
                    {
                        responseText += $"🔗 {investigationResult.SourceUrl}\n";
                    }
                    responseText += $"\n⚠️ **IMPORTANTE**: Estes são CANDIDATOS, não fatos validados.\n";

                    AddAssistantMessage(responseText);


                    AddAssistantMessage(
                    "⚠️ Foram encontrados fatos candidatos.\n" +
                    "Use o menu **Conhecimento → Fatos Candidatos** para validá-los quando desejar."
                    );
                }
                else if (investigationResult.Success)
                {
                    var responseText = $"🔍 Pesquisei sobre '{query}', mas não encontrei fatos estruturados.\n\n";
                    if (!string.IsNullOrEmpty(investigationResult.RawText))
                    {
                        responseText += $"Resumo:\n{investigationResult.RawText}\n\n";
                    }
                    responseText += $"Fonte: {investigationResult.SourceName}";

                    AddAssistantMessage(responseText);
                }
                else
                {
                    AddSystemMessage($"❌ Não consegui encontrar informações sobre '{query}'.");
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro na investigação: {ex.Message}");
                Console.WriteLine($"Detalhes do erro: {ex.StackTrace}");
            }
            finally
            {
                StatusText.Text = "Sistema pronto";
                await UpdateStatistics();
            }
        }

        private async Task<List<Fact>> SaveCandidateFacts(InvestigationResult investigationResult)
        {
            var savedFacts = new List<Fact>();

            foreach (var candidate in investigationResult.CandidateFacts)
            {
                try
                {
                    // Validar dados antes de salvar
                    var subject = string.IsNullOrWhiteSpace(candidate.Subject) ? "Desconhecido" : candidate.Subject.Trim();
                    var relation = string.IsNullOrWhiteSpace(candidate.Relation) ? "tem" : candidate.Relation.Trim();
                    var obj = string.IsNullOrWhiteSpace(candidate.Object) ? "informação desconhecida" : candidate.Object.Trim();

                    // Verificar se já existe
                    var existing = await _context.Facts
                        .Where(f => f.Subject == subject &&
                                    f.Relation == relation &&
                                    f.Object == obj &&
                                    f.Status == FactStatus.CANDIDATE)
                        .FirstOrDefaultAsync();

                    if (existing != null)
                    {
                        Console.WriteLine($"Fato candidato já existe: {subject} {relation} {obj}");
                        savedFacts.Add(existing);
                        continue;
                    }

                    // Criar fato candidato usando o serviço
                    var fact = await _factService.CreateCandidateFact(
                        subject,
                        relation,
                        obj,
                        candidate.Sources.FirstOrDefault()?.Type ?? SourceType.WEB,
                        investigationResult.SourceName ?? "Web Search",
                        investigationResult.SourceUrl
                    );

                    savedFacts.Add(fact);
                    Console.WriteLine($"Fato candidato salvo: {subject} {relation} {obj}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao salvar fato candidato: {ex.Message}");
                    AddSystemMessage($"⚠️ Erro ao salvar um fato: {ex.Message}");
                }
            }

            return savedFacts;
        }

        private async Task PresentValidationDialogDirect(List<Fact> facts)
        {
            try
            {
                // Recarregar os fatos do banco com todas as relações
                var factIds = facts.Select(f => f.Id).ToList();
                var reloadedFacts = await _context.Facts
                    .Where(f => factIds.Contains(f.Id))
                    .Include(f => f.Sources)
                    .Include(f => f.History)
                    .ToListAsync();

                var dialog = new ValidationDialog(reloadedFacts)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusText.Text = "Processando validação...";

                    // Criar uma sessão temporária
                    var session = await _validationService.StartValidationSession(
                        "Direct validation from web search",
                        reloadedFacts
                    );

                    // Processar decisões
                    var result = await _validationService.ProcessUserDecisions(
                        session.Id,
                        dialog.Decisions,
                        "user"
                    );

                    AddSystemMessage(result.GetSummary());
                    await UpdateStatistics();

                    // Verificar conflitos
                    await CheckAndPresentConflicts();
                }
                else
                {
                    AddSystemMessage("Validação cancelada. Os fatos continuam salvos como candidatos.");
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"❌ Erro ao validar fatos: {ex.Message}");
                Console.WriteLine($"Erro detalhado: {ex.StackTrace}");
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

                var dialog = new ConflictResolutionDialog(conflicts)
                {
                    Owner = this
                };

                dialog.ShowDialog();
                await UpdateStatistics();

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
                    AddAssistantMessage("ℹ️ Não há fatos candidatos aguardando validação.");

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
            AddSystemMessage(
               "⚠️ Você deseja limpar o chat?\n\n" +
               "1️⃣ Sim, limpar o chat\n" +
               "2️⃣ Não, cancelar\n\n" +
               "Digite **1** ou **2**."
);

            _pendingChoice = PendingChoice.System;
            _inputLocked = true;
            _choiceHandler = async (choice) =>
            {
                if (choice == 1)
                {
                    ChatPanel.Children.Clear();
                    AddSystemMessage("🧹 Chat limpo com sucesso.");
                }
                else
                {
                    AddSystemMessage("✅ Operação cancelada. Sistema ativo.");
                }

                _inputLocked = false;
                await Task.CompletedTask;
            };





        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AddSystemMessage(
                "🧠 **Anacreon — Sistema Cognitivo Epistêmico**\n\n" +
                "Versão: 2.0 (Refatoração Epistêmica)\n\n" +
                "Características:\n" +
                "• Conhecimento rastreável\n" +
                "• Validação humana obrigatória\n" +
                "• Confiança sempre < 100%\n" +
                "• Detecção automática de conflitos\n" +
                "• Histórico imutável\n\n" +
                "Este sistema opera inteiramente dentro do ambiente conversacional."
);

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
            AddSystemMessage(
                "⚠️ Deseja realmente sair do sistema?\n\n" +
                "1️⃣ Sim, sair agora\n" +
                "2️⃣ Não, continuar usando\n\n" +
                "Digite **1** ou **2**."
                            );

            _pendingChoice = PendingChoice.System;
            _inputLocked = true;
            _choiceHandler = async (choice) =>
            {
                if (choice == 1)
                {
                    AddSystemMessage("👋 Encerrando o sistema...");
                    Application.Current.Shutdown();
                }
                else
                {
                    AddSystemMessage("✅ Operação cancelada. Sistema ativo.");
                    _inputLocked = false;
                }

                await Task.CompletedTask;
            };



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
