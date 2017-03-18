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
								   DateTime.Now.ToShortDateString().Replace("/", "_") + ".txt";
            ValidateLoggingLocation();
            SyncLocation = ConfigurationManager.AppSettings["SyncLocation"];
            ValidateSyncLocation();
            BufferSize = int.Parse(ConfigurationManager.AppSettings["BufferSize"]);
		    TraceEnabled = bool.Parse(ConfigurationManager.AppSettings["TraceEnabled"]);

			TraceItems = new List<string>();
	    }

        public static string LogingLocation { get; private set; }
		public static string SyncLocation { get; private set; }
		public static int BufferSize { get; private set; }
		public static bool TraceEnabled { get; private set; }
		public static List<string> TraceItems { get; set; } 
		
        public static string GetRelativePath(string path)
        {
			var relativePath = path.Remove(0, SyncLocation.Length).Replace('\\','/');
	        return relativePath;
        }
        public static string GetLocalPath(string relativePath)
        {
            return String.Format("{0}{1}", SyncLocation, relativePath.Replace('/', '\\'));
        }
		
	    public static bool IsDirectory(string path)
	    {
		    try
		    {
			    return ((File.GetAttributes(path) & FileAttributes.Directory) != 0);
		    }
		    catch (FileNotFoundException) // when?
		    {
			    return false;
		    } 
	    }

	    public static bool IsFileLocked(string fullPath)
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
		public static bool ChangeFileAttributes(string fileName, string creationTimeTicks, string lastWriteTimeTicks, string isReadOnlyString)
		{
			//try
			var fullPath = GetLocalPath(fileName);
			var creationTime = new DateTime(long.Parse(creationTimeTicks));
			var lastWriteTime = new DateTime(long.Parse(lastWriteTimeTicks));
			var isReadOnly = bool.Parse(isReadOnlyString);

			File.SetCreationTimeUtc(fullPath, creationTime);
			File.SetLastWriteTimeUtc(fullPath, lastWriteTime);

			var fileAttributes = File.GetAttributes(fullPath);
			if (isReadOnly)
				fileAttributes |= FileAttributes.ReadOnly;

			File.SetAttributes(fullPath, fileAttributes);
			//catch(Exception ex) { return false; }
			return true;
		}



        #region PrivateMethods
        private static void ValidateLoggingLocation()
        {
            var dirPath = Path.GetDirectoryName(LogingLocation);
            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string fileName = Path.GetFileNameWithoutExtension(LogingLocation);
            string fileExt = Path.GetExtension(LogingLocation);

            for (int i = 1; ; ++i)
            {
                if (File.Exists(LogingLocation))
                    LogingLocation = Path.Combine(dirPath, fileName + "(" + i + ")" + fileExt);
                else
                    return;
            }

            //if (File.Exists(LogingLocation))
            //    File.Delete(LogingLocation);
        }
        private static void ValidateSyncLocation()
        {
            if (!Directory.Exists(SyncLocation))
                Directory.CreateDirectory(SyncLocation);
        }
        #endregion PrivateMethods
    }
}
