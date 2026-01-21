using SmartAI.Models;
using SmartAI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SmartAI
{
    public partial class FactsViewerDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<FactViewModel> _facts;
        public ObservableCollection<FactViewModel> Facts
        {
            get => _facts;
            set
            {
                _facts = value;
                OnPropertyChanged(nameof(Facts));
            }
        }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged(nameof(FilterText));
                ApplyFilter();
            }
        }

        private readonly List<Fact> _allFacts;
        private readonly string _dialogTitle;

        public FactsViewerDialog(List<Fact> facts, string title)
        {
            InitializeComponent();
            DataContext = this;

            _allFacts = facts;
            _dialogTitle = title;
            Title = title;

            Facts = new ObservableCollection<FactViewModel>();
            LoadFacts(facts);

            UpdateTitle();
        }

        private void LoadFacts(List<Fact> facts)
        {
            Facts.Clear();

            foreach (var fact in facts)
            {
                Facts.Add(new FactViewModel
                {
                    Fact = fact,
                    Subject = fact.Subject,
                    Relation = fact.Relation,
                    Object = fact.Object,
                    Confidence = fact.Confidence,
                    Status = fact.Status.ToString(),
                    Source = fact.Sources.FirstOrDefault()?.Type.ToString() ?? "Unknown",
                    ValidatedAt = fact.ValidatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                    ApprovedBy = fact.ApprovedBy ?? "N/A"
                });
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                LoadFacts(_allFacts);
            }
            else
            {
                var filtered = _allFacts.Where(f =>
                    f.Subject.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    f.Relation.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    f.Object.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                LoadFacts(filtered);
            }

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            SubtitleText.Text = $"{Facts.Count} fato(s) encontrado(s)";
        }

        private void ShowDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is FactViewModel viewModel)
            {
                var detailsDialog = new FactDetailsDialog(viewModel.Fact)
                {
                    Owner = this
                };

                detailsDialog.ShowDialog();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FactViewModel : INotifyPropertyChanged
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

        private double _confidence;
        public double Confidence
        {
            get => _confidence;
            set
            {
                _confidence = value;
                OnPropertyChanged(nameof(Confidence));
                OnPropertyChanged(nameof(ConfidencePercent));
            }
        }

        public string ConfidencePercent => $"{Confidence:P0}";

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private string _source = string.Empty;
        public string Source
        {
            get => _source;
            set
            {
                _source = value;
                OnPropertyChanged(nameof(Source));
            }
        }

        private string _validatedAt = string.Empty;
        public string ValidatedAt
        {
            get => _validatedAt;
            set
            {
                _validatedAt = value;
                OnPropertyChanged(nameof(ValidatedAt));
            }
        }

        private string _approvedBy = string.Empty;
        public string ApprovedBy
        {
            get => _approvedBy;
            set
            {
                _approvedBy = value;
                OnPropertyChanged(nameof(ApprovedBy));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}