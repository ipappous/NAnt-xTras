
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;
using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks
{

    [TestFixture]
    public class SqlplusTaskTest : BuildTestBase
    {
        private const string _validDbConnection = "EFSCR_NINJA/EFSCR_NINJA@EISP_PROD"; //scott/tiger@localhost

        private const string _invalidDBConnection = "Demo/Dammy@dodo";

        private const string _format = @"<?xml version='1.0' ?>
            <project>
           		<sqlplus  dbconnection= '{0}' {1} resultproperty='sqlPlusResult'  debug='{2}' failonerror='{3}' >
			        {4}
		</sqlplus>
            </project>";


        private const string _formatcustomSql = @"<?xml version='1.0' ?>
            <project>
<sqlplus  dbconnection= ""EFSCR_NINJA/EFSCR_NINJA@EISP_PROD"" failonerror=""false"" debug=""false"">
<includeerrorpattern pattern=""ora-""/>
    <sqlscript>
    <![CDATA[
    update mparam set value = &1 
    where key = 'MAINTENANCE_MODE';
    commit;
    /
    ]]>
    </sqlscript>
<arg value=""0"" />
    </sqlplus>	
</project>";

        /// <summary>Test <arg> option.</summary>
        [Test]
        public void Test_Debug()
        {
            string result = "";
            CopyDataToTemp();
            result = RunBuild(FormatBuildFile(_validDbConnection, "workingdir='TestData'", "true", "false", ""));
        }



        [Test]
        public void Test_Command_File_Creation_For_WorkingDir()
        {
            string result = "";
            CopyDataToTemp();
            result = RunBuild(FormatBuildFile(_validDbConnection, "workingdir='TestData'", "true", "false", ""));
            Assert.IsTrue(result.IndexOf("01. CreateTable.sql") != -1,
                          "Could not Create command file for batch script execution.");

            Assert.IsTrue(
                result.IndexOf("01. CreateTable.sql") > result.IndexOf("01. SubFolderData\01. Create Dummy Table.sql"),
                "Scripts in subfolders should be executed first.");
        }


        [Test]
        public void Test_InLineSQL_File_Creation()
        {
            string result = "";
       
            result = RunBuild(FormatBuildFile(_validDbConnection, "", "true", "false", "<sqlscript> <![CDATA[select 1 from dual; /]]></sqlscript>"));
            Assert.IsTrue(result.IndexOf("tmp.sql") != -1,
                          "Could not Create temp sql file for inline script.");
        }


        [Test]
        public void Test_Directory_Script_Execution_NoData()
        {
            string result = "";
            try
            {
                result = RunBuild(FormatBuildFile(_validDbConnection, "workingdir='TestData'", "false", "true", ""));
                Assert.Fail("Project should have failed:" + result);
            }
            catch (TestBuildException be)
            {
                Assert.IsTrue(be.InnerException.ToString().IndexOf("There is nothing to execute!!") != -1,
                    "There should be no data to execute");
            }
        }

        [Test]
        public void Test_Directory_Script_Execution()
        {
            string result = "";
            CopyDataToTemp();
            result = RunBuild(FormatBuildFile(_validDbConnection, "workingdir='TestData'", "false", "true", ""));
            Assert.IsTrue(result.IndexOf("01. CreateTable.sql") != -1,
                          "scripts should be ran.");
        }

        private string FormatBuildFile(string dbconnection, string workDir, string debug, string failonerror,
                                       string nestedElements)
        {
            return String.Format(CultureInfo.InvariantCulture, _format, dbconnection, workDir, debug, failonerror,
                                 nestedElements);
        }


        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        private void CopyDataToTemp()
        {
            var target = new DirectoryInfo(TempDirName);
            var root = target.CreateSubdirectory("TestData");
            CopyFilesRecursively(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\TestData"), root);
        }



    }
}
