using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

using NantXtras.Utils;

namespace NantXtras.Tasks.Abstract
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


        public class ExcludeError : Element
        {
            private string _pattern;
            private bool _ifDefined = true;

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

            /// <summary>
            /// If <see langword="true" /> then the patterns will be excluded; 
            /// otherwise, skipped. The default is <see langword="true" />.
            /// </summary>
            [TaskAttribute("if")]
            [BooleanValidator()]
            public virtual bool IfDefined
            {
                get { return _ifDefined; }
                set { _ifDefined = value; }
            }

        }

        public class IncludeError : ExcludeError
        {
        }

        [BuildElementArray("includeerrorpattern")]
        public IncludeError[] IncludeErrorPattern
        {
            set
            {
                foreach (IncludeError includeErrorPattern in value)
                {
                    if (includeErrorPattern.IfDefined)
                    {
                        _includeErrorPatterns.Add(includeErrorPattern.Pattern);
                    }
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
                    if (excludeErrorPattern.IfDefined)
                    {
                        _excludeErrorPatterns.Add(excludeErrorPattern.Pattern);
                    }
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

        /// <summary>
        /// Performs additional checks after the task has been initialized.
        /// </summary>
        /// <exception cref="BuildException"><see cref="FileName" /> does not hold a valid file name.</exception>
        protected override void Initialize()
        {
            base.Initialize();

            ErrorWriter = new ScanningTextWriter(this, _includeErrorPatterns, _excludeErrorPatterns);

            OutputWriter = new ScanningTextWriter(this, _includeErrorPatterns, _excludeErrorPatterns);


        }

        /// <summary>
        /// Executes the external program.
        /// </summary>
        protected override void ExecuteTask()
        {

            Exception theEx = null;
            try
            {
                base.ExecuteTask();


                if (ResultProperty != null)
                {
                    Properties[ResultProperty] = base.ExitCode.ToString(
                        CultureInfo.InvariantCulture);
                }

            }
            catch (Exception ex)
            {
                theEx = ex;
            }
            finally
            {
                ScanningTextWriter err = (ScanningTextWriter) ErrorWriter;
                ScanningTextWriter outw = (ScanningTextWriter) OutputWriter;
                if (!string.IsNullOrEmpty(err.Errors) || !string.IsNullOrEmpty(outw.Errors))
                {
                    string errMsg = "Critical errors found during the execution of : " + ExeName + " " +
                                    CommandLine + ":" + Environment.NewLine;
                    errMsg += err.Errors;
                    errMsg += outw.Errors;
                    if (ResultProperty != null)
                    {
                        Properties[ResultProperty] = FailedExitCode.ToString();
                    }
                    throw new BuildException(errMsg, theEx);

                }
                else if (theEx != null)
                {
                    throw new BuildException("UnKnown Build Error:", theEx);
                }

            }



        }

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

        #endregion Override implementation of Task

        #region Public Instance Methods

        #endregion Public Instance Methods

        #region Protected Instance Methods
      
        #endregion Protected Instance Methods

        #region Private Instance Methods


        #endregion Private Instance Methods
  
    }
}
