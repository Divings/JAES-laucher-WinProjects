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
        string extraJavaArgs = "";
        // 実行ファイルディレクトリ取得
        string exeDir = AppContext.BaseDirectory;
        string jaesJar = Path.Combine(exeDir, "JAES.jar");

        // --cwd オプションがある場合、カレントディレクトリを変更 (その後の処理に影響)
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

        // 秘密鍵の存在確認
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
           
            IniFile.IniFile ini;

            // 設定ファイル読み込み
           if (!File.Exists("config.ini"))
            {
                Console.Error.WriteLine(
                    "JAES.jar not found and config.ini does not exist.\n" +
                    "Please place JAES.jar in the same directory as this executable,\n" +
                    "or create config.ini to specify the path."
                );
                ErrorInput();
                return 1;
            }
            ini = new IniFile.IniFile("config.ini");
          
            // 設定ファイルからJAES.jarのパスを取得
            var configuredJar = ini.GetString("JAES", "JarPath", "");
            
            if (!string.IsNullOrEmpty(configuredJar))
            {
                jaesJar = configuredJar;
            }
        }

        // ポータブルモード確認
            IniFile.IniFile inis;
        // 設定ファイル読み込み

            if (File.Exists("config.ini"))
            {
                inis = new IniFile.IniFile("config.ini");
            // ポータブルモード設定確認
            var portablemode = inis.GetBool("General", "PortableMode");
            // ポータブルモードの場合
            if (portablemode)
            {
                // ポータブルモード用鍵ディレクトリ作成
                string portableKeyDir = Path.Combine(exeDir, "JAES-conf", "key");

                // ディレクトリ作成
                try
                {
                    Directory.CreateDirectory(portableKeyDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        "Failed to create key directory for portable mode.\n" +
                        portableKeyDir + "\n" +
                        ex.Message
                    );
                    ErrorInput();
                    return 1;
                }
                // Java引数にポータブルモード用鍵ディレクトリを追加
                extraJavaArgs = "--portable " + portableKeyDir;
                // Console.WriteLine("Portable Key Directory: " + portableKeyDir); 
            }
        }

        // 再度JAES.jarの存在確認(最終チェック)
        if (!File.Exists(jaesJar))
        {
            Console.Error.WriteLine(
                "JAES.jar not found.\n" +
                "Please place JAES.jar in the same directory as this executable,\n" +
                "or specify the path in config.ini under [JAES] JarPath."
            );
            ErrorInput();
            return 1;
        }

        // Javaプロセス起動
        var psi = new ProcessStartInfo
        {
            FileName = "java",
            Arguments ="-Xmx1g -jar \"" + jaesJar + "\" " +extraJavaArgs + " " +string.Join(" ", args),
            UseShellExecute = false
        };

        try
        {
            var proc = Process.Start(psi);
            proc.WaitForExit();
            if (proc == null)
            {
                Console.Error.WriteLine("Failed to start Java process.");
                ErrorInput();
                return 1;
            }
            return proc.ExitCode;
        }
        catch (Win32Exception) {             
            Console.Error.WriteLine(
                "Java Runtime Environment (JRE) is not installed or not found in PATH.\n" +
                "Please install Java (JDK 25 or compatible) and ensure the 'java' command is available in PATH.\n\"https://jdk.java.net/25/\""
            );
            ErrorInput();
            return 1;
        }
    }

    // エラー時の入力待ち
    static void ErrorInput()
    {
        if (!Environment.UserInteractive)
            return;

        Console.Error.WriteLine();
        Console.Error.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
