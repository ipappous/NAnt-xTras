using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NantXtrasTasks.Tasks.Oracle;
using NantXtrasTasks.Utils;

namespace NantXtrasTasks.Tasks.Abstract
{
    [Serializable()]
    public abstract class ExternalProgramScannerBase : ExternalProgramBase
    {
  
        #region Private Instance Fields
              private string _resultProperty;
        private EnvironmentSet _environmentSet = new EnvironmentSet();
        private List<string> _includeErrorPatterns = new List<string>();
        private List<string> _excludeErrorPatterns = new List<string>();

        #endregion Private Instance Fields

        #region Public Static Fields

        /// <summary>
        /// Defines the exit code that will be returned by <see cref="ExitCode" />
        /// if the process could not be started, or did not exit (in time).
        /// </summary>
        public const int FailedExitCode = -1001;


        #endregion Public Static Fields

        #region Private Static Fields

        #endregion Private Static Fields

        #region Public Instance Properties
        
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

        /// <summary>
        /// Environment variables to pass to the program.
        /// </summary>
        [BuildElement("environment")]
        public EnvironmentSet EnvironmentSet
        {
            get { return _environmentSet; }
        }
        #endregion Public Instance Properties

        #region Override implementation of Task

        #endregion Override implementation of Task

        #region Public Instance Methods

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Updates the <see cref="ProcessStartInfo" /> of the specified 
        /// <see cref="Process"/>.
        /// </summary>
        /// <param name="process">The <see cref="Process" /> of which the <see cref="ProcessStartInfo" /> should be updated.</param>
        protected override void PrepareProcess(Process process)
        {
            base.PrepareProcess(process);
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

        /// <summary>
        /// Performs additional checks after the task has been initialized.
        /// </summary>
        /// <exception cref="BuildException"><see cref="FileName" /> does not hold a valid file name.</exception>
        protected override void Initialize()
        {
            base.Initialize();

            ErrorWriter = new ScanningTextWriter(this,_includeErrorPatterns,_excludeErrorPatterns);

            OutputWriter = new ScanningTextWriter(this, _includeErrorPatterns, _excludeErrorPatterns);


        }

        /// <summary>
        /// Executes the external program.
        /// </summary>
        protected override void ExecuteTask()
        {

            base.ExecuteTask();


            if (ResultProperty != null)
            {
                Properties[ResultProperty] = base.ExitCode.ToString(
                    CultureInfo.InvariantCulture);
            }

            ScanningTextWriter err = (ScanningTextWriter) ErrorWriter;
            ScanningTextWriter outw = (ScanningTextWriter)OutputWriter;
            if (!string.IsNullOrEmpty(err.Errors) || !string.IsNullOrEmpty(outw.Errors))
            {
                string errMsg = "Critical errors found during the execution of : " + ExeName + " " + ProgramArguments + ":" + Environment.NewLine;
                errMsg += err.Errors;
                errMsg += outw.Errors;
                if (ResultProperty != null)
                {
                    Properties[ResultProperty] = FailedExitCode.ToString();
                }
                throw new BuildException(errMsg);
            
            }


        }
    

      
        #endregion Protected Instance Methods

        #region Private Instance Methods


        #endregion Private Instance Methods
  
    }
}
