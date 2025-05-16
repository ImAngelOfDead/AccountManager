using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace AccountManager.Common
{
    public static class CreateZip
    {
        public static void CreateZipFile(string filesPath, string zipFilePath)
        {
            try
            {
                string[] files = Directory.GetFiles(filesPath);
                using ZipOutputStream zipOutputStream = new ZipOutputStream(File.Create(zipFilePath));
                zipOutputStream.SetLevel(9);
                byte[] buffer = new byte[4096];
                foreach (string path in files)
                {
                    zipOutputStream.PutNextEntry(new ZipEntry(Path.GetFileName(path))
                    {
                        DateTime = DateTime.Now
                    });
                    using FileStream fileStream = File.OpenRead(path);
                    int count;
                    do
                    {
                        count = fileStream.Read(buffer, 0, buffer.Length);
                        zipOutputStream.Write(buffer, 0, count);
                    } while (count > 0);
                }

                zipOutputStream.Finish();
                zipOutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Exception during processing {0}", ex);
            }
        }
    }
}