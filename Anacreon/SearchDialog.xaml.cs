using System.Windows;
using System.Windows.Controls; // NECESSÁRIO
using System.Windows.Input;

namespace SmartAI
{
    public partial class SearchDialog : Window
    {
        public string SearchQuery { get; private set; } = string.Empty;

        public SearchDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Método exigido pelo XAML.
            // Pode permanecer vazio.
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                PerformSearch();
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void PerformSearch()
        {
            var query = SearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show(
                    "Por favor, digite algo para buscar!",
                    "Campo vazio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                SearchTextBox.Focus();
                return;
            }

            SearchQuery = query;
            DialogResult = true;
        }
    }
}
