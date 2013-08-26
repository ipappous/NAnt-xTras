using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NantXtras.Tasks.Abstract;
using NantXtras.Utils;

namespace NantXtras.Tasks.Oracle
{
    [TaskName("sqlplus")]
    public class SqlplusTask : ExternalProgramScannerBase
    {
        public SqlplusTask()
        {
            base.ExeName = "sqlplus";

        }


        private string wrapperSQL = @"set linesize 1000
set verify off
set feedback 0
set serveroutput on size 1000000

begin
   dbms_output.put_line(to_char(sysdate, 'yyyy/mm/dd hh24:mi:ss')||': Start of &1 ...');
end;
/

set verify on
set feedback 1

@@""&1""

set verify off
set feedback 0
set serveroutput on size 1000000

begin
   dbms_output.put_line(to_char(sysdate, 'yyyy/mm/dd hh24:mi:ss')||': End   of &1 ...');
   dbms_output.put_line('----------------------------------------------------');
   dbms_output.put_line('--');
end;
/

set verify on
set feedback 1";


        private string wrapperSQLInLine = @"set linesize 1000
set verify off
set feedback 0
set serveroutput on size 1000000

begin
   dbms_output.put_line(to_char(sysdate, 'yyyy/mm/dd hh24:mi:ss')||': Start of InLine code execution ...');
end;
/

set verify on
set feedback 1

<<sqlScript>>

set verify off
set feedback 0
set serveroutput on size 1000000

begin
   dbms_output.put_line(to_char(sysdate, 'yyyy/mm/dd hh24:mi:ss')||': End   of InLine code execution ...');
   dbms_output.put_line('----------------------------------------------------');
   dbms_output.put_line('--');
end;
/

set verify on
set feedback 1

quit;";


        public override string ProgramArguments
        {
            get
            {
                switch (_execMode)
                {
                    case execMode.Directory:
                        return string.Format(" {0} @\"{1}\" ", DBConnection, RunScriptsPath);
                    case execMode.InLine:
                        return string.Format(" {0} @\"{1}\" ", DBConnection, TempSQLFile);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
            }
        }

        private string _dbconnection;
        private DirectoryInfo _workingDirectory;
        private bool _debug;
        private RawXml _sqlScript = null;


        [TaskAttribute("debug")]
        [BooleanValidator()]
        public bool Debug
        {
            get { return _debug; }
            set { _debug = value; }
        }


        [TaskAttribute("dbconnection")]
        [StringValidator(AllowEmpty = false)]
        public string DBConnection
        {
            get { return _dbconnection; }
            set { _dbconnection = value; }
        }

    
        private string RunSqlPath
        {
            get
            {
                if (WorkingDirectory.Exists)
                {
                    return Path.Combine(WorkingDirectory.FullName, "runfile.sql");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private string TempSQLFile
        {
            get
            {
                if (WorkingDirectory.Exists)
                {
                    return Path.Combine(WorkingDirectory.FullName, "tmp.sql");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private string RunScriptsPath
        {
            get
            {
                if (WorkingDirectory.Exists)
                {
                    return Path.Combine(WorkingDirectory.FullName, "runscripts.txt");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private void cleanup()
        {

            if (File.Exists(RunSqlPath))
            {
                File.Delete(RunSqlPath);
            }
            if (File.Exists(RunScriptsPath))
            {
                File.Delete(RunScriptsPath);
            }
            if (File.Exists(TempSQLFile))
            {
                File.Delete(TempSQLFile);
            }
            
        }

        enum execMode
        {
            None,
            Directory,
            InLine
        }

        private execMode _execMode;


        /// <summary>
        /// The directory in which the command will be executed.
        /// </summary>
        /// <value>
        /// The directory in which the command will be executed. The default 
        /// is the project's base directory.
        /// </value>
        /// <remarks>
        /// <para>
        /// The working directory will be evaluated relative to the project's
        /// base directory if it is relative.
        /// </para>
        /// </remarks>
        [TaskAttribute("workingdir")]
        public DirectoryInfo WorkingDirectory
        {
            get
            {
                if (_workingDirectory == null)
                {
                    return base.BaseDirectory;
                }
                return _workingDirectory;
            }
            set { _workingDirectory = value; }
        }


        /// <summary>
        /// The sql script to execute, for inline execution
        /// if that is defined then only this code would be executed. 
        /// </summary>
        [BuildElement("sqlscript")]
        public RawXml SqlScript
        {
            get { return _sqlScript; }
            set { _sqlScript = value; }
        }

        /// <summary>
        /// Performs additional checks after the task has been initialized.
        /// </summary>
        /// <exception cref="BuildException"><see cref="FileName" /> does not hold a valid file name.</exception>
        protected override void Initialize()
        {
            base.Initialize();


            if (!WorkingDirectory.Exists)
            {
                _execMode =execMode.None;
                return;
            }

            cleanup();

            string filesToRun = "";
            if (SqlScript != null && !string.IsNullOrEmpty(SqlScript.Xml.InnerText))
            {
                string sqlToExecute = wrapperSQLInLine.Replace("<<sqlScript>>", SqlScript.Xml.InnerText);
                File.WriteAllText(TempSQLFile, sqlToExecute);
                _execMode = SqlplusTask.execMode.InLine;
            }
            else
            {
                FileSet fset = new FileSet();
                fset.BaseDirectory = WorkingDirectory;
                fset.Includes.Add(@"**\*.sql");
                fset.Includes.Add(@"**\*.prc");
                fset.Includes.Add(@"**\*.pkg");

                foreach (string fileName in fset.FileNames)
                {
                    filesToRun += string.Format(@"START RUNFILE.SQL ""{0}""{1}", fileName, Environment.NewLine);
                }

                if (filesToRun.Length ==0)
                {
                    _execMode = execMode.None;
                    return;
                }
                filesToRun += "quit;" + Environment.NewLine;
                Log(Level.Verbose, "Creating runscripts file: " + RunScriptsPath);
                //save runsqlfile
                File.WriteAllText(RunScriptsPath, filesToRun);
                Log(Level.Verbose, "Creating runfile: " + RunSqlPath);
                //save runsqlfile
                File.WriteAllText(RunSqlPath, wrapperSQL);
                _execMode = SqlplusTask.execMode.Directory;
            }


        }


        /// <summary>
        /// Executes the external program.
        /// </summary>
        protected override void ExecuteTask()
        {

            Log(Level.Info, "Executing Scripts in: " + WorkingDirectory);
            if (Debug)
            {
                switch (_execMode)
                {
                    case execMode.Directory:
                        Log(Level.Info, Environment.NewLine + "Debug mode, the following files would be executed in running mode:");
                        Log(Level.Info, File.ReadAllText(RunScriptsPath));
                        break;
                    case execMode.InLine:
                        Log(Level.Info, Environment.NewLine + "Debug mode, the inline script converted to the following file:");
                        Log(Level.Info, TempSQLFile);
                        break;
                   default:
                        throw new ArgumentOutOfRangeException();
                }
                if (ResultProperty != null)
                {
                    Properties[ResultProperty] = 0.ToString();
                }
                return;
            }

            if (_execMode == execMode.None)
            {
                throw new BuildException("There is nothing to execute!!!");
            }

            try
            {
                base.ExecuteTask();
                Log(Level.Info, "Scripts Executed in: " + WorkingDirectory);

            }
            catch (Exception ex)
            {
                Log(Level.Error, "Failed to Execute Scripts in: " + WorkingDirectory);
                throw ex;
            } finally
            {

                if (!Debug)
                {
                    cleanup();
                }  
            }
           

        }

        protected override void PrepareProcess(System.Diagnostics.Process process)
        {
            base.PrepareProcess(process);

            // set working directory specified by user
            process.StartInfo.WorkingDirectory = WorkingDirectory.FullName;


        }
    }
}
