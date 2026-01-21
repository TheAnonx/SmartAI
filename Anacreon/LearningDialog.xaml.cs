using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SmartAI
{
    /// <summary>
    /// Diálogo legado - mantido para compatibilidade
    /// Use ValidationDialog para o novo sistema epistêmico
    /// </summary>
    public partial class LearningDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<FactItem> _facts = new ObservableCollection<FactItem>();
        public ObservableCollection<FactItem> Facts
        {
            get => _facts;
            set
            {
                _facts = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Facts)));
            }
        }

        public List<string> SelectedFacts
        {
            get
            {
                return Facts.Where(f => f.IsSelected).Select(f => f.Text).ToList();
            }
        }

        private bool _dialogResult = false;
        public new bool DialogResult
        {
            get => _dialogResult;
            private set => _dialogResult = value;
        }

        public LearningDialog(List<string> facts)
        {
            InitializeComponent();
            DataContext = this;

            Facts = new ObservableCollection<FactItem>();

            for (int i = 0; i < facts.Count; i++)
            {
                var factText = facts[i].Trim();
                var cleanedFact = System.Text.RegularExpressions.Regex.Replace(
                    factText, @"^[\d\•\-\*]+\.?\s*", "").Trim();

                if (!string.IsNullOrWhiteSpace(cleanedFact))
                {
                    Facts.Add(new FactItem
                    {
                        Text = cleanedFact,
                        IsSelected = true
                    });
                }
            }

            if (Facts.Count > 0)
            {
                SubtitleText.Text = $"Selecione quais dos {Facts.Count} fato(s) você quer que eu aprenda:";
            }
            else
            {
                SubtitleText.Text = "Nenhum fato disponível para aprender.";
                LearnButton.IsEnabled = false;
            }

            UpdateLearnButton();
        }

        private void FactCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateLearnButton();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var fact in Facts)
            {
                fact.IsSelected = true;
            }
            UpdateLearnButton();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var fact in Facts)
            {
                fact.IsSelected = false;
            }
            UpdateLearnButton();
        }

        private void Learn_Click(object sender, RoutedEventArgs e)
        {
            var selectedCount = Facts.Count(f => f.IsSelected);

            if (selectedCount == 0)
            {
                MessageBox.Show(
                    "Por favor, selecione pelo menos um fato para aprender!",
                    "Nenhum fato selecionado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Deseja aprender {selectedCount} fato(s) selecionado(s)?",
                "Confirmar Aprendizado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = true;
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void UpdateLearnButton()
        {
            var selectedCount = Facts.Count(f => f.IsSelected);

            LearnButton.IsEnabled = selectedCount > 0;

            if (selectedCount > 0)
            {
                LearnButton.Content = $"🧠 Aprender {selectedCount} Fato{(selectedCount > 1 ? "s" : "")} Selecionado{(selectedCount > 1 ? "s" : "")}";
            }
            else
            {
                LearnButton.Content = "🧠 Aprender Selecionados";
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!DialogResult)
            {
                DialogResult = false;
            }
            base.OnClosing(e);
        }
    }

    public class FactItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}