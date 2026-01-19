using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SmartAI.AI;
using SmartAI.Data;
using SmartAI.Models;

namespace SmartAI
{
    public partial class MainWindow : Window
    {
        private AIContext _context;
        private IntelligenceEngine _ai;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAI();
        }

        private void InitializeAI()
        {
            try
            {
                _context = new AIContext();
                _context.Database.EnsureCreated();

                _ai = new IntelligenceEngine(_context);

                AddAIMessage("👋 Olá! Sou uma IA que aprende com você. Me faça perguntas ou me ensine coisas novas!");
                AddAIMessage("💡 Exemplos:\n• O que é cachorro?\n• Baleia é um mamífero\n• Python é uma linguagem de programação");

                UpdateStats();
                txtInput.Focus();
            }
            catch (Exception ex)
            {
                string errorMessage = $"Erro ao inicializar: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nDetalhes: {ex.InnerException.Message}";
                }
                MessageBox.Show(errorMessage, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                _ai = null;
                txtInput.IsEnabled = false;
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            ProcessInput();
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtInput.Text))
            {
                ProcessInput();
            }
        }

        private void ProcessInput()
        {
            var input = txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
                return;

            // Mostra mensagem do usuário
            AddUserMessage(input);
            txtInput.Clear();

            // Processa com a IA
            try
            {
                var response = _ai.ProcessQuestion(input);

                // Salva conversa
                var conversation = new Conversation
                {
                    UserMessage = input,
                    AIResponse = response.Message,
                    LearnedSomething = response.Learned
                };
                _context.Conversations.Add(conversation);
                _context.SaveChanges();

                // Mostra resposta
                AddAIMessage(response.Message, response.Confidence, response.Learned);

                // Se aprendeu algo, mostra sugestão
                if (response.Learned)
                {
                    txtStatus.Text = "✓ Aprendi algo novo! Obrigado!";
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(87, 242, 135));
                }
                else if (response.NeedsMoreInfo)
                {
                    txtStatus.Text = "❓ Preciso de mais informações...";
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(254, 231, 92));

                    if (!string.IsNullOrWhiteSpace(response.SuggestedLearning))
                    {
                        AddAIMessage($"💡 Sugestão: {response.SuggestedLearning}");
                    }
                }
                else
                {
                    txtStatus.Text = $"Confiança: {response.Confidence:P0}";
                    txtStatus.Foreground = Brushes.White;
                }

                UpdateStats();
            }
            catch (Exception ex)
            {
                AddAIMessage($"❌ Erro: {ex.Message}");
            }
        }

        private void AddUserMessage(string message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("MessageBubbleUser")
            };

            var stack = new StackPanel();

            var label = new TextBlock
            {
                Text = "Você",
                Foreground = new SolidColorBrush(Color.FromRgb(185, 187, 190)),
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stack.Children.Add(label);

            var text = new TextBlock
            {
                Text = message,
                Style = (Style)FindResource("MessageText")
            };
            stack.Children.Add(text);

            border.Child = stack;
            chatPanel.Children.Add(border);

            ScrollToBottom();
        }

        private void AddAIMessage(string message, double confidence = 1.0, bool learned = false)
        {
            var border = new Border
            {
                Style = (Style)FindResource("MessageBubbleAI")
            };

            if (learned)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(87, 242, 135));
                border.BorderThickness = new Thickness(2);
            }

            var stack = new StackPanel();

            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var label = new TextBlock
            {
                Text = "🧠 Smart AI",
                Foreground = new SolidColorBrush(Color.FromRgb(88, 101, 242)),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            header.Children.Add(label);

            if (confidence < 1.0 && confidence > 0)
            {
                var confLabel = new TextBlock
                {
                    Text = $" • {confidence:P0} confiança",
                    Foreground = new SolidColorBrush(Color.FromRgb(185, 187, 190)),
                    FontSize = 10,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                header.Children.Add(confLabel);
            }

            stack.Children.Add(header);

            var text = new TextBlock
            {
                Text = message,
                Style = (Style)FindResource("MessageText")
            };
            stack.Children.Add(text);

            border.Child = stack;
            chatPanel.Children.Add(border);

            ScrollToBottom();
        }

        private void UpdateStats()
        {
            var instanceCount = _context.Instances.Count();
            var conversationCount = _context.Conversations.Count();
            var learnedCount = _context.Conversations.Count(c => c.LearnedSomething);

            txtKnowledgeCount.Text = $"{instanceCount} itens";
            txtConversationCount.Text = conversationCount.ToString();
            txtLearnedCount.Text = $"{learnedCount} coisas";
        }

        private void ScrollToBottom()
        {
            scrollChat.ScrollToBottom();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}