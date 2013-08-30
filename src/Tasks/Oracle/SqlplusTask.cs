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

        enum execMode
        {
            None,
            Directory,
            FileSet,
            InLine
        }

        #region Private Instance Fields

        private string _dbconnection;
        private DirectoryInfo _workingDirectory;
        private bool _debug;
        private RawXml _sqlScript = null;
        private FileSet _fileset = null;
        private execMode _execMode;

        #endregion Private Instance Fields

        #region Private Static Fields

        const string wrapperSQL = @"set linesize 1000
set verify off
set feedback 0
set serveroutput on size 1000000

begin
   dbms_output.put_line(to_char(sysdate, 'yyyy/mm/dd hh24:mi:ss')||': Start of &1 ...');
end;
/

set verify on
set feedback 1

@""&1""

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


        const string wrapperSQLInLine = @"set linesize 1000
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

        #endregion Private Static Fields

        #region Public Instance Properties

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

        [BuildElement("fileset")]
        public virtual FileSet ExecuteFileSet
        {
            get { return _fileset; }
            set { _fileset = value; }
        }


        #endregion Public Instance Properties
        
        #region Private Instance Methods


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

        #endregion Private Instance Methods

        #region Override implementation of Task
        



        /// <summary>
        /// Performs additional checks after the task has been initialized.
        /// </summary>
        /// <exception cref="BuildException"><see cref="FileName" /> does not hold a valid file name.</exception>
        protected override void Initialize()
        {
            base.Initialize();

            _execMode = execMode.None;
            if (!WorkingDirectory.Exists)
            {
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
                FileSet fset = null;
                if (_fileset != null)
                {
                    fset = ExecuteFileSet;
                    _execMode = SqlplusTask.execMode.FileSet;
                }
                else
                {
                    fset = new FileSet();
                    fset.BaseDirectory = WorkingDirectory;
                    fset.Includes.Add(@"**\*.sql");
                    fset.Includes.Add(@"**\*.prc");
                    fset.Includes.Add(@"**\*.pck");
                    _execMode = SqlplusTask.execMode.Directory;

                }

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
            }


        }

        public override string ProgramArguments
        {
            get
            {
                switch (_execMode)
                {
                    case execMode.Directory:
                    case execMode.FileSet:
                        return string.Format(" {0} @\"{1}\" ", DBConnection, RunScriptsPath);
                    case execMode.InLine:
                        return string.Format(" {0} @\"{1}\" ", DBConnection, TempSQLFile);
                    default:
                        throw new ArgumentOutOfRangeException();
                }

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
                        case  execMode.FileSet:
                        Log(Level.Info, Environment.NewLine + "Debug mode, the following files would be selected from File Set and executed:");
                        Log(Level.Info, File.ReadAllText(RunScriptsPath));
                        break;
                        case execMode.Directory:
                        Log(Level.Info, Environment.NewLine + "Debug mode, the following files would be executed in running mode:");
                        Log(Level.Info, File.ReadAllText(RunScriptsPath));
                        break;
                    case execMode.InLine:
                        Log(Level.Info, Environment.NewLine + "Debug mode, the inline script converted to the following file:");
                        Log(Level.Info, TempSQLFile);
                        break;
                    case execMode.None:
                        if (this.FailOnError)
                        {
                            throw new BuildException("There is nothing to execute!!! in:" + WorkingDirectory);
                        }
                        else
                        {
                            Log(Level.Error, Environment.NewLine + "Debug mode, There is nothing to execute!!! in:" + WorkingDirectory);
                        }
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
                if (this.FailOnError)
                {
                    throw new BuildException("There is nothing to execute!!! in:" + WorkingDirectory);
                }
                else
                {
                    Log(Level.Error, Environment.NewLine + "There is nothing to execute!!! in:" + WorkingDirectory);
                }
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

        #endregion Override implementation of Task
    }
}
