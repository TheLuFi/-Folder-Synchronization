
using System.Diagnostics;

Console.Write("Put the directory you want to copy:");
string copypath = Console.ReadLine();

Console.Write("Put the directory you want the copy file to go:");
string copiedpath = Console.ReadLine();

Console.Write(@"Put the directory you want the log file to go(please put the \ at the end):");
string logfilepath = Console.ReadLine();

Console.Write("How many seconds do you want the synchronization to happen:");
int intervalseconds = Convert.ToInt32(Console.ReadLine());

var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalseconds));

var source = new DirectoryInfo(copypath);
var destination = new DirectoryInfo(copiedpath);


// It loops the code
while (await timer.WaitForNextTickAsync())
{
    DeleteAll(source, destination, logfilepath: logfilepath);
    CopyFolderContents(copypath, copiedpath, "", true, true, logfilepath);

    // It reads the file log 
    using (StreamReader r = File.OpenText(logfilepath + "log.txt"))
    {
        DumpLog(r);
    }
}

//Makes sure that the log file doesn't delete himself
StreamWriter w = new StreamWriter(logfilepath + "log.txt", true);

Console.ReadLine();

static void CopyFolderContents(string sourceFolder, string destinationFolder, string mask, Boolean createFolders, Boolean recurseFolders, string logfilepath)
{
    try
    {
        var exDir = sourceFolder;
        var dir = new DirectoryInfo(exDir);
        var destDir = new DirectoryInfo(destinationFolder);

        SearchOption so = (recurseFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        foreach (string sourceFile in Directory.GetFiles(dir.ToString(), mask, so))
        {
            FileInfo srcFile = new FileInfo(sourceFile);
            string srcFileName = srcFile.Name;

            // Create a destination that matches the source structure
            FileInfo destFile = new FileInfo(destinationFolder + srcFile.FullName.Replace(sourceFolder, ""));

            if (!Directory.Exists(destFile.DirectoryName) && createFolders)
            {
                Directory.CreateDirectory(destFile.DirectoryName);
                using (StreamWriter w = File.AppendText(logfilepath + "log.txt"))
                {
                    Log($"Created: {destFile.DirectoryName}", w);
                }
            }

            //Check if src file was modified and modify the destination file
            if (srcFile.LastWriteTime > destFile.LastWriteTime || !destFile.Exists)
            {
                File.Copy(srcFile.FullName, destFile.FullName, true);
                using (StreamWriter w = File.AppendText(logfilepath + "log.txt"))
                {
                    Log($"Copied: {destFile.FullName}", w);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine(ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
    }
}

static void DeleteAll(DirectoryInfo source, DirectoryInfo target, string logfilepath)
{
    if (!source.Exists)
    {
        target.Delete(true);
        return;
    }

    // Delete each existing file in target directory not existing in the source directory.
    foreach (FileInfo fi in target.GetFiles())
    {
        var sourceFile = Path.Combine(source.FullName, fi.Name);
        if (!File.Exists(sourceFile)) //Source file doesn't exist, delete target file
        {
            fi.Delete();
            using (StreamWriter w = File.AppendText(logfilepath + "log.txt"))
            {
                if (!sourceFile.EndsWith("log.txt"))
                {
                    Log($"Deleted: {sourceFile.ToString()}", w);
                }
            }
        }
    }

    // Delete non existing files in each subdirectory using recursion.
    foreach (DirectoryInfo diTargetSubDir in target.GetDirectories())
    {
        DirectoryInfo nextSourceSubDir = new DirectoryInfo(Path.Combine(source.FullName, diTargetSubDir.Name));
        DeleteAll(nextSourceSubDir, diTargetSubDir, logfilepath);
    }
}

static void Log(string logMessage, TextWriter w)
{
    w.Write("\r\nLog Entry : ");
    w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
    w.WriteLine("  :");
    w.WriteLine($"  :{logMessage}");
    w.WriteLine("-------------------------------");
}

static void DumpLog(StreamReader r)
{
    string line;
    while ((line = r.ReadLine()) != null)
    {
        Console.WriteLine(line);
    }
}
