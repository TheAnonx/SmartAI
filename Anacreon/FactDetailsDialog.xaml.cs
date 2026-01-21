using SmartAI.Models;
using System;
using System.Linq;
using System.Windows;

namespace SmartAI
{
    public partial class FactDetailsDialog : Window
    {
        private readonly Fact _fact;

        public FactDetailsDialog(Fact fact)
        {
            InitializeComponent();
            _fact = fact;
            LoadDetails();
        }

        private async void LoadDetails()
        {
            var details = $"📖 DETALHES DO FATO\n\n";
            details += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

            details += $"Subject: {_fact.Subject}\n";
            details += $"Relation: {_fact.Relation}\n";
            details += $"Object: {_fact.Object}\n\n";

            details += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            details += $"METADADOS\n";
            details += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

            details += $"Status: {_fact.Status}\n";
            details += $"Confiança: {_fact.Confidence:P2}\n";
            details += $"Versão: {_fact.Version}\n";
            details += $"Criado em: {_fact.CreatedAt:dd/MM/yyyy HH:mm:ss}\n";

            if (_fact.ValidatedAt.HasValue)
                details += $"Validado em: {_fact.ValidatedAt:dd/MM/yyyy HH:mm:ss}\n";

            if (!string.IsNullOrEmpty(_fact.ApprovedBy))
                details += $"Aprovado por: {_fact.ApprovedBy}\n";

            if (_fact.DeprecatedAt.HasValue)
            {
                details += $"\n⚠️ DEPRECADO\n";
                details += $"Deprecado em: {_fact.DeprecatedAt:dd/MM/yyyy HH:mm:ss}\n";
                details += $"Razão: {_fact.DeprecationReason}\n";
            }

            details += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            details += $"FONTES ({_fact.Sources.Count})\n";
            details += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

            foreach (var source in _fact.Sources)
            {
                details += $"• Tipo: {source.Type}\n";
                details += $"  Identificador: {source.Identifier}\n";
                details += $"  Peso de Confiança: {source.TrustWeight:P0}\n";
                details += $"  Coletado em: {source.CollectedAt:dd/MM/yyyy HH:mm}\n";

                if (!string.IsNullOrEmpty(source.URL))
                    details += $"  URL: {source.URL}\n";

                details += "\n";
            }

            if (_fact.History.Any())
            {
                details += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                details += $"HISTÓRICO ({_fact.History.Count} mudanças)\n";
                details += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

                foreach (var history in _fact.History.OrderBy(h => h.Version))
                {
                    details += $"Versão {history.Version} - {history.ChangeType}\n";
                    details += $"  Por: {history.ChangedBy}\n";
                    details += $"  Em: {history.ChangedAt:dd/MM/yyyy HH:mm:ss}\n";

                    if (!string.IsNullOrEmpty(history.Reason))
                        details += $"  Razão: {history.Reason}\n";

                    details += "\n";
                }
            }

            DetailsTextBox.Text = details;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}