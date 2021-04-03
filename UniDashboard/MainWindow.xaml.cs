using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;

namespace UniDashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new();
        private static readonly BackgroundWorker worker = new();
        private readonly JObject configurationJson;
        public MainWindow()
        {
            InitializeComponent();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            string jsonPath = Environment.GetEnvironmentVariable("uni") + @"\dashboard.json";
            try
            {
                configurationJson = JObject.Parse(File.ReadAllText(jsonPath));
            }
            catch (Exception)
            {
                Application.Current.Shutdown();
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Process process = new()
            {
                StartInfo =
                {
                    FileName = (string)configurationJson["moodle"]["autoCompilerPath"],
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
            compilationButton.Visibility = Visibility.Visible;
            compilationTextBlock.Visibility = Visibility.Hidden;
        }
        private void CompilationButton_Click(object sender, RoutedEventArgs e)
        {
            compilationButton.Visibility = Visibility.Hidden;
            compilationTextBlock.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();
        }
        private void MoodleButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo((string)configurationJson["moodle"]["site"] + (string)configurationJson["moodle"]["dashboardPath"]) {
                UseShellExecute = true
            });
        }
        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            loadingTextBlock.Visibility = Visibility.Visible;
            
            IEnumerable<Assignment> assignments = await GetAssignmentsAsync();
            if (assignments != null)
            {
                loadingTextBlock.Visibility = Visibility.Hidden;
                compilationButton.Visibility = Visibility.Visible;
                moodleButton.Visibility = Visibility.Visible;
                Master.ItemsSource = new ObservableCollection<Assignment>(assignments);
            }
            else
            {
                Application.Current.Shutdown();
            }

        }
        private async Task<IEnumerable<Assignment>> GetAssignmentsAsync()
        {
            string tokenUrl = (string)configurationJson["moodle"]["site"] + (string)configurationJson["moodle"]["tokenEndpoint"]
                           + $"?username={configurationJson["moodle"]["username"]}"
                           + $"&password={configurationJson["moodle"]["password"]}"
                            + "&service=moodle_mobile_app";
            try
            {
                string tokenResponse = await client.GetStringAsync(tokenUrl);
                JObject tokenJson = JObject.Parse(tokenResponse);
                string token = (string)tokenJson["token"];

                string assignUrl = (string)configurationJson["moodle"]["site"] + (string)configurationJson["moodle"]["wsEndpoint"]
                                 + $"?wstoken={token}"
                                  + "&wsfunction=mod_assign_get_assignments"
                                  + "&moodlewsrestformat=json";
                string assignResponse = await client.GetStringAsync(assignUrl);
                JObject assignJson = JObject.Parse(assignResponse);

                long currentUnix = DateTimeOffset.Now.ToUnixTimeSeconds();
                var assignQuery =
                    from course in assignJson["courses"]
                    from assign in course["assignments"]
                    where currentUnix < (long)assign["duedate"]
                    orderby (long)assign["duedate"] ascending
                    select new Assignment((long)assign["duedate"])
                    {
                        name = (string)assign["name"],
                        course = ((string)assign["course"]).Substring(0, 5),
                        id = (int)assign["id"]
                    };
                return assignQuery;

            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}