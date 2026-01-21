// Arquivo completo - substituir todo o conteúdo
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartAI.Models;
using SmartAI.Services;

namespace SmartAI
{
    public partial class ValidationDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<CandidateFactViewModel> _candidateFacts;
        public ObservableCollection<CandidateFactViewModel> CandidateFacts
        {
            get => _candidateFacts;
            set
            {
                _candidateFacts = value;
                OnPropertyChanged(nameof(CandidateFacts));
            }
        }

        public List<FactDecision> Decisions { get; private set; } = new List<FactDecision>();

        private int _approvedCount = 0;
        private int _rejectedCount = 0;
        private int _editedCount = 0;

        private bool _dialogResult = false;
        public new bool DialogResult
        {
            get => _dialogResult;
            private set => _dialogResult = value;
        }

        public ValidationDialog(List<Fact> candidateFacts)
        {
            InitializeComponent();
            DataContext = this;

            CandidateFacts = new ObservableCollection<CandidateFactViewModel>();

            foreach (var fact in candidateFacts)
            {
                CandidateFacts.Add(new CandidateFactViewModel
                {
                    Fact = fact,
                    Subject = fact.Subject,
                    Relation = fact.Relation,
                    Object = fact.Object,
                    Sources = fact.Sources.ToList()
                });
            }

            UpdateSubtitle();
            UpdateStats();
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CandidateFactViewModel viewModel)
            {
                // Adicionar decisão de aprovação
                var decision = new FactDecision
                {
                    Fact = viewModel.Fact,
                    Action = ValidationAction.APPROVE,
                    Confidence = 0.90
                };

                Decisions.Add(decision);
                _approvedCount++;

                // Remover da lista
                CandidateFacts.Remove(viewModel);
                UpdateStats();
                UpdateSubtitle();

                // Feedback visual
                ShowTemporaryMessage("✓ Fato aprovado", "#4CAF50");
            }
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CandidateFactViewModel viewModel)
            {
                var decision = new FactDecision
                {
                    Fact = viewModel.Fact,
                    Action = ValidationAction.REJECT,
                    Reason = "Rejeitado pelo usuário"
                };

                Decisions.Add(decision);
                _rejectedCount++;

                CandidateFacts.Remove(viewModel);
                UpdateStats();
                UpdateSubtitle();

                ShowTemporaryMessage("✗ Fato rejeitado", "#F44336");
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CandidateFactViewModel viewModel)
            {
                // Encontrar o item visual correspondente
                var item = FindVisualParent<Border>(button);
                if (item != null)
                {
                    // Encontrar o painel de edição
                    var editPanelBorder = FindVisualChild<Border>(item, "EditPanelBorder");
                    if (editPanelBorder != null)
                    {
                        // Toggle visibilidade
                        if (editPanelBorder.Visibility == Visibility.Collapsed)
                        {
                            editPanelBorder.Visibility = Visibility.Visible;
                            button.Content = "💾 Salvar Edição";
                        }
                        else
                        {
                            // Encontrar os elementos de edição
                            var subjectBox = FindVisualChild<TextBox>(editPanelBorder, "EditSubject");
                            var relationBox = FindVisualChild<TextBox>(editPanelBorder, "EditRelation");
                            var objectBox = FindVisualChild<TextBox>(editPanelBorder, "EditObject");
                            var confidenceBox = FindVisualChild<TextBox>(editPanelBorder, "EditConfidence");

                            if (subjectBox != null && relationBox != null && objectBox != null && confidenceBox != null)
                            {
                                // Validar confiança
                                if (!double.TryParse(confidenceBox.Text, out double confidence)
                                    || confidence < 0 || confidence >= 1.0)
                                {
                                    MessageBox.Show(
                                        "Confiança deve ser entre 0.0 e 0.99",
                                        "Valor Inválido",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                                    return;
                                }

                                // Criar decisão de edição
                                var decision = new FactDecision
                                {
                                    Fact = viewModel.Fact,
                                    Action = ValidationAction.EDIT,
                                    EditedSubject = subjectBox.Text,
                                    EditedRelation = relationBox.Text,
                                    EditedObject = objectBox.Text,
                                    Confidence = confidence
                                };

                                Decisions.Add(decision);
                                _editedCount++;

                                CandidateFacts.Remove(viewModel);
                                UpdateStats();
                                UpdateSubtitle();

                                ShowTemporaryMessage("✏️ Fato editado e aprovado", "#FF9800");

                                // Esconder o painel de edição
                                editPanelBorder.Visibility = Visibility.Collapsed;
                                button.Content = "✏️ Editar";
                            }
                        }
                    }
                }
            }
        }

        private void Complete_Click(object sender, RoutedEventArgs e)
        {
            if (Decisions.Count == 0)
            {
                var result = MessageBox.Show(
                    "Você não tomou nenhuma decisão sobre os fatos candidatos.\n\n" +
                    "Deseja cancelar a validação?",
                    "Nenhuma Decisão",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = false;
                    Close();
                }
                return;
            }

            var summary = $"Você processou {Decisions.Count} fato(s):\n\n" +
                         $"✓ Aprovados: {_approvedCount}\n" +
                         $"✗ Rejeitados: {_rejectedCount}\n" +
                         $"✏️ Editados: {_editedCount}\n\n" +
                         $"Confirmar validação?";

            var confirmResult = MessageBox.Show(
                summary,
                "Confirmar Validação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult == MessageBoxResult.Yes)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Deseja cancelar a validação?\n\nTodas as decisões serão descartadas.",
                "Cancelar Validação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Cancel_Click(sender, e);
        }

        private void UpdateSubtitle()
        {
            var remaining = CandidateFacts.Count;
            SubtitleText.Text = remaining > 0
                ? $"{remaining} fato(s) candidato(s) aguardando sua decisão"
                : "Todos os fatos foram processados";
        }

        private void UpdateStats()
        {
            StatsText.Text = $"Aprovados: {_approvedCount} | Rejeitados: {_rejectedCount} | Editados: {_editedCount}";
        }

        private void ShowTemporaryMessage(string message, string color)
        {
            // Feedback simples
            var originalText = InfoText.Text;
            InfoText.Text = message;

            // Restaurar após 2 segundos
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) =>
            {
                InfoText.Text = "ℹ️ IMPORTANTE: Fatos candidatos NÃO são conhecimento validado. Somente após sua aprovação eles serão persistidos no sistema.";
                timer.Stop();
            };
            timer.Start();
        }

        // Helper methods para encontrar elementos visuais
        private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }

        private T? FindVisualChild<T>(DependencyObject parent, string name = "") where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && (string.IsNullOrEmpty(name) || typedChild.Name == name))
                    return typedChild;

                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CandidateFactViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Fact Fact { get; set; } = new Fact();

        private string _subject = string.Empty;
        public string Subject
        {
            get => _subject;
            set
            {
                _subject = value;
                OnPropertyChanged(nameof(Subject));
            }
        }

        private string _relation = string.Empty;
        public string Relation
        {
            get => _relation;
            set
            {
                _relation = value;
                OnPropertyChanged(nameof(Relation));
            }
        }

        private string _object = string.Empty;
        public string Object
        {
            get => _object;
            set
            {
                _object = value;
                OnPropertyChanged(nameof(Object));
            }
        }

        public List<FactSource> Sources { get; set; } = new List<FactSource>();

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
