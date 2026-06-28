using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        string exeDir = AppContext.BaseDirectory;
        string configPath = Path.Combine(exeDir, "config.ini");
        string jaesJar = Path.Combine(exeDir, "JAES.jar");
        string keyPath = Path.Combine(appData, "JAES", "key", "private.pem");

        var javaArgs = new List<string>();

        // config.ini 読み込み
        IniFile.IniFile? ini = null;

        if (File.Exists(configPath))
        {
            ini = new IniFile.IniFile(configPath);

            // 作業ディレクトリ変更
            var dir = ini.GetString("General", "SetWorkDir", "");
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Directory.SetCurrentDirectory(dir);
            }
        }

        // --cwd がある場合はさらに作業ディレクトリ変更
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--cwd")
            {
                Directory.SetCurrentDirectory(args[i + 1]);
                i++;
            }
        }

        // JAES.jar がexe横にない場合、config.iniのJarPathを見る
        if (!File.Exists(jaesJar))
        {
            if (ini == null)
            {
                Console.Error.WriteLine(
                    "JAES.jar not found and config.ini does not exist.\n" +
                    "Please place JAES.jar in the same directory as this executable,\n" +
                    "or create config.ini to specify the path."
                );
                WaitInput();
                return 1;
            }

            var configuredJar = ini.GetString("JAES", "JarPath", "");
            if (!string.IsNullOrEmpty(configuredJar))
            {
                jaesJar = configuredJar;
            }
        }

        // config.ini がある場合、公開鍵・ポータブルモード確認
        if (ini != null)
        {
            var publicKey = ini.GetString("General", "PublicKey", "");

            if (!string.IsNullOrEmpty(publicKey))
            {
                if (!File.Exists(publicKey))
                {
                    Console.Error.WriteLine(
                        "The specified public key cannot be found.\n" +
                        "Please check the path in config.ini under [General] PublicKey."
                    );
                    WaitInput();
                    return 1;
                }

                javaArgs.Add(publicKey);
            }

            var portableMode = ini.GetBool("General", "PortableMode");

            if (portableMode)
            {
                string portableKeyDir = Path.Combine(exeDir, "JAES-conf", "key");

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
                    WaitInput();
                    return 1;
                }

                keyPath = Path.Combine(portableKeyDir, "private.pem");

                javaArgs.Add("--portable");
                javaArgs.Add(portableKeyDir);
            }
        }

        // 秘密鍵確認
        if (!File.Exists(keyPath))
        {
            Console.Error.WriteLine(
                "The private key cannot be found.\n" +
                "Generating a new key pair.\n" +
                "※ Existing encrypted data will no longer be decryptable."
            );
        }

        // JAES.jar 最終確認
        if (!File.Exists(jaesJar))
        {
            Console.Error.WriteLine(
                "JAES.jar not found.\n" +
                "Please place JAES.jar in the same directory as this executable,\n" +
                "or specify the path in config.ini under [JAES] JarPath."
            );
            WaitInput();
            return 1;
        }

        // Java起動
        var psi = new ProcessStartInfo
        {
            FileName = "java",
            UseShellExecute = false
        };

        psi.ArgumentList.Add("-Xmx1g");
        psi.ArgumentList.Add("-jar");
        psi.ArgumentList.Add(jaesJar);

        foreach (var arg in javaArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        try
        {
            var proc = Process.Start(psi);

            if (proc == null)
            {
                Console.Error.WriteLine("Failed to start Java process.");
                WaitInput();
                return 1;
            }

            proc.WaitForExit();

            WaitInput();

            return proc.ExitCode;
        }
        catch (Win32Exception)
        {
            Console.Error.WriteLine(
                "Java Runtime Environment (JRE) is not installed or not found in PATH.\n" +
                "Please install Java (JDK 25 or compatible) and ensure the 'java' command is available in PATH.\n" +
                "https://jdk.java.net/25/"
            );
            WaitInput();
            return 1;
        }
    }

    static void WaitInput()
    {
        if (!Environment.UserInteractive)
            return;

        Console.WriteLine();
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}