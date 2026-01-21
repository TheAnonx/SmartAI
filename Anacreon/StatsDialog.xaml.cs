using System.Linq;
using System.Windows;

namespace SmartAI
{
    public partial class StatsDialog : Window
    {
        private readonly DetailedStats _stats;

        public StatsDialog(DetailedStats stats)
        {
            InitializeComponent();
            _stats = stats;
            LoadStats();
        }

        private void LoadStats()
        {
            var statsText = $"📊 ESTATÍSTICAS DO SISTEMA\n\n";
            statsText += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            statsText += $"CONHECIMENTO\n";
            statsText += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

            statsText += $"✅ Fatos Validados: {_stats.TotalValidated}\n";
            statsText += $"⏳ Fatos Candidatos: {_stats.TotalCandidates}\n";
            statsText += $"❌ Fatos Rejeitados: {_stats.TotalRejected}\n";
            statsText += $"🗑️ Fatos Deprecados: {_stats.TotalDeprecated}\n";
            statsText += $"⚠️ Conflitos Ativos: {_stats.TotalConflicts}\n\n";

            statsText += $"Confiança Média: {_stats.AverageConfidence:P2}\n";
            statsText += $"Total de Fatos: {_stats.TotalValidated + _stats.TotalCandidates + _stats.TotalRejected + _stats.TotalDeprecated}\n\n";

            if (_stats.ValidationStats != null)
            {
                statsText += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                statsText += $"VALIDAÇÃO\n";
                statsText += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

                statsText += $"Sessões Totais: {_stats.ValidationStats.TotalSessions}\n";
                statsText += $"Sessões Completas: {_stats.ValidationStats.CompletedSessions}\n";
                statsText += $"Candidatos Apresentados: {_stats.ValidationStats.TotalCandidatesPresented}\n";
                statsText += $"Aprovados: {_stats.ValidationStats.TotalApproved}\n";
                statsText += $"Rejeitados: {_stats.ValidationStats.TotalRejected}\n";
                statsText += $"Editados: {_stats.ValidationStats.TotalEdited}\n";
                statsText += $"Taxa de Aprovação: {_stats.ValidationStats.ApprovalRate:P2}\n\n";
            }

            if (_stats.SourceDistribution != null && _stats.SourceDistribution.Any())
            {
                statsText += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                statsText += $"DISTRIBUIÇÃO DE FONTES\n";
                statsText += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

                foreach (var source in _stats.SourceDistribution.OrderByDescending(x => x.Value))
                {
                    statsText += $"{source.Key}: {source.Value}\n";
                }
            }

            StatsTextBox.Text = statsText;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}