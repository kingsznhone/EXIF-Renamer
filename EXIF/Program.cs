using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
namespace EXIF
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            DirectoryInfo root = new DirectoryInfo(@"D:\Workspace\album");
            foreach(DirectoryInfo subDir in root.GetDirectories())
            {
                await CleanDir(subDir);
            }
        }

        public static async Task CleanDir(DirectoryInfo dir)
        {
            
            var filecollection = dir.GetFiles();
            await Parallel.ForAsync(0, filecollection.Length, async (i, CancellationToken) =>
            {
                FileInfo file = filecollection[i];
                if (file.Extension != ".jpg")
                {
                    return;
                }
                if (!await GetExif(file))
                {
                    Console.WriteLine($"No Exif Info: {file.FullName}");
                    WriteEXIF(file);
                }
                Console.WriteLine($"  {i + 1}/{filecollection.Length}");
            });
        }

        public static async ValueTask<bool> GetExif(FileInfo file)
        {
            Regex re = new Regex(@"^Date\/Time Original.*(\d{4}):(\d{2}):(\d{2}) (\d{2}):(\d{2}):(\d{2})");
            Process p = new Process();
            p.StartInfo.FileName = @".\Resources\exiftool.exe";
            p.StartInfo.Arguments = $"\"{file.FullName}\"";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();

            string[] lines = output.Split(['\n']);
            await p.WaitForExitAsync();

            foreach (string line in lines)
            {
                
                var match = re.Match(line);
                if (match.Success)
                {
                    var TimeText =  $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}" +
                                   $"_{match.Groups[4].Value}{match.Groups[5].Value}{match.Groups[6].Value}";
                    string newname = $"IMG_" + TimeText+ $"{file.Extension}";

                    newname = Path.Join(file.Directory.FullName, newname);

                    int counter = 1;
                    while (true)
                    {
                        newname = $"IMG_" + TimeText + $"_{counter}" + $"{file.Extension}";
                        newname = Path.Join(file.Directory.FullName, newname);
                        try
                        {
                            File.Move(file.FullName, newname);
                            break;
                        }
                        catch
                        {
                            counter++;
                        }
                    }
                    Console.Write($"Rename Success: {Path.GetFileName(newname)}");

                    
                    return true;
                }

            }
            return false;
        }

        public static void AddMinute(FileInfo file)
        {

            Process p = new Process();
            p.StartInfo.FileName = @".\Resources\exiftool.exe";
            p.StartInfo.Arguments = $"\"-AllDates+=00:00:00 00:01:00\" -verbose \"{file.FullName}\"";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();

            string[] lines = output.Split(new char[] { '\n' });
            p.WaitForExit();
        }
        public static void WriteEXIF(FileInfo file)
        {
            Regex re = new Regex(@"^IMG_(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2}).+(\.jpg)");
            var match = re.Match(file.Name);
            DateTime lastModified;
            if (match.Success)
            {
                string timeString = $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}" +
                    $"T{match.Groups[4].Value}:{match.Groups[5].Value}:{match.Groups[6].Value}Z";
                lastModified = DateTime.Parse(timeString);
            }
            else 
            { 
                lastModified = file.LastWriteTime; 
            }

            // 构建日期时间字符串
            string dateTimeString = lastModified.ToString("yyyy:MM:dd HH:mm:ss");

            // 构建要填充的字符串
            string arg = $"\"-DateTimeOriginal={dateTimeString}\"";

            arg += $" {file.FullName}";
            Process p = new Process();
            p.StartInfo.FileName = @".\Resources\exiftool.exe";
            p.StartInfo.Arguments = arg;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            Console.WriteLine($"EXIF Update: {file.Name}");
            GetExif(file);
            Console.WriteLine();
        }
    }
}