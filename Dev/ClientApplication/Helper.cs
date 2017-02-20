using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ClientApplication
{
    public class Helper
    {
	    static Helper()
	    {
			LogingLocation = ConfigurationManager.AppSettings["LogingLocation"] + "Log_" +
								   DateTime.Now.ToShortDateString().Replace("/", "") + ".txt";
			SyncLocation = ConfigurationManager.AppSettings["SyncLocation"];
			StreamedPipeServerName = ConfigurationManager.AppSettings["StreamedPipeServerName"];
			BufferSize = int.Parse(ConfigurationManager.AppSettings["BufferSize"]);
		    TraceEnabled = bool.Parse(ConfigurationManager.AppSettings["TraceEnabled"]);

			TraceItems = new List<string>();
	    }

        public static string LogingLocation { get; private set; }
		public static string SyncLocation { get; private set; }
		public static string StreamedPipeServerName { get; private set; }
		public static int BufferSize { get; private set; }
		public static bool TraceEnabled { get; private set; }
		public static List<string> TraceItems { get; set; } 
		
        public static string GetRelativePath(String path)
        {
			var relativePath = path.Remove(0, SyncLocation.Length).Replace('\\','/');
	        return relativePath;
        }
        public static string GetLocalPath(string relativePath)
        {
            return String.Format("{0}{1}", SyncLocation, relativePath.Replace('/', '\\'));
        }

        public static bool IsDirectory(String path)
        {
            return ((File.GetAttributes(path) & FileAttributes.Directory) != 0);
        }
        public static bool IsFileLocked(String fullPath)
        {
            var fileInfo = new FileInfo(fullPath);
            FileStream stream = null;
            
            try
            {
                stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because: 
                //  -still being written to or being processed by another thread
                //  -does not exist (has already been processed)
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Exceptia aceasta apare cand fisierul este read-only
                // Solutie :
                //      1. Fisierul trecut prea ReadOnly = false (bineinteles: doar in path-ul de interes -> cel de sync)
                //      2. Returnat true (ca sa inlocuiasca IOException-ul gen)
                //      3. Trebuie avut grija sa fie dupaia trecut inapoi pe ReadOnly = true
                //          Idee: in loc de List<string> (lista de path-uri) -> List<clasa> ;clasa care contine FullLocalPath(string), WasReadOnly(bool), etc
                //                  iar cand WasReadOnly = true; inainte de dequeue trecut inapoi pe ReadOnly = true;

                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                    return true;
                }

                // Problema: Daca e aruncata din mai multe motive ? 
                //      1. Afla care ar putea fi motivele
                //      2. In between o sa fie verificarea aia cu if(fileInfo.IsReadOnly) ca sa te asiguri si sa te prinzi daca vine din alta parte 
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        public static void ValidateDirectoryForFile(string fileName)
        {
            var dirPath = Path.GetDirectoryName(fileName);
            var fullPath = SyncLocation + dirPath;

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

    }
}









//        public static List<InitialSyncFileHash> GetFilesHashes(List<string> paths)
//        {
//            var initialSyncFileHashes = new List<InitialSyncFileHash>();
//            using (var md5 = System.Security.Cryptography.MD5.Create())
//            {
//                foreach (var path in paths)
//                {
//                    using (var stream = File.OpenRead(path))
//                    {
//                        var info = new FileInfo(path);
//                        var infoStr = info.CreationTime.ToString(CultureInfo.InvariantCulture)
//                                      + info.LastWriteTime.ToString()
//                                      + info.Attributes.ToString()
//                                      + info.IsReadOnly.ToString();
//
//                        var infoHash = infoStr.GetHashCode();
//
//                        var md5Hash = BitConverter.ToInt32(md5.ComputeHash(stream), 0);
//
//                        md5Hash += infoHash;
//
//                        initialSyncFileHashes.Add(new InitialSyncFileHash(path, md5Hash.GetHashCode()));
//                    }
//                }
//            }
//
//            return initialSyncFileHashes;
//        }