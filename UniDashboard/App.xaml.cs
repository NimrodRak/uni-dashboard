
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Windows;

namespace UniDashboard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public JObject configurationJson;
        public List<Assignment> items;
        
        public void MessageAndExit(string message)
        {
            MessageBox.Show(message);
            this.Shutdown();
        }
    }
}
