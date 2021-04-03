using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniDashboard
{
    public class Assignment
    {
        private static readonly DateTime nowDate = DateTimeOffset.Now.UtcDateTime;
        public string name { get; set; }
        public string course { get; set; }
        private DateTime assignmentDate { get; }
        public string date { get; set; }
        public int remaining { get; }
        public int id { get; set; }
        public Assignment(long seconds)
        {
            assignmentDate = UnixSecondsToDate(seconds);
            remaining = (int)(assignmentDate - nowDate).TotalDays;
            date = assignmentDate.ToString("dd/MM");
        }
        private static DateTime UnixSecondsToDate(long seconds)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            return dtDateTime.AddSeconds(seconds).ToLocalTime();
        }
    }

}
