using SmartAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SmartAI
{
    public partial class HistoryViewerDialog : Window
    {
        private readonly List<FactHistory> _history;

        public HistoryViewerDialog(List<FactHistory> history)
        {
            InitializeComponent();
            _history = history;
            LoadHistory();
        }

        private void LoadHistory()
        {
            var historyText = $"📜 HISTÓRICO DE MUDANÇAS\n\n";
            historyText += $"Total de mudanças: {_history.Count}\n\n";
            historyText += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";

            foreach (var entry in _history)
            {
                historyText += $"🕒 {entry.ChangedAt:dd/MM/yyyy HH:mm:ss}\n";
                historyText += $"Fato ID: {entry.FactId} | Versão: {entry.Version}\n";
                historyText += $"Tipo: {entry.ChangeType}\n";
                historyText += $"Por: {entry.ChangedBy}\n";

                if (!string.IsNullOrEmpty(entry.Reason))
                    historyText += $"Razão: {entry.Reason}\n";

                if (entry.ChangeType == ChangeType.CONFIDENCE_UPDATED)
                {
                    historyText += $"Confiança: {entry.PreviousConfidence:P2} → {entry.NewConfidence:P2}\n";
                }
                else if (entry.ChangeType == ChangeType.CONTENT_EDITED)
                {
                    if (entry.PreviousSubject != entry.NewSubject)
                        historyText += $"Subject: {entry.PreviousSubject} → {entry.NewSubject}\n";
                    if (entry.PreviousRelation != entry.NewRelation)
                        historyText += $"Relation: {entry.PreviousRelation} → {entry.NewRelation}\n";
                    if (entry.PreviousObject != entry.NewObject)
                        historyText += $"Object: {entry.PreviousObject} → {entry.NewObject}\n";
                }

                historyText += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";
            }

            HistoryTextBox.Text = historyText;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}