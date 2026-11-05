using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string connectionString =
    @"Server=.\SQLEXPRESS;Database=ExpenseTrackerDB;Integrated Security=true;TrustServerCertificate=true;";

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }
        private void LoadData()
        {
            try
            {
                DataTable table = new DataTable();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, Date, Category, Description, Amount FROM Expenses ORDER BY Date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(table);
                    }
                }

                dgExpenses.ItemsSource = table.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        // Добавление расхода
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Простая валидация
            if (dpDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCategory.Text))
            {
                MessageBox.Show("Введите категорию.");
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму (положительное число).");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Параметризованный запрос — защита от SQL-инъекций [citation:6]
                    string query = @"INSERT INTO Expenses (Date, Category, Description, Amount) 
                                     VALUES (@Date, @Category, @Description, @Amount)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", dpDate.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@Category", txtCategory.Text.Trim());
                        cmd.Parameters.AddWithValue("@Description",
                            string.IsNullOrWhiteSpace(txtDescription.Text) ? (object)DBNull.Value : txtDescription.Text.Trim());
                        cmd.Parameters.AddWithValue("@Amount", amount);

                        cmd.ExecuteNonQuery();
                        
                    }
                }

                // Очистка полей и обновление таблицы
                txtCategory.Clear();
                txtDescription.Clear();
                txtAmount.Clear();
                LoadData();
                UpdateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message);
            }
        }
        private void UpdateTotal()
        {
            if (dgExpenses.ItemsSource == null)
            {
                txtTotalSum.Text = "0.00";
                return;
            }

            decimal total = 0;

            // Проходим по всем строкам в DataGrid
            foreach (var item in dgExpenses.Items)
            {
                if (item is DataRowView rowView)
                {
                    object amountObj = rowView["Amount"];
                    if (amountObj != null && amountObj != DBNull.Value)
                    {
                        total += Convert.ToDecimal(amountObj);
                    }
                }
            }

            // Форматируем вывод (например, 1500.50)
            txtTotalSum.Text = total.ToString("N2");
        }

        // Обновление таблицы
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // Удаление выбранной записи
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgExpenses.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления.");
                return;
            }

            DataRowView row = dgExpenses.SelectedItem as DataRowView;
            if (row == null) return;

            int id = Convert.ToInt32(row["Id"]);

            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM Expenses WHERE Id = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }
    }
}
