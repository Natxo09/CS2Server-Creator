using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CS2ServerCreator
{
    public class AppConfiguration
    {
        public string SelectedDirectory { get; set; }
        public string SelectedMap { get; set; }
        public string GameMode { get; set; }
        public int MaxPlayers { get; set; }
        public int Port { get; set; }
        public bool IsAutoexecChecked { get; set; }
        public bool IsInsecureChecked { get; set; }
        public bool IsDisableBotsChecked { get; set; }
        public string CustomParameters { get; set; }
        public string ServerName { get; set; }
        public string ServerPassword { get; set; }
    }
}
