using System.IO;
using NAnt.Core.Attributes;
using NantXtras.Tasks.Abstract;

namespace NantXtras.Tasks.Oracle
{
    [TaskName("expdp")]
    public class ExpDpTask : ExternalProgramScannerBase
    {
        public ExpDpTask()
        {
            base.ExeName = "expdp";

        }



        #region Private Instance Fields

        private string _dbconnection;



        #endregion Private Instance Fields

        #region Private Static Fields

        #endregion Private Static Fields

        #region Public Instance Properties



        [TaskAttribute("dbconnection")]
        [StringValidator(AllowEmpty = false)]
        public string DBConnection
        {
            get { return _dbconnection; }
            set { _dbconnection = value; }
        }


        #endregion Public Instance Properties
        
        #region Private Instance Methods

        #endregion Private Instance Methods

        #region Override implementation of Task
        

        protected override void Initialize()
        {
            base.Initialize();
            
        }

        public override string ProgramArguments
        {
            get { return string.Format(" '{0}' ", DBConnection); }
        }

        /// <summary>
        /// Executes the external program.
        /// </summary>
        protected override void ExecuteTask()
        {


                base.ExecuteTask();
 

        }

        protected override void PrepareProcess(System.Diagnostics.Process process)
        {
            base.PrepareProcess(process);

   }

        #endregion Override implementation of Task
    }
}
