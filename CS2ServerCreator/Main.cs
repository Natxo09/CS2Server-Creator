using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.Net;
using System.Net.Sockets;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace CS2ServerCreator
{
    public partial class Main : Form
    {
        private string selectedDirectory = "";
        private Process cs2Process = null;
        private Timer restartTimer;
        private Timer checkServerStatusTimer;
        public Main()
        {
            InitializeComponent();
            DisplayInternalIP();
            DisplayExternalIP();

            // Inicialización del Timer de reinicio
            restartTimer = new Timer();
            restartTimer.Tick += TimerTick;

            // Inicialización del Timer de comprobación del estado del servidor
            checkServerStatusTimer = new Timer();
            checkServerStatusTimer.Interval = 10 * 1000; // 10 segundos
            checkServerStatusTimer.Tick += CheckServerStatusTick;
        }

        private void btnDirExplorer_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.ValidateNames = false;
                openFileDialog.CheckFileExists = false;
                openFileDialog.CheckPathExists = true;
                openFileDialog.FileName = "Select a folder";
                openFileDialog.Title = "Select a directory";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedDirectory = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                    textDir.Text = selectedDirectory;
                    //MessageBox.Show($"Selected directory: {selectedDirectory}");
                }
            }
        }

        private void AppendTextToConsole(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextToConsole), new object[] { text });
                return;
            }
            richTextBoxConsole.AppendText(text + Environment.NewLine);
        }

        private void Cs2Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Invoke(new Action(() =>
                {
                    richTextBoxConsole.AppendText(e.Data + Environment.NewLine);
                    richTextBoxConsole.SelectionStart = richTextBoxConsole.Text.Length;
                    richTextBoxConsole.ScrollToCaret();
                }));
            }
        }

        private void btnClearConsole_Click(object sender, EventArgs e)
        {
            richTextBoxConsole.Clear();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (cs2Process == null || cs2Process.HasExited)  // Si el proceso no está en ejecución
            {
                if (!string.IsNullOrEmpty(selectedDirectory))
                {
                    string exePath = System.IO.Path.Combine(selectedDirectory, "cs2.exe");

                    if (System.IO.File.Exists(exePath))
                    {
                        // Argumentos base
                        string args = "-dedicated";
                        string customArgs = textCustomParameters.Text.Trim();

                        // Determinar los argumentos basados en comboGamemode:
                        if (comboGamemode.SelectedItem != null)
                        {
                            string selectedMode = comboGamemode.SelectedItem.ToString();
                            switch (selectedMode)
                            {
                                case "Casual":
                                    args += " +game_type 0 +game_mode 0";
                                    break;
                                case "Competitive":
                                    args += " +game_type 0 +game_mode 1";
                                    break;
                                case "Deathmatch":
                                    args += " +game_type 1 +game_mode 2";
                                    break;
                            }
                        }

                        // Añadir el mapa seleccionado
                        if (comboMap.SelectedItem != null)
                        {
                            string selectedMap = comboMap.SelectedItem.ToString();
                            args += " +map " + selectedMap;
                        }

                        // Añadir -insercure o -secure a args
                        if (checkInsecure.Checked)
                        {
                            args += " -insecure";
                        }
                        else
                        {
                            args += " -secure";
                        }

                        // Añadir Custom args
                        if (!string.IsNullOrEmpty(customArgs))
                        {
                            args += " " + customArgs;
                        }

                        // Añadir el número de jugadores máximo basado en numericUpDownPlayers
                        int numPlayers = (int)numericUpDownPlayers.Value;
                        args += " -maxplayers " + numPlayers;

                        // Verificar si checkAutoexec está marcado
                        if (checkAutoexec.Checked)
                        {
                            args += " +exec autoexec.cfg";
                        }
                        if (checkDisBots.Checked)
                        {
                            args += " -nobots";
                        }

                        int port = (int)numericUpDownPort.Value;
                        args += " -port " + port;

                        // Si el checkbox de minutos está marcado, inicia el Timer de reinicio
                        if (checkMinutes.Checked)
                        {
                            restartTimer.Interval = (int)numericUpDownMinutes.Value * 60 * 1000; // Convertir minutos a milisegundos
                            restartTimer.Start();
                        }

                        checkServerStatusTimer.Start(); // Inicia el Timer de comprobación de estado

                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = args,
                            WorkingDirectory = selectedDirectory
                        };

                        if (checkBoxConsole.Checked) // Si el checkBox está marcado
                        {
                            args += " -hideconsole";
                            startInfo.RedirectStandardOutput = true;
                            //startInfo.RedirectStandardInput = true;
                            startInfo.UseShellExecute = false;
                            startInfo.CreateNoWindow = true;
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden; // Esta línea evita que se muestre la consola.

                            cs2Process = Process.Start(startInfo);

                            cs2Process.OutputDataReceived += Cs2Process_OutputDataReceived;
                            cs2Process.BeginOutputReadLine();
                        }
                        else
                        {
                            cs2Process = Process.Start(startInfo);
                        }

                        //btnStart.Text = "Stop";  // Cambiar el texto del botón a "Stop"
                        //txtStatus.Text = "Running ......";
                    }
                    else
                    {
                        MessageBox.Show("cs2.exe was not found in the selected directory.");
                    }
                }
                else
                {
                    MessageBox.Show("Please, Select the right directory.");
                }
            }
            //else  // Si el proceso ya está en ejecución
            //{
            //    cs2Process.Kill();  // Terminar el proceso
            //    cs2Process = null;  // Restablecer la variable
            //    btnStart.Text = "Start";  // Cambiar el texto del botón de nuevo a "Start"
            //    txtStatus.Text = "Stopped ......";
            //}
        }

        private void AppendTextAndScroll(RichTextBox box, string text)
        {
            if (box.InvokeRequired)
            {
                box.Invoke(new Action(() => AppendTextAndScroll(box, text)));
            }
            else
            {
                box.AppendText(text);
                box.SelectionStart = box.Text.Length;
                box.ScrollToCaret();
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnServerCfg_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                int gameIndex = selectedDirectory.IndexOf("\\game\\");

                if (gameIndex >= 0)
                {
                    string cfgPath = selectedDirectory.Substring(0, gameIndex) + "\\game\\csgo\\cfg\\server.cfg";

                    if (File.Exists(cfgPath))
                    {
                        Process.Start(cfgPath);
                    }
                    else
                    {
                        MessageBox.Show($"El archivo {cfgPath} no se encontró.");
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show("The filee 'game' was not found in the current directory. ¿Do you want to select the folder manually?", "Folder was not found", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                        {
                            folderDialog.Description = "Select the folder 'game'";

                            if (folderDialog.ShowDialog() == DialogResult.OK)
                            {
                                selectedDirectory = folderDialog.SelectedPath;
                                textDir.Text = selectedDirectory;

                                string cfgPath = Path.Combine(selectedDirectory, "csgo\\cfg\\server.cfg");

                                if (File.Exists(cfgPath))
                                {
                                    Process.Start(cfgPath);
                                }
                                else
                                {
                                    MessageBox.Show($"The filee {cfgPath} was not found.");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, select the directory first.");
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            About aboutForm = new About(); // Crea una nueva instancia del formulario About.
            aboutForm.Show(); // Muestra el formulario.
        }

        private void btnCreateAutoexecCfg_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "autoexec.cfg");

                    if (!File.Exists(cfgPath))
                    {
                        try
                        {
                            // Contenido para autoexec.cfg
                            string content = @"
hostname ""Counter-Strike 2 Dedicated Server""
rcon_password ""yourrconpassword""
sv_password """"
sv_cheats 0
sv_lan 0
exec banned_user.cfg
exec banned_ip.cfg
";
                            // Crear el archivo autoexec.cfg
                            File.WriteAllText(cfgPath, content);
                            checkAutoexec.Checked = true;
                            MessageBox.Show("autoexec.cfg created successfully!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while creating autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg already exists.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }

        }

        private void UpdateAutoexecHostname()
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "autoexec.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Leer todas las líneas del archivo
                            string[] lines = File.ReadAllLines(cfgPath);

                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].StartsWith("hostname "))
                                {
                                    lines[i] = $"hostname \"{textSvName.Text}\"";
                                    break; // Salir del bucle una vez se haya actualizado la línea
                                }
                            }

                            // Guardar las líneas modificadas de vuelta en el archivo
                            File.WriteAllLines(cfgPath, lines);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while updating autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private void textSvName_TextChanged(object sender, EventArgs e)
        {
            UpdateAutoexecHostname();
        }

        private void UpdateAutoexecPassword()
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "autoexec.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Leer todas las líneas del archivo
                            string[] lines = File.ReadAllLines(cfgPath);

                            // Asegurarnos de que el archivo tiene al menos 4 líneas
                            if (lines.Length >= 4 && lines[3].StartsWith("sv_password "))
                            {
                                lines[3] = $"sv_password \"{textPassword.Text}\""; // Corregí la variable a textSvPassword

                                // Guardar las líneas modificadas de vuelta en el archivo
                                File.WriteAllLines(cfgPath, lines);
                            }
                            else
                            {
                                MessageBox.Show("The sv_password line is not correctly positioned in autoexec.cfg or the line doesn't exist.");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while updating autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private void textPassword_TextChanged(object sender, EventArgs e)
        {
            UpdateAutoexecPassword();
        }

        private void btnAutoexecCfg_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "autoexec.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Abrir el archivo autoexec.cfg con el programa predeterminado
                            Process.Start(cfgPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while opening autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private int GetGameModeIndex(string gameMode)
        {
            switch (gameMode)
            {
                case "Casual":
                    return 0;
                case "Competitive":
                    return 1;
                case "Deathmatch":
                    return 2;
                default:
                    return -1;
            }
        }

        private void SaveConfigurationToXml(AppConfiguration config, string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppConfiguration));
            using (TextWriter writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, config);
            }
        }

        private void SaveConfiguration()
        {
            AppConfiguration config = new AppConfiguration()
            {
                SelectedDirectory = selectedDirectory,
                GameMode = comboGamemode.SelectedItem != null ? comboGamemode.SelectedItem.ToString() : string.Empty,
                SelectedMap = comboMap.SelectedItem != null ? comboMap.SelectedItem.ToString() : string.Empty,
                MaxPlayers = (int)numericUpDownPlayers.Value,
                Port = (int)numericUpDownPort.Value,
                IsAutoexecChecked = checkAutoexec.Checked,
                IsInsecureChecked = checkInsecure.Checked,
                IsDisableBotsChecked = checkDisBots.Checked,
                CustomParameters = textCustomParameters.Text,
                ServerName = textSvName.Text,
                ServerPassword = textPassword.Text,
            };

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            saveFileDialog.DefaultExt = "xml";
            saveFileDialog.AddExtension = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveConfigurationToXml(config, saveFileDialog.FileName);
            }
        }

        private AppConfiguration LoadConfigurationFromXml(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppConfiguration));
            using (TextReader reader = new StreamReader(filePath))
            {
                return (AppConfiguration)serializer.Deserialize(reader);
            }
        }

        private void LoadConfiguration()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AppConfiguration config = LoadConfigurationFromXml(openFileDialog.FileName);

                selectedDirectory = config.SelectedDirectory;
                textDir.Text = selectedDirectory;
                comboGamemode.SelectedIndex = GetGameModeIndex(config.GameMode);
                comboMap.SelectedItem = config.SelectedMap;
                numericUpDownPlayers.Value = config.MaxPlayers;
                numericUpDownPort.Value = config.Port;
                checkAutoexec.Checked = config.IsAutoexecChecked;
                checkInsecure.Checked = config.IsInsecureChecked;
                checkDisBots.Checked = config.IsDisableBotsChecked;
                textCustomParameters.Text = config.CustomParameters;
                textSvName.Text = config.ServerName;
                textPassword.Text = config.ServerPassword;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadConfiguration();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Natxo09/CS2Server-Creator");
        }

        private void btnSteamCmd_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://developer.valvesoftware.com/wiki/SteamCMD");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void servercfgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                int gameIndex = selectedDirectory.IndexOf("\\game\\");

                if (gameIndex >= 0)
                {
                    string cfgPath = selectedDirectory.Substring(0, gameIndex) + "\\game\\csgo\\cfg\\server.cfg";

                    if (File.Exists(cfgPath))
                    {
                        Process.Start(cfgPath);
                    }
                    else
                    {
                        MessageBox.Show($"El archivo {cfgPath} no se encontró.");
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show("The filee 'game' was not found in the current directory. ¿Do you want to select the folder manually?", "Folder was not found", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                        {
                            folderDialog.Description = "Select the folder 'game'";

                            if (folderDialog.ShowDialog() == DialogResult.OK)
                            {
                                selectedDirectory = folderDialog.SelectedPath;
                                textDir.Text = selectedDirectory;

                                string cfgPath = Path.Combine(selectedDirectory, "csgo\\cfg\\server.cfg");

                                if (File.Exists(cfgPath))
                                {
                                    Process.Start(cfgPath);
                                }
                                else
                                {
                                    MessageBox.Show($"The filee {cfgPath} was not found.");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, select the directory first.");
            }
        }

        private void autoexeccfgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "autoexec.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Abrir el archivo autoexec.cfg con el programa predeterminado
                            Process.Start(cfgPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while opening autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private void githubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Natxo09/CS2Server-Creator");
        }

        private void DisplayInternalIP()
        {
            try
            {
                // Obtener el nombre del host del equipo local
                string hostName = Dns.GetHostName();

                // Encontrar la dirección IP usando el nombre del host
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);

                // Seleccionar una dirección IP (la primera que encuentre que es IPv4, si existe)
                foreach (IPAddress ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                    {
                        textInternalIp.Text = ip.ToString();
                        return;
                    }
                }

                // Si no se encuentra una dirección IPv4, puedes mostrar un mensaje o manejarlo de otra manera
                textInternalIp.Text = "No IPv4 address found!";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving internal IP: " + ex.Message);
            }
        }

        private void DisplayExternalIP()
        {
            try
            {
                WebClient webClient = new WebClient();
                string externalIP = webClient.DownloadString("http://api.ipify.org");
                textExternalIp.Text = externalIP.Trim(); // Establecer la IP externa en el TextBox
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving external IP: " + ex.Message);
            }
        }

        private void btnInfoParam_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://developer.valvesoftware.com/wiki/Command_line_options");
        }

        private void gamemodecasualcfgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "gamemode_casual.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Abrir el archivo autoexec.cfg con el programa predeterminado
                            Process.Start(cfgPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while opening autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private void gamemodecompetitivecfgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "gamemode_competitive.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Abrir el archivo autoexec.cfg con el programa predeterminado
                            Process.Start(cfgPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while opening autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private void gamemodecompetitive2v2cfgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "gamemode_competitive2v2.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Abrir el archivo autoexec.cfg con el programa predeterminado
                            Process.Start(cfgPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while opening autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private void gamemodedeathmatchcfgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                string baseDir = selectedDirectory;
                int gameIndex = baseDir.IndexOf("\\game\\");

                if (gameIndex != -1)
                {
                    baseDir = baseDir.Substring(0, gameIndex + "\\game\\".Length);
                    string cfgPath = Path.Combine(baseDir, "csgo", "cfg", "gamemode_deathmatch.cfg");

                    if (File.Exists(cfgPath))
                    {
                        try
                        {
                            // Abrir el archivo autoexec.cfg con el programa predeterminado
                            Process.Start(cfgPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred while opening autoexec.cfg: " + ex.Message);
                        }
                    }
                    else
                    {
                        MessageBox.Show("autoexec.cfg does not exist. Please create it first.");
                    }
                }
                else
                {
                    MessageBox.Show("The folder 'game' was not found in the selected directory path.");
                }
            }
            else
            {
                MessageBox.Show("Please, select the right directory.");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (checkBoxConsole.Checked)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveFileDialog.DefaultExt = "txt";
                    saveFileDialog.AddExtension = true;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string path = saveFileDialog.FileName;
                        File.WriteAllText(path, richTextBoxConsole.Text);
                        MessageBox.Show("Log saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("The server is not running on the app, if you want to save the logs you have to start the server on app", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            restartTimer.Stop();  // Detiene el Timer

            if (cs2Process != null && !cs2Process.HasExited)
            {
                cs2Process.Kill();
                cs2Process.WaitForExit();
            }

            btnStart_Click(this, EventArgs.Empty);
        }

        private void CheckServerStatusTick(object sender, EventArgs e)
        {
            if (cs2Process == null || cs2Process.HasExited)
            {
                checkServerStatusTimer.Stop();
                restartTimer.Stop();
                // Aquí puedes realizar otras acciones, como notificar al usuario.
            }
        }

        private void btnMetamod_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/watch?v=Gnsmn9GPX4k");
        }

        private void btnCounterStrikeSharp_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/roflmuffin/CounterStrikeSharp");
        }

    }
}
