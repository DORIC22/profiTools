using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace profiTools.wind
{
    /// <summary>
    /// Логика взаимодействия для WinWinget.xaml
    /// </summary>
    public partial class WinWinget : Window
    {
        public ObservableCollection<ProgramModel> Programs { get; set; }

        public WinWinget()
        {
            InitializeComponent();

            // Инициализация списка программ
            Programs = new ObservableCollection<ProgramModel>();
            programListView.ItemsSource = Programs;

            // Загрузка списка программ из winget
            LoadProgramsFromWinget();
        }

        private void LoadProgramsFromWinget()
        {
            try
            {
                // Настраиваем процесс для выполнения команды winget list
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "list --accept-source-agreements",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Запускаем процесс
                using (Process process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        MessageBox.Show("Не удалось запустить winget.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Парсим вывод команды
                    ParseWingetOutput(output);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения списка программ: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ParseWingetOutput(string output)
        {
            // Разделяем вывод на строки
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Пропускаем первую строку (заголовки) и начинаем обработку данных
            foreach (var line in lines.Skip(1))
            {
                // Парсим строки по пробелам, чтобы извлечь имя программы
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    // Первое слово в строке обычно содержит имя программы
                    string programName = parts[0];
                    Programs.Add(new ProgramModel { ProgramName = programName });
                }
            }

            // Обновляем интерфейс
            programListView.Items.Refresh();
        }

        private void ActionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Получаем текущий CheckBox и его контекст данных
            var checkBox = sender as CheckBox;
            var dataContext = checkBox.DataContext as ProgramModel;

            if (checkBox == null || dataContext == null)
                return;

            // Если выбрано "Удалить", сбрасываем "Обновить"
            if (checkBox.Content.ToString() == "Удалить")
            {
                dataContext.Update = false;
            }
            // Если выбрано "Обновить", сбрасываем "Удалить"
            else if (checkBox.Content.ToString() == "Обновить")
            {
                dataContext.Remove = false;
            }

            // Обновляем интерфейс
            programListView.Items.Refresh();
        }

        private void ActionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Здесь можно добавить логику при снятии галочки, если требуется
        }

        private void succesButton_Click(object sender, RoutedEventArgs e)
        {
            // Выполняем действия над выбранными программами
            foreach (var program in Programs)
            {
                if (program.Remove)
                {
                    ExecuteWingetCommand("uninstall", program.ProgramName);
                }
                if (program.Update)
                {
                    ExecuteWingetCommand("upgrade", program.ProgramName);
                }
            }

            MessageBox.Show("Действия выполнены успешно!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteWingetCommand(string command, string programName)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"{command} \"{programName}\" --silent",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выполнения команды {command} для {programName}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ProgramModel
    {
        public string ProgramName { get; set; }
        public bool Remove { get; set; }
        public bool Update { get; set; }
    }
}
