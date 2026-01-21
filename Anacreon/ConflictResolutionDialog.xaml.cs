using Microsoft.EntityFrameworkCore;
using SmartAI.Models;
using SmartAI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SmartAI
{
    public partial class ConflictResolutionDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<ConflictViewModel> _conflicts;
        public ObservableCollection<ConflictViewModel> Conflicts
        {
            get => _conflicts;
            set
            {
                _conflicts = value;
                OnPropertyChanged(nameof(Conflicts));
            }
        }

        private readonly ConflictDetectionService _conflictService;
        private int _resolvedCount = 0;

        public ConflictResolutionDialog(List<FactConflict> conflicts)
        {
            InitializeComponent();
            DataContext = this;

            _conflictService = new ConflictDetectionService(
                new SmartAI.Data.AIContext(
                    new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SmartAI.Data.AIContext>()
                        .UseSqlite("Data Source=smartai.db")
                        .Options
                )
            );

            Conflicts = new ObservableCollection<ConflictViewModel>();

            foreach (var conflict in conflicts)
            {
                Conflicts.Add(new ConflictViewModel
                {
                    Conflict = conflict,
                    Subject = conflict.Subject,
                    Relation = conflict.Relation,
                    FactAObject = conflict.FactA?.Object ?? "Unknown",
                    FactBObject = conflict.FactB?.Object ?? "Unknown",
                    FactAConfidence = conflict.FactA?.Confidence ?? 0.0,
                    FactBConfidence = conflict.FactB?.Confidence ?? 0.0,
                    FactASource = conflict.FactA?.Sources.FirstOrDefault()?.Type.ToString() ?? "Unknown",
                    FactBSource = conflict.FactB?.Sources.FirstOrDefault()?.Type.ToString() ?? "Unknown"
                });
            }

            UpdateTitle();
        }

        private async void KeepFactA_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConflictViewModel viewModel)
            {
                var result = MessageBox.Show(
                    $"Manter '{viewModel.FactAObject}' e deprecar '{viewModel.FactBObject}'?",
                    "Confirmar Resolução",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _conflictService.ResolveConflict(
                            viewModel.Conflict.Id,
                            ConflictResolution.KEEP_FACT_A,
                            "user",
                            "Usuário escolheu manter Fact A"
                        );

                        Conflicts.Remove(viewModel);
                        _resolvedCount++;
                        UpdateTitle();
                        ShowTemporaryMessage($"✓ Conflito resolvido - Mantido: {viewModel.FactAObject}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao resolver conflito: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void KeepFactB_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConflictViewModel viewModel)
            {
                var result = MessageBox.Show(
                    $"Manter '{viewModel.FactBObject}' e deprecar '{viewModel.FactAObject}'?",
                    "Confirmar Resolução",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _conflictService.ResolveConflict(
                            viewModel.Conflict.Id,
                            ConflictResolution.KEEP_FACT_B,
                            "user",
                            "Usuário escolheu manter Fact B"
                        );

                        Conflicts.Remove(viewModel);
                        _resolvedCount++;
                        UpdateTitle();
                        ShowTemporaryMessage($"✓ Conflito resolvido - Mantido: {viewModel.FactBObject}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao resolver conflito: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void KeepBoth_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConflictViewModel viewModel)
            {
                var result = MessageBox.Show(
                    $"Manter ambos os fatos?\n\n" +
                    $"Ambos terão confiança reduzida em 15% para refletir a incerteza.",
                    "Confirmar Resolução",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _conflictService.ResolveConflict(
                            viewModel.Conflict.Id,
                            ConflictResolution.KEEP_BOTH,
                            "user",
                            "Usuário escolheu manter ambos (contextualmente válidos)"
                        );

                        Conflicts.Remove(viewModel);
                        _resolvedCount++;
                        UpdateTitle();
                        ShowTemporaryMessage("✓ Conflito resolvido - Ambos mantidos com confiança reduzida");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao resolver conflito: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DeprecateBoth_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConflictViewModel viewModel)
            {
                var result = MessageBox.Show(
                    $"Deprecar ambos os fatos?\n\n" +
                    $"Ambos serão marcados como obsoletos e removidos do conhecimento ativo.",
                    "Confirmar Resolução",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _conflictService.ResolveConflict(
                            viewModel.Conflict.Id,
                            ConflictResolution.DEPRECATE_BOTH,
                            "user",
                            "Usuário escolheu deprecar ambos"
                        );

                        Conflicts.Remove(viewModel);
                        _resolvedCount++;
                        UpdateTitle();
                        ShowTemporaryMessage("✓ Conflito resolvido - Ambos deprecados");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao resolver conflito: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (Conflicts.Any())
            {
                var result = MessageBox.Show(
                    $"Ainda há {Conflicts.Count} conflito(s) não resolvido(s).\n\n" +
                    $"Deseja sair mesmo assim?",
                    "Conflitos Pendentes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = _resolvedCount > 0;
            Close();
        }

        private void UpdateTitle()
        {
            var remaining = Conflicts.Count;
            SubtitleText.Text = remaining > 0
                ? $"{remaining} conflito(s) detectado(s) - Resolvidos: {_resolvedCount}"
                : $"Todos os conflitos foram resolvidos! Total: {_resolvedCount}";
        }

        private void ShowTemporaryMessage(string message)
        {
            InfoText.Text = message;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                InfoText.Text = "ℹ️ IMPORTANTE: Conflitos indicam contradições no conhecimento. Escolha a versão correta ou mantenha ambas se forem contextuais.";
                timer.Stop();
            };
            timer.Start();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ConflictViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public FactConflict Conflict { get; set; } = new FactConflict();

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

        private string _factAObject = string.Empty;
        public string FactAObject
        {
            get => _factAObject;
            set
            {
                _factAObject = value;
                OnPropertyChanged(nameof(FactAObject));
            }
        }

        private string _factBObject = string.Empty;
        public string FactBObject
        {
            get => _factBObject;
            set
            {
                _factBObject = value;
                OnPropertyChanged(nameof(FactBObject));
            }
        }

        private double _factAConfidence;
        public double FactAConfidence
        {
            get => _factAConfidence;
            set
            {
                _factAConfidence = value;
                OnPropertyChanged(nameof(FactAConfidence));
                OnPropertyChanged(nameof(FactAConfidencePercent));
            }
        }

        private double _factBConfidence;
        public double FactBConfidence
        {
            get => _factBConfidence;
            set
            {
                _factBConfidence = value;
                OnPropertyChanged(nameof(FactBConfidence));
                OnPropertyChanged(nameof(FactBConfidencePercent));
            }
        }

        public string FactAConfidencePercent => $"{FactAConfidence:P0}";
        public string FactBConfidencePercent => $"{FactBConfidence:P0}";

        private string _factASource = string.Empty;
        public string FactASource
        {
            get => _factASource;
            set
            {
                _factASource = value;
                OnPropertyChanged(nameof(FactASource));
            }
        }

        private string _factBSource = string.Empty;
        public string FactBSource
        {
            get => _factBSource;
            set
            {
                _factBSource = value;
                OnPropertyChanged(nameof(FactBSource));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}