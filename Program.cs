using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        // APPDATA 取得（例: C:\Users\user\AppData\Roaming）
        string appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData
        );
        string exeDir = AppContext.BaseDirectory;
        string jaesJar = Path.Combine(exeDir, "JAES.jar");

        // --cwd 処理
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--cwd")
            {
                Directory.SetCurrentDirectory(args[i + 1]);
                i++; // skip dir
            }
        }

        // 秘密鍵パス: %APPDATA%\JAES\key\private.pem
        string keyPath = Path.Combine(
            appData, "JAES", "key", "private.pem"
        );

        if (!File.Exists(keyPath))
        {
            Console.Error.WriteLine(
                "The private key cannot be found.\n" +
                "Generating a new key pair.\n" +
                "※ Existing encrypted data will no longer be decryptable."
            );
        }

        // JAES.jar の存在確認
        //　存在しない場合設定ファイルからファイルpathを取得
        if (!File.Exists(jaesJar))
        {
            Console.WriteLine("ファイルは存在しません。");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = "-Xmx1g -jar \"" + jaesJar + "\" " + string.Join(" ", args),
            UseShellExecute = false
        };

        try
        {
            var proc = Process.Start(psi);
            proc.WaitForExit();
            if (proc == null)
            {
                Console.Error.WriteLine("Failed to start Java process.");
                return 1;
            }
            return proc.ExitCode;
        }
        catch (Win32Exception) {             
            Console.Error.WriteLine(
                "Java Runtime Environment (JRE) is not installed or not found in PATH.\n" +
                "Please install Java (JDK 25 or compatible) and ensure the 'java' command is available in PATH.\n\"https://jdk.java.net/25/\""
            );
            return 1;
        }
    }
}
