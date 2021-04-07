using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace UniDashboard
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        private static readonly HttpClient client = new();
        private readonly BackgroundWorker worker = new();

        public SplashScreenWindow()
        {
            InitializeComponent();
            string uniEnvVar = Environment.GetEnvironmentVariable("uni");
            if (uniEnvVar == null)
            {
                ((App)Application.Current).MessageAndExit("UNI environment variable not found.");
            }
            string jsonPath =  uniEnvVar + @"\dashboard.json";
            try
            {
                ((App)Application.Current).configurationJson = JObject.Parse(File.ReadAllText(jsonPath));
            }
            catch (Exception e)
            {
                ((App)Application.Current).MessageAndExit(e.Message);
            }

            InitializeWorker();
            worker.RunWorkerAsync();
        }
        private void InitializeWorker()
        {
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
        }
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ((App)Application.Current).MessageAndExit(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                ((App)Application.Current).MessageAndExit("Operation cancelled");
            }
            else if (e.Result == null)
            {
                ((App)Application.Current).MessageAndExit("Operation failed");
            }
            else
            {
                ((App)Application.Current).items = e.Result as List<Assignment>;
                MainWindow mw = new();
                mw.Show();
                this.Close();
            }
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Task<IEnumerable<Assignment>> getAssigns = GetAssignmentsAsync();
            getAssigns.Wait();
            List<Assignment> assigns = getAssigns.Result.ToList();
            worker.ReportProgress(50);
            int progressIncrement = 50 / assigns.Count;
            int currentProgress = 50;
            List<Assignment> filteredAssigns = new();

            JObject configurationJson = ((App)Application.Current).configurationJson;
            foreach (var item in assigns)
            {
                Task<string> tokenTask = GetTokenAsync();
                tokenTask.Wait();
                string token = tokenTask.Result;
                string assignStatusUrl = (string)configurationJson["moodle"]["site"] + (string)configurationJson["moodle"]["wsEndpoint"]
                                     + $"?wstoken={token}"
                                      + "&wsfunction=mod_assign_get_submission_status"
                                      + "&moodlewsrestformat=json"
                                     + $"&assignid={item.id}";
                Task<string> statusResTask = HttpRequestAsync(assignStatusUrl);
                statusResTask.Wait();
                JObject statusRes = JObject.Parse(statusResTask.Result);
                if (!statusRes.ContainsKey("lastattempt") || (string)statusRes["lastattempt"]["submission"]["status"] != "submitted")
                {
                    filteredAssigns.Add(item);
                }
                currentProgress += progressIncrement;
                worker.ReportProgress(currentProgress);
            }
            e.Result = filteredAssigns;
        }
        private static async Task<string> GetTokenAsync()
        {
            JObject configurationJson = ((App)Application.Current).configurationJson;
            string tokenUrl = (string)configurationJson["moodle"]["site"] + (string)configurationJson["moodle"]["tokenEndpoint"]
                           + $"?username={configurationJson["moodle"]["username"]}"
                           + $"&password={configurationJson["moodle"]["password"]}"
                            + "&service=moodle_mobile_app";
            
            JObject tokenJson = JObject.Parse(await HttpRequestAsync(tokenUrl));
            if (tokenJson.ContainsKey("token"))
            {
                return (string)tokenJson["token"];
            }
            else
            {
                throw new Exception("Token key not present in response JSON");
            }
        }
        private static async Task<IEnumerable<Assignment>> GetAssignmentsAsync()
        {
            JObject configurationJson = ((App)Application.Current).configurationJson;
            string token = await GetTokenAsync();
            string assignUrl = (string)configurationJson["moodle"]["site"] + (string)configurationJson["moodle"]["wsEndpoint"]
                             + $"?wstoken={token}"
                              + "&wsfunction=mod_assign_get_assignments"
                              + "&moodlewsrestformat=json";
            
            JObject assignJson = JObject.Parse(await HttpRequestAsync(assignUrl));

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
                    id = (int)assign["id"],
                    cmid = (int)assign["cmid"]
                };
            return assignQuery;
        }
        private static async Task<string> HttpRequestAsync(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception($"HTTP request to ${url} failed");
            }
        }
    }
}
