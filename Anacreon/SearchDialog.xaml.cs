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
            
            ErrorMessage.Visibility = Visibility.Collapsed;
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
                if (string.IsNullOrWhiteSpace(query))
                {
                    ErrorMessage.Text = "⚠️ Digite algo para buscar.";
                    ErrorMessage.Visibility = Visibility.Visible;
                    SearchTextBox.Focus();
                    return;
                }

                return;
            }

            SearchQuery = query;
            DialogResult = true;
        }
    }
}
