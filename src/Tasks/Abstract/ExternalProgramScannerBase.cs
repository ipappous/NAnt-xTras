using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NantXtrasTasks.Tasks.Abstract
{
    [Serializable()]
    internal abstract class ExternalProgramScannerBase :ExternalProgramBase
    {
        #region Private Instance Fields

        #endregion Private Instance Fields

        #region Public Static Fields

        /// <summary>
        /// Defines the exit code that will be returned by <see cref="ExitCode" />
        /// if the process could not be started, or did not exit (in time).
        /// </summary>
        public const int UnknownExitCode = -1000;

        #endregion Public Static Fields

        #region Private Static Fields

        #endregion Private Static Fields

        #region Public Instance Properties
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
        protected virtual void PrepareProcess(Process process)
        }

        /// <summary>
        /// Starts the process and handles errors.
        /// </summary>
        /// <returns>The <see cref="Process" /> that was started.</returns>
        protected virtual Process StartProcess()
        {
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods


        #endregion Private Instance Methods
  
    }
}
