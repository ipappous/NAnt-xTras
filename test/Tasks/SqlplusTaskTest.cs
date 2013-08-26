
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;
using NAnt.Core;
using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

namespace NAntxTras.Tests.Tasks.Oracle
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
        public void Test_InLineSQL_File_Creation()
        {
            string result = "";

            result = RunBuild(FormatBuildFile(_validDbConnection, "", "true", "false", "<sqlscript> <![CDATA[select 1 from dual; ]]></sqlscript>"));
            Assert.IsTrue(result.IndexOf("tmp.sql") != -1,
                          "Could not Create temp sql file for inline script.");
        }

        [Test]
        public void Test_InLineSQL_execution()
        {
            string sql =
@"declare 
  v number(38);
begin 
  select 1 into v from dual; 
  dbms_output.put_line('Value: ' || v);
end;
/";
            string result = "";

            result = RunBuild(FormatBuildFile(_validDbConnection, "", "false", "false", "<sqlscript> <![CDATA[" + sql + "]]></sqlscript>"));
            Assert.IsTrue(result.IndexOf("Value: 1") != -1,
                          "Invalid output value! Inline script not executed");
        }

        

            [Test]
        public void Test_InLineSQL_execution_invalidDBConnection()
        {
            string sql =
@"declare 
  v number(38);
begin 
  select 1 into v from dual; 
  dbms_output.put_line('Value: ' || v);
end;
/";
            string result = "";

            result = RunBuild(FormatBuildFile(_invalidDBConnection, "", "false", "false", "<sqlscript> <![CDATA[" + sql + "]]></sqlscript>"));
            Assert.IsTrue(result.IndexOf("ORA-12154") != -1,
                          "ORA-12154: TNS should be invalid");
        }

             [Test]
                public void Test_InLineSQL_execution_()
        {
            string nested =
@"<includeerrorpattern pattern='sp2-'/>
<sqlscript> <![CDATA[
    DATAFILE
  'D:\APP\ARJU\ORADATA\A\SYSTEM01.DBF',
  'D:\APP\ARJU\ORADATA\A\SYSAUX01.DBF',
  'D:\APP\ARJU\ORADATA\A\UNDOTBS01.DBF',
  'D:\APP\ARJU\ORADATA\A\USERS01.DBF',
  'D:\APP\ARJU\PRODUCT\11.1.0\DB_1\DATABASE\DATA01.DBF',
  'F:\MIGRATE_TO_ASM.DBF'
CHARACTER SET WE8MSWIN1252;
/
]]></sqlscript>
<arg value='69' />";

            string result = "";

            result = RunBuild(FormatBuildFile(_validDbConnection, "", "false", "false", nested));
            Assert.IsTrue(result.IndexOf("Critical errors found during the execution of") != -1,
                          "Critical errors should be scanned from the output.");
        }



        [Test]
        public void Test_InLineSQL_execution_withArg()
        {
            string nested =
@"<sqlscript> <![CDATA[
declare 
  v number(38);
begin 
  select &1 into v from dual; 
  dbms_output.put_line('Value: ' || v);
end;
/
]]></sqlscript>
<arg value='69' />";
            string result = "";

            result = RunBuild(FormatBuildFile(_validDbConnection, "", "false", "false", nested));
            Assert.IsTrue(result.IndexOf("Value: 69") != -1,
                          "Invalid output value! Inline script with argument not executed");
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

        [Test]
        public void Test_Directory_Script_Execution_SQLErrorChecks()
        {
            string result = "";
            string nested =
@"<includeerrorpattern pattern='ora-'/>";
            CopyDataToTemp();
            result = RunBuild(FormatBuildFile(_validDbConnection, "workingdir='TestData'", "false", "false", nested));
            Assert.IsTrue(result.IndexOf("Critical errors found during the execution") != -1,
                          "scripts should fail");
            Assert.IsTrue(result.IndexOf("ORA-00955") != -1,
                          "There should have name is already used by an existing object error");
        }


        [Test]
        public void Test_Directory_Script_Execution_SQLErrorChecks_withexclude()
        {
            string result = "";
            string nested =
@"<includeerrorpattern pattern='ora-'/>
  <excludeerrorpattern pattern='ora-00955'/>";
            CopyDataToTemp();
            result = RunBuild(FormatBuildFile(_validDbConnection, "workingdir='TestData'", "false", "false", nested));
            Assert.IsFalse(result.IndexOf("Critical errors found during the execution") != -1,
                          "scripts should run");
            Assert.IsTrue(result.IndexOf("ORA-00955") != -1,
                          "There should have name is already used by an existing object error but no critical errror");
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
