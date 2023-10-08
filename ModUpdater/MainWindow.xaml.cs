using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Xml;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text.RegularExpressions;

namespace ModUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class DataContextModel
        {
            public string ProgramVersion { get; } = "RL-1.0.2";
        }




        public MainWindow()
        {
             
            Console.WriteLine("Ready");
            InitializeComponent();
            this.DataContext = new DataContextModel();
            if (File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache.json")))
            {
                var task = Task.Run(() => LoadAndStoreTokenAsync());
                task.Wait();  // Ожидание завершения задачи
                token.Text = task.Result["token"];
                folder_from.Text = task.Result["folder_from"];
                folder_to.Text = task.Result["folder_to"];
                version.Text = task.Result["version"];
                is_modrinth_launcher.IsChecked = bool.Parse(task.Result["modrinth_mode"]);

                if (is_modrinth_launcher.IsChecked == true)
                {
                    folder_from_label.Text = "Modrinth profile folder";
                }
                else if (is_modrinth_launcher.IsChecked == false)
                {
                    folder_from_label.Text = "Folder with your old mods";

                }
            }


        }



        private void update_button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(is_modrinth_launcher.IsChecked.ToString());
            if(is_modrinth_launcher.IsChecked == true)
            {
                string scriptExecutablePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modrinthupdater.exe");

                if (version.Text.Length > 1)
                {
                    if (Directory.Exists(folder_from.Text) && Directory.Exists(folder_to.Text))
                    {
                        if (File.Exists(System.IO.Path.Combine(folder_from.Text, "profile.json"))){
                            Dictionary<string, string> data = new Dictionary<string, string>();
                            data.Add("version", version.Text);
                            data.Add("token", token.Text);
                            data.Add("folder_from", folder_from.Text);
                            data.Add("folder_to", folder_to.Text);

                            Task task = WriteToFileAsync("cache.json", data);
                            output.Text = "";

                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = scriptExecutablePath,
                                    Arguments = $"\"{version.Text}\" \"{folder_from.Text}\" \"{folder_to.Text}\" \"{token.Text}\"",
                                    RedirectStandardOutput = true,
                                    StandardOutputEncoding = System.Text.Encoding.UTF8, 
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                },
                                EnableRaisingEvents = true
                            };

                            process.OutputDataReceived += Process_OutputDataReceived;

                            process.Exited += (sender2, e2) =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show("Your mods have updated!", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                                    Task t = WriteToFileAsync($"log {DateTime.Today.ToString()}", new Dictionary<string, string> { { "log", output.Text.ToString() } });
                                });
                            };

                            process.Start();
                            process.BeginOutputReadLine(); 

                        }
                        else
                        {
                            MessageBox.Show("Could not find \"profile.json\" in Directory!\nMake sure you specify the Modrinth profile folder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        



                    }
                    else
                    {
                        MessageBox.Show("One of directories does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
                else
                {
                    MessageBox.Show("Version number is too short", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                }
            }
            else if(is_modrinth_launcher.IsChecked == false)
            {
                string scriptExecutablePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modpackupdater.exe");

                if (version.Text.Length > 1)
                {
                    if (Directory.Exists(folder_from.Text) && Directory.Exists(folder_to.Text))
                    {
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        data.Add("version", version.Text);
                        data.Add("token", token.Text);
                        data.Add("folder_from", folder_from.Text);
                        data.Add("folder_to", folder_to.Text);

                        Task task = WriteToFileAsync("cache.json", data);
                        output.Text = "";
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = scriptExecutablePath,
                                Arguments = $"\"{version.Text}\" \"{folder_from.Text}\" \"{folder_to.Text}\" \"{token.Text}\"",
                                RedirectStandardOutput = true,
                                StandardOutputEncoding = System.Text.Encoding.UTF8,   
                                UseShellExecute = false,
                                CreateNoWindow = true,
                            },
                            EnableRaisingEvents = true
                        };

                        process.OutputDataReceived += Process_OutputDataReceived;

                        process.Exited += (sender2, e2) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("Your mods have updated!", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                                Task t = WriteToFileAsync($"log {DateTime.Today.ToString()}", new Dictionary<string, string> { { "log", output.Text.ToString() } });
                            });
                        };

                        process.Start();
                        process.BeginOutputReadLine();



                    }
                    else
                    {
                        MessageBox.Show("One of directories does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
                else
                {
                    MessageBox.Show("Version number is too short", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                }
            }



        }

        private async Task<Dictionary<string, string>> LoadAndStoreTokenAsync()
        {
            return await LoadTokenAsync();
        }

        private async Task<Dictionary<string, string>> LoadTokenAsync()
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache.json");
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string content = await reader.ReadToEndAsync();
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                    return data;
                }
            }
            return null;
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string sanitizedData = Regex.Replace(e.Data, @"[^\u0020-\u007E]", " ");

                Dispatcher.Invoke(() =>
                {
                    output.Text += sanitizedData + Environment.NewLine;

                    if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        scrollViewer.ScrollToEnd();
                    }
                });
            }
        }




        public async Task WriteToFileAsync(string file_name, Dictionary<string, string> data)
        {
            string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file_name), false))
            {
                await writer.WriteAsync(json);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            scrollViewer.ScrollToEnd();
        }

        private void open_from_folder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(folder_from.Text))
            {
                Process.Start("explorer.exe", folder_from.Text);
            }
            else
            {
                MessageBox.Show("Directory does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
           

        }

        private void open_to_folder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(folder_to.Text))
            {
                Process.Start("explorer.exe", folder_to.Text);
            }
            else
            {
                MessageBox.Show("Directory does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
              
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (folder_to.Text.Length > 0 && Directory.Exists(folder_to.Text))
            {
                dialog.InitialDirectory = folder_to.Text;
            }
            else
            {
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            }
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                folder_to.Text = dialog.FileName;
            }
           
        }

        private void BrowseButton_Click2(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if(folder_from.Text.Length > 0 && Directory.Exists(folder_from.Text))
            {
                dialog.InitialDirectory = folder_from.Text;
            } else
            {
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            }
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                folder_from.Text = dialog.FileName;
            }
        }

        private void is_modrinth_launcher_Checked(object sender, RoutedEventArgs e)
        {
            if(is_modrinth_launcher.IsChecked == true)
            {
                folder_from_label.Text = "Modrinth profile folder";
            } else if(is_modrinth_launcher.IsChecked == false) 
            {
                folder_from_label.Text = "Folder with your old mods";

            }
        }
    }
}
