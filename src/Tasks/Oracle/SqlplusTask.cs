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
using NantXtrasTasks.Utils;

namespace NantXtrasTasks.Tasks.Oracle
{
    [TaskName("sqlplus")]
    public class SqlplusTask : ExternalProgramBase
    {
        public SqlplusTask()
        {
            base.ExeName = "sqlplus";

        }



        public class ExcludeError : Element
        {
            private string _pattern;

            /// <summary>
            /// The pattern or file name to exclude.
            /// </summary>
            [TaskAttribute("pattern", Required = true)]
            [StringValidator(AllowEmpty = false)]
            public virtual string Pattern
            {
                get { return _pattern; }
                set { _pattern = value; }
            }

        }

        public class IncludeError : ExcludeError
        {
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

        private FileInfo _output;
        private bool _outputAppend;
        private string _resultProperty;
        private string _dbconnection;
        private EnvironmentSet _environmentSet = new EnvironmentSet();
        private DirectoryInfo _workingDirectory;
        private bool _debug;
        private List<string> _includeErrorPatterns = new List<string>();
        private List<string> _excludeErrorPatterns = new List<string>();
        private RawXml _sqlScript = null;

        [BuildElementArray("includeerrorpattern")]
        public IncludeError[] IncludeErrorPattern
        {
            set
            {
                foreach (IncludeError includeErrorPattern in value)
                {
                    _includeErrorPatterns.Add(includeErrorPattern.Pattern);
                }
            }

        }




        [BuildElementArray("excludeerrorpattern")]
        public ExcludeError[] ExcludeErrorPattern
        {
            set
            {
                foreach (ExcludeError excludeErrorPattern in value)
                {
                    _excludeErrorPatterns.Add(excludeErrorPattern.Pattern);
                }
            }
        }

        /// <summary>
        /// <para>
        /// The name of a property in which the exit code of the program should 
        /// be stored. Only of interest if <see cref="Task.FailOnError" /> is 
        /// <see langword="false" />.
        /// </para>
        /// <para>
        /// If the exit code of the program is "-1000" then the program could 
        /// not be started, or did not exit (in time).
        /// </para>
        /// </summary>
        [TaskAttribute("resultproperty")]
        [StringValidator(AllowEmpty = false)]
        public string ResultProperty
        {
            get { return _resultProperty; }
            set { _resultProperty = value; }
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

        /// <summary>
        /// Gets or sets a value indicating whether the application should be
        /// spawned. If you spawn an application, its output will not be logged
        /// by NAnt. The default is <see langword="false" />.
        /// </summary>
        //[TaskAttribute("spawn")]
        //public override bool Spawn
        //{
        //    get { return base.Spawn; }
        //    set { base.Spawn = value; }
        //}

        /// <summary>
        /// The name of a property in which the unique identifier of the spawned
        /// application should be stored. Only of interest if <see cref="Spawn" />
        /// is <see langword="true" />.
        /// </summary>
        //[TaskAttribute("pidproperty")]
        //[StringValidator(AllowEmpty = false)]
        //public string ProcessIdProperty
        //{
        //    get { return _processIdProperty; }
        //    set { _processIdProperty = value; }
        //}

        /// <summary>
        /// The file to which the standard output will be redirected.
        /// </summary>
        /// <remarks>
        /// By default, the standard output is redirected to the console.
        /// </remarks>
        [TaskAttribute("output")]
        public override FileInfo Output
        {
            get { return _output; }
            set { _output = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether output should be appended 
        /// to the output file. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if output should be appended to the <see cref="Output" />; 
        /// otherwise, <see langword="false" />.
        /// </value>
        [TaskAttribute("append")]
        public override bool OutputAppend
        {
            get { return _outputAppend; }
            set { _outputAppend = value; }
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
            Directory,
            InLine
        }

        private execMode _execMode;
        /// <summary>
        /// Performs additional checks after the task has been initialized.
        /// </summary>
        /// <exception cref="BuildException"><see cref="FileName" /> does not hold a valid file name.</exception>
        protected override void Initialize()
        {
            base.Initialize();
            //ErrorWriter = new LogWriter(this, Level.Info,
            //                            CultureInfo.InvariantCulture);

            //OutputWriter = new LogWriter(this, Level.Info,
            //                             CultureInfo.InvariantCulture);

            ErrorWriter = new ScanningTextWriter(this,_includeErrorPatterns,_excludeErrorPatterns);

            OutputWriter = new ScanningTextWriter(this, _includeErrorPatterns, _excludeErrorPatterns);

            if (!WorkingDirectory.Exists)
            {
                return;
            }

            cleanup();

            string filesToRun = "";
            if (SqlScript!=null && !string.IsNullOrEmpty(SqlScript.Xml.InnerText))
            {
                string sqlToExecute = wrapperSQLInLine.Replace("<<sqlScript>>", SqlScript.Xml.InnerText);
                File.WriteAllText(TempSQLFile, sqlToExecute);
                _execMode = execMode.InLine;
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
                filesToRun += "quit;" + Environment.NewLine;
                Log(Level.Verbose, "Creating runscripts file: " + RunScriptsPath);
                //save runsqlfile
                File.WriteAllText(RunScriptsPath, filesToRun);
                Log(Level.Verbose, "Creating runfile: " + RunSqlPath);
                //save runsqlfile
                File.WriteAllText(RunSqlPath, wrapperSQL);
                _execMode = execMode.Directory;
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
            Log(Level.Info, Environment.NewLine+ "Debug mode, the following files would be executed in running mode:");
            Log(Level.Info, File.ReadAllText(RunScriptsPath));
            if (ResultProperty != null)
            {
                Properties[ResultProperty] = (-1001).ToString();
            }
            return;
        }

        base.ExecuteTask();


        if (ResultProperty != null)
        {
            Properties[ResultProperty] = base.ExitCode.ToString(
                CultureInfo.InvariantCulture);
        }

        if (!Debug)
        {
            cleanup();
        }
        ScanningTextWriter err = (ScanningTextWriter) ErrorWriter;
        ScanningTextWriter outw = (ScanningTextWriter)OutputWriter;
        if (string.IsNullOrEmpty(err.Errors) && string.IsNullOrEmpty(outw.Errors))
        {
            Log(Level.Info, "Scripts Executed in: " + WorkingDirectory);
            
        }else
        {
            string errMsg = "Critical errors found executing Scripts in Directory: " + WorkingDirectory +":"+ Environment.NewLine;
            errMsg += err.Errors;
            errMsg += outw.Errors;
            if (ResultProperty != null)
            {
                Properties[ResultProperty] = (-1001).ToString();
            }
            throw new BuildException(errMsg);
            
        }


    }

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
        /// Environment variables to pass to the program.
        /// </summary>
        [BuildElement("environment")]
        public EnvironmentSet EnvironmentSet
        {
            get { return _environmentSet; }
        }

        protected override void PrepareProcess(System.Diagnostics.Process process)
        {
            base.PrepareProcess(process);

            // set working directory specified by user
            process.StartInfo.WorkingDirectory = WorkingDirectory.FullName;

            // set environment variables

            foreach (EnvironmentVariable variable in EnvironmentSet.EnvironmentVariables)
            {
                if (variable.IfDefined && !variable.UnlessDefined)
                {
                    if (variable.Value == null)
                    {
                        process.StartInfo.EnvironmentVariables[variable.VariableName] = "";
                    }
                    else
                    {
                        process.StartInfo.EnvironmentVariables[variable.VariableName] = variable.Value;
                    }
                }
            }
        }
    }
}
