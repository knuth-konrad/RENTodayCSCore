using System;

using libBAUtilCoreCS;
using static libBAUtilCoreCS.ConsoleHelper;
using static libBAUtilCoreCS.FilesystemHelper;
using static libBAUtilCoreCS.StringHelper;
using libBAUtilCoreCS.Utils.Args;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using NLog.LayoutRenderers;
using System.Collections;

namespace RENToday
{
   internal class Program
   {
      // Parameters
      public const Int32 PARAM_MIN = 1; // # of mandatory parameters

      /// <summary>
      /// Application exit codes (%ERRORLEVEL%)
      /// </summary>
      enum AppResult
      {
         /// <summary>
         /// Operation successful
         /// </summary>
         OKSuccess = 0,
         /// <summary>
         /// Less than minimum # of parameters supplied
         /// </summary>
         TooFewParameters = 1,
         /// <summary>
         /// At least 1 mandatory parameter is missing
         /// </summary>
         MissingMandatoryParameter = 2,
         /// <summary>
         /// A supplied value is invalid
         /// </summary>
         InvalidParameterValue = 3,
         /// <summary>
         /// File doesn't exist
         /// </summary>
         FileDoesNotExist = 4,
         /// <summary>
         /// Folder doesn't exist
         /// </summary>
         FolderDoesNotExist = 5
      }

      /// <summary>
      /// What to do, rename single file or files in directory.
      /// </summary>
      enum FileAction
      {
         /// <summary>
         /// Rename a single file
         /// </summary>
         RenameFile = 0,
         /// <summary>
         /// Rename all files in a folder
         /// </summary>
         RenameDirectory = 1
      }

      struct Parameters
      {
         public FileAction FileAction;
         public Boolean Overwrite;
         public String Prefix;
         public Boolean RecurseSubdirectories;
      }

      static Int32 Main(string[] args)
      {
         // Application intro
         AppIntro("RENToday");
         AppCopyright();


         // ** Parse the command line parameters **

         // All valid parameters
         // /f - File name
         // - or -
         // /d - directory with file specification, e.g. C:\MyData\*.txt
         // /o - Overwrite existing file with the same name
         // /p - File (name) prefix.
         // /s = Recurse subdirectories
         List<string> paramListAll = new List<string>() { "f", "d", "o", "p", "s" };

         CmdArgs cmd = new CmdArgs(paramListAll);
         cmd.Initialize();

         // * Validate what we have
         // Too few parameters?
         if (cmd.ParametersCount < PARAM_MIN)
         {
            WriteIndent(String.Format("!!! Too few parameters. Mandatory parameters: {0}, parameters supplied: {1}", PARAM_MIN, cmd.ParametersCount.ToString()), 2);
            ShowHelp();
            return (Int32)AppResult.TooFewParameters;
         }

         // Missing mandatory parameter?
         if (!cmd.HasParameter("f") && !cmd.HasParameter("d"))
         {
            String tmp = String.Format("!!! Mandatory parameter missing. Either {0}'f' or {0}'d' is required", new CmdArgs().DelimiterArgs);
            WriteIndent(tmp, 2);
            ShowHelp();
            return (Int32)AppResult.MissingMandatoryParameter;
         }

         // * Rename a single file (/f) or all files in a directory (/d)?
         // /f takes precedence over /d
         Parameters parameters = new Parameters();
         if (cmd.HasParameter("d"))
            parameters.FileAction = FileAction.RenameDirectory;

         if (cmd.HasParameter("f"))
            parameters.FileAction = FileAction.RenameFile;

         // Validate the passed values
         if (parameters.FileAction == FileAction.RenameFile)
         {
            KeyValue o = cmd.GetParameterByName("f");
            
            try
            {
               String tmp = o.Value.ToString();
               if (tmp.ToLower() == System.Boolean.TrueString.ToLower())
               {
                  String msg = String.Format("Missing filename: {0}", o.OriginalParameter);
                  WriteIndent(msg, 2);
                  ShowHelp();
                  return (Int32)AppResult.MissingMandatoryParameter;
               }
            }
            catch
            {
               String msg = String.Format("{0}'f' isn't a valid file name: {1}", cmd.DelimiterArgs, o.ValueText);
               WriteIndent(msg, 2);
               ShowHelp();
               return (Int32)AppResult.MissingMandatoryParameter;
            }
         }

         if (parameters.FileAction == FileAction.RenameDirectory)
         {
            KeyValue o = cmd.GetParameterByName("d");

            try
            {
               String tmp = o.Value.ToString();
               // Parameter /d passed w/o a value.
               if (tmp.ToLower() == System.Boolean.TrueString.ToLower())
               {
                  String msg = String.Format("Missing directory/file specification: {0}", o.OriginalParameter);
                  WriteIndent(msg, 2);
                  ShowHelp();
                  return (Int32)AppResult.MissingMandatoryParameter;
               }
            }
            catch
            {
               String msg = String.Format("{0}'d' isn't a valid  directory/file specification: {1}", cmd.DelimiterArgs, o.ValueText);
               WriteIndent(msg, 2);
               ShowHelp();
               return (Int32)AppResult.MissingMandatoryParameter;
            }
         }


         // Overwrite existing files?
         if (cmd.HasParameter("o"))
               parameters.Overwrite = true;

         // Add static filename prefix?
         if (cmd.HasParameter("p"))
            parameters.Prefix = (String)cmd.GetValueByName("p");

         // Recurse subdirectories?
         if (cmd.HasParameter("s"))
            parameters.RecurseSubdirectories= true;

         // Echo the command line parameters
         if (parameters.FileAction == FileAction.RenameFile)
            Console.WriteLine("Source file           : {0}", (String)cmd.GetValueByName("f"));
         if (parameters.FileAction == FileAction.RenameDirectory)
            Console.WriteLine("Source directory      : {0}", (String)cmd.GetValueByName("d"));
         Console.WriteLine("Overwrite             : {0}", parameters.Overwrite.ToString());
         Console.WriteLine("Recurse subdirectories: {0}", parameters.RecurseSubdirectories.ToString());
         if (parameters.Prefix != null && parameters.Prefix.Trim().Length > 0)
            Console.WriteLine("Prefix                : {0}", parameters.Prefix);
         else
            Console.WriteLine("Prefix                : <none>");
         BlankLine(true);


         // *** We're in business. Rename a single file or files in a folder?
         Int32 fileCount = 0;
         
         if (parameters.FileAction == FileAction.RenameFile)
         {
            // *** Rename a single file
            String file = cmd.GetValueByName("f").ToString();
            if (!FilesystemHelper.FileExists(file)) 
            {
               ConsoleHelper.WriteError(String.Format("File {0} not found.", file));
               return (Int32)AppResult.FileDoesNotExist;
            }

            AppResult result = RenFile(file, parameters);
            if (result != AppResult.OKSuccess)
            {
               BlankLine();
               WriteError("An error occurred during the renaming operation");
               BlankLine();
            }
            else
               fileCount = 1;

         }
         else if (parameters.FileAction == FileAction.RenameDirectory)
         {
            // *** Rename multiple files in a folder
            String folder = cmd.GetValueByName("d").ToString();
            fileCount = RenDirectory(folder, parameters);
         }


         // We're done, display the number of changed file names.
         BlankLine();
         Console.WriteLine("File(s) renamed: {0}", fileCount.ToString());
         
         return (Int32)AppResult.OKSuccess;


      }  // Main

      /// <summary>
      /// Renames a single file.
      /// </summary>
      /// <param name="fileSource">Rename this file.</param>
      /// <param name="prms">Relevant parameters passed via command line.</param>
      /// <returns><see cref="AppResult"/></returns>
      static AppResult RenFile(String fileSource, Parameters prms)
      {

         // ToDo: use NewFileName() to generate the file name, takes special case "*" in prefix into account
         
         String fileOrg = fileSource;

         // Create a string like <prefix>yyyyMMdd_HHnnss_sss
         String fileTemp = String.Empty;
         DateTime dtm = DateTime.Now;
         fileTemp = prms.Prefix
            + dtm.ToString("yyyy") + dtm.ToString("MM") + dtm.ToString("dd")
            + "_"
            + dtm.ToString("HH") + dtm.ToString("mm") + dtm.ToString("ss")
            + "_"
            + dtm.ToString("fff");

         String fileOld = Path.GetFileNameWithoutExtension(fileSource);
         fileSource = fileSource.Replace(fileOld, fileTemp);

         Console.WriteLine("- Scanning for file {0}", fileOrg);
         
         String fileNew = fileSource;

         if (FilesystemHelper.FileExists(fileNew) && prms.Overwrite == true)
         {
            try
            {
               Console.WriteLine("  Renaming {0}", fileOrg);
               Console.WriteLine("   -> {0}", fileNew);
               File.Delete(fileNew);
               File.Move(fileOrg, fileNew);
            }
            catch (Exception ex)
            {
               ConsoleHelper.WriteError(String.Format("Error renaming {0}\n{1}", fileNew, ex.InnerException.ToString()));
            }
         }  // if (FilesystemHelper.FileExists(fileNew) && prms.Overwrite == true)
         else if (FilesystemHelper.FileExists(fileNew) && prms.Overwrite == false)
         {
            Console.WriteLine("  Skipping ... {0} already exists", fileNew);
         }  
         else
         {
            try
            {
               Console.WriteLine("  Renaming {0}", fileOrg);
               Console.WriteLine("   -> {0}", fileNew);
               File.Delete(fileNew);
               File.Move(fileOrg, fileNew);
            }
            catch (Exception ex)
            {
               ConsoleHelper.WriteError(String.Format("Error renaming {0}\n{1}", fileNew, ex.Message));
            }
         }

         return AppResult.OKSuccess;
      }  // static AppResult RenFile

      /// <summary>
      /// Renames multiple files.
      /// </summary>
      /// <param name="filePattern">Rename all files matching this pattern.</param>
      /// <param name="prms">Relevant parameters passed via command line.</param>
      /// <returns><see cref="AppResult"/></returns>
      static Int32 RenDirectory(String filePattern, Parameters prms)
      {

         Int32 fileCount = 0;
         
         String folderSource = Path.GetDirectoryName(filePattern);
         Console.WriteLine("- Scanning folder {0}", folderSource);

         String filesSource = Path.GetFileName(filePattern);

         IEnumerable<String> files;
         if (prms.RecurseSubdirectories == false)
         {
            files = from file in Directory.EnumerateFiles(folderSource, filesSource, SearchOption.TopDirectoryOnly) select file;
         }
         else // if (prms.RecurseSubdirectories == false)
         {
            files = from file in Directory.EnumerateFiles(folderSource, filesSource, SearchOption.AllDirectories) select file;
         }

         foreach (String file in files)
         {
            String fileNew = NewFileName(file, prms);

            if (File.Exists(fileNew) && prms.Overwrite == true)
            {
               try
               {
                  fileCount += 1;
                  Console.WriteLine("  Renaming {0}", file);
                  Console.WriteLine("   -> {0}", fileNew);
                  File.Delete(fileNew);
                  File.Move(file, fileNew);
               }
               catch (Exception ex)
               {
                  fileCount -= 1;
                  ConsoleHelper.WriteError(String.Format("Error renaming {0}\n{1}", fileNew, ex.Message));
               }
            }
            else if (File.Exists(fileNew) && prms.Overwrite == false)
            {
               Console.WriteLine("   Skipping ... {0} already exists.", fileNew);
            }
            else
            {
               try
               {
                  fileCount += 1;
                  Console.WriteLine("  Renaming {0}", file);
                  Console.WriteLine("   -> {0}", fileNew);
                  File.Delete(fileNew);
                  File.Move(file, fileNew);
               }
               catch (Exception ex)
               {
                  fileCount -= 1;
                  ConsoleHelper.WriteError(String.Format("Error renaming {0}\n{1}", fileNew, ex.Message));
               }
            }

            // Pause process in order to achieve unique file names (milliseconds).
            System.Threading.Thread.Sleep(3);
         }

         return fileCount;

      }  // static AppResult RenDirectory

      static String NewFileName(String fileName, Parameters prms)
      {

         // Special character in prefix: *. If present, the original file name will be put in there
         String prefix = prms.Prefix;
         if (prefix.Contains("*"))
         {
            prefix = prefix.Replace("*", Path.GetFileNameWithoutExtension(fileName));
         }

         // Create a string like <prefix>yyyyMMdd_HHnnss_sss
         String fileTemp = String.Empty;
         DateTime dtm = DateTime.Now;
         fileTemp = prefix
            + dtm.ToString("yyyy") + dtm.ToString("MM") + dtm.ToString("dd")
            + "_"
            + dtm.ToString("HH") + dtm.ToString("mm") + dtm.ToString("ss")
            + "_"
            + dtm.ToString("fff");

         fileName = fileName.Replace(Path.GetFileNameWithoutExtension(fileName), fileTemp);

         return fileName;      
      
      }  // static String NewFileName


      /// <summary>
      /// Shows RENToday's usage and syntax help.
      /// </summary>
      static void ShowHelp()
      {

         CmdArgs o = new CmdArgs();
         string s = o.DelimiterArgs;

         BlankLine();
         Console.WriteLine("RENToday renames files to today's date.");

         BlankLine();
         Console.WriteLine("RENToday Usage:");
         Console.WriteLine("RENToday {0}f=<Filename>|{0}d=<directory with file specification> [{0}p=<prefix>] [{0}o] [{0}s]", s);
         BlankLine();
         Console.WriteLine(@"     e.g.: RENToday {0}f=d:\data\myfile.txtDummy", s);
         Console.WriteLine(@"           - Rename the single file d:\data\myfile.txt to 20020228_134228_623.txt, assuming today's date is");
         Console.WriteLine(@"             February 28th, 2002 and the time is 13:42:28 (and 623 milliseconds).");
         Console.WriteLine(@"           RENToday {0}d=d:\data\*.txt", s);
         Console.WriteLine(@"           - Rename each file with the file extension .txt in d:\data\ to 20020228_134228_623.txt, assuming today's date is");
         Console.WriteLine(@"             February 28th, 2002 and the time is 13:42:28 (and 623 milliseconds) when the first renaming occurs.");
         Console.WriteLine(@"             Of course, the time will be updated for each following renaming process.");
         Console.WriteLine(@"             RENToday implements a 3 millisecond delay internally to ensure that file names are unique.");
         BlankLine();
         Console.WriteLine(@"Please note: switch {0}f takes precedence over switch {0}d if both are supplied.", s);
         BlankLine();
         Console.WriteLine(@"           RENToday {0}f=d:\data\myfile.txt {0}p=MyPrefix_", s);
         Console.WriteLine(@"           - Rename d:\data\myfile.txt to MyPrefix_20020228.txt, assuming the above example's date & time.");
         Console.WriteLine(@"             Switch {0}p supports the special character '*'. If present, the original filename will be put at this", s);
         Console.WriteLine(@"             in <prefix>.");
         Console.WriteLine(@"             E.g. RENToday {0}f=d:\data\myfile.txt {0}p=MyPrefix_*_ will result in MyPrefix_myfile_20020228_134228_623.txt.", s);
         BlankLine();
         Console.WriteLine("{0}o - Overwrite existing files with the same name.", s);
         Console.WriteLine("{0}s - Recurse subdirectories. Only valid together with {0}d, will be ignored otherwise.", s);

      }  // void ShowHelp()

   }  // class Program
}
