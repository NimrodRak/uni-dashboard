using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;

namespace UniDashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly BackgroundWorker worker = new();
        private readonly string autoCompilerPath;

        public MainWindow()
        {
            InitializeComponent();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            autoCompilerPath = (string)((App)Application.Current).configurationJson["moodle"]["autoCompilerPath"];
            if (!File.Exists(autoCompilerPath))
            {
                compilationButton.IsEnabled = false;
                compilationButton.ToolTip += " Disabled because AutoCompiler.bat was not found";
            }
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Process process = new()
            {
                StartInfo =
                {
                    FileName = autoCompilerPath,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.StandardInput.WriteLine("Y");
            process.StandardInput.WriteLine();
            process.WaitForExit();
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ((App)Application.Current).MessageAndExit(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                ((App)Application.Current).MessageAndExit("Operation cancelled.");
            }
            else
            {
                compilationButton.Content = "Compile Files";
                compilationButton.IsEnabled = true;
            }
        }
        private void CompilationButton_Click(object sender, RoutedEventArgs e)
        {
            compilationButton.Content = "Compiling";
            compilationButton.IsEnabled = false;
            worker.RunWorkerAsync();
        }
        private void MoodleButton_Click(object sender, RoutedEventArgs e)
        {
            JObject configurationJson = ((App)Application.Current).configurationJson;
            Process.Start(new ProcessStartInfo((string)configurationJson["moodle"]["site"] + (string)configurationJson["moodle"]["dashboardPath"]) {
                UseShellExecute = true
            });
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ObservableCollection<Assignment> assigns = new(((App)Application.Current).items);
            if (assigns.Count == 0)
            {
                Master.Visibility = Visibility.Collapsed;
                clearTODOTextBlock.Visibility = Visibility.Visible;
            }
            Master.ItemsSource = assigns;
        }
        private void Master_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            JObject configurationJson = ((App)Application.Current).configurationJson;
            Process.Start(new ProcessStartInfo((string)configurationJson["moodle"]["site"] + "mod/assign/view.php?id=" + ((Assignment)e.AddedItems[0]).cmid.ToString()) {
                UseShellExecute = true
            });
            lb.UnselectAll();
        }
    }
}