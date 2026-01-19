using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SmartAI
{
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

        public bool DialogResult { get; private set; }

        public LearningDialog(List<string> facts)
        {
            InitializeComponent();

            // Definir o DataContext para this para o binding funcionar
            DataContext = this;

            Facts = new ObservableCollection<FactItem>();

            for (int i = 0; i < facts.Count; i++)
            {
                Facts.Add(new FactItem
                {
                    Text = facts[i], // Removi a numeração manual
                    IsSelected = true // Todos selecionados por padrão
                });
            }

            SubtitleText.Text = $"Selecione quais dos {facts.Count} fatos você quer que eu aprenda:";

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
                    "Selecione pelo menos um fato para aprender!",
                    "Atenção",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateLearnButton()
        {
            var selectedCount = Facts.Count(f => f.IsSelected);
            LearnButton.Content = selectedCount > 0
                ? $"🧠 Aprender {selectedCount} Selecionado{(selectedCount > 1 ? "s" : "")}"
                : "🧠 Aprender Selecionados";
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