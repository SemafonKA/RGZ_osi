using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using N_Manager;

namespace RGZ_osi
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public int defaultQuantTime = 1000;

        public Manager manager;

        public MainPage()
        {
            this.InitializeComponent();
            manager = new Manager(defaultQuantTime);

            manager.events.Add(async () =>
            {
                ProcessQueueList.ItemsSource = (await manager.ProcessesQueue.GetListAsync()).Reverse<Process>();
                return 0;
            });

#pragma warning disable CS1998
            manager.events.Add(async () =>
            {
                ProcessList.ItemsSource = manager.Processes.ToList();
                return 0;
            });

            manager.events.Add(async () =>
            {
                FinalizedProcessList.ItemsSource = manager.Processes.Where((e) => e.IsAlive == false);
                return 0;
            });
#pragma warning restore CS1998
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Exit();
        }
        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var about = new MainPage_About();
            await about.ShowAsync();
        }

        private async void MakeNewProcessButton_Click(object sender, RoutedEventArgs e)
        {
            var name = ProcessNameBox.Text;
            var isParced = int.TryParse(ProcessIdBox.Text.ToString(),out var id);
            if (!isParced)
            {
                var _ = new ContentDialog()
                {
                    Title = "Некорректные данные!",
                    Content = "Некорректно введённые данные ID процесса",
                    CloseButtonText = "Исправлюсь..."
                }.ShowAsync();
                return;
            }

            isParced = int.TryParse(ProcessPriorityBox.Text.ToString(),out var priority);
            if (!isParced)
            {
                var _ = new ContentDialog()
                {
                    Title = "Некорректные данные!",
                    Content = "Некорректно введённые данные приоритета процесса",
                    CloseButtonText = "Исправлюсь..."
                }.ShowAsync();
                return;
            }

            isParced = int.TryParse(ProcessSizeBox.Text.ToString(),out var initTime);
            if (!isParced)
            {
                var _ = new ContentDialog()
                {
                    Title = "Некорректные данные!",
                    Content = "Некорректно введённые данные времени исполнения процесса",
                    CloseButtonText = "Исправлюсь..."
                }.ShowAsync();
                return;
            }

            var process = new N_Manager.Process()
            {
                Id = id,
                Priority = priority,
                InitialSize = initTime,
                Size = initTime,
                Name = name
            };

            if (!await manager.AddProcess(process))
            {
                var _ = new ContentDialog()
                {
                    Title = "Некорректные данные!",
                    Content = "Процесс с таким ID уже существует",
                    CloseButtonText = "Исправлюсь..."
                }.ShowAsync();
                return;
            }

            ProcessNameBox.Text = "";
            ProcessIdBox.Text = "";
            ProcessPriorityBox.Text = "";
            ProcessSizeBox.Text = "";

            manager.RunEvents();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Button but = sender as Button;

            if (manager.Status == ManagerStatus.aborted)
            {
                but.Content = "Стоп";
                manager.Run();
            }
            else
            {
                await manager.AwaitExit();
                but.Content = "Пуск";
            }
        }

        private void QuantTimeBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox box = sender as TextBox;
            var isParsed = int.TryParse(box.Text, out var time);
            if (isParsed)
            {
                manager.quantTimeMS = time;
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            await manager.Reset();
            StartButton.Content = "Пуск";
            manager.RunEvents();
        }
    }
}
