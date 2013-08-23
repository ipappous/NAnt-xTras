
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;
using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {

    [TestFixture]
    public class SqlplusTaskTest : BuildTestBase
    {
        private const string _validDbConnection = "EFSCR_NINJA/EFSCR_NINJA@EISP_PROD"; //scott/tiger@localhost

        private const string _invalidDBConnection = "Demo/Dammy@dodo";

        const string _format = @"<?xml version='1.0' ?>
            <project>
           		<sqlplus  dbconnection= ""EFSCR_NINJA/EFSCR_NINJA@EISP_PROD"" workingdir=""C:\svn\efs\cr\600-Development\5\trunk\900-Pack\03 Version Extras\5.00.147\09. Phone\DB Scripts"" resultproperty=""sqlPlusResult""  failonerror=""false"" debug=""true"">
			<includeerrorpattern pattern=""ora-""/>
		</sqlplus>
            </project>";

        const string _formatcustomSql = @"<?xml version='1.0' ?>
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
            result = RunBuild(_format);
        }


        [Test]
        public void Test_InLine()
        {
            string result = "";
            result = RunBuild(_formatcustomSql);
        }

        /// <summary>Test <arg> option.</summary>
        [Test]
        public void Test_ArgOption() {
            string result = "";
            if (PlatformHelper.IsWin32) {
                result = RunBuild(FormatBuildFile("program='cmd.exe'", "<arg value='/c echo Hello, World!'/>"));
            } else {
                result = RunBuild(FormatBuildFile("program='echo'", "<arg value='Hello, World!'/>"));
            }
            Assert.IsTrue(result.IndexOf("Hello, World!") != -1, "Could not find expected text from external program, <arg> element is not working correctly.");
        }

        /// <summary>Regression test for bug #461732 - ExternalProgramBase.ExecuteTask() hanging</summary>
        /// <remarks>
        /// http://sourceforge.net/tracker/index.php?func=detail&aid=461732&group_id=31650&atid=402868
        /// </remarks>
        [Test]
        public void Test_ReadLargeAmountFromStdout() {

            // create a text file with A LOT of data
            string line = "01234567890123456789012345678901234567890123456789012345678901234567890123456789" + Environment.NewLine;
            StringBuilder contents = new StringBuilder("You can delete this file" + Environment.NewLine);
            for (int i = 0; i < 250; i++) {
                contents.Append(line);
            }
            string tempFileName = Path.Combine(TempDirName, "bigfile.txt");
            TempFile.Create(tempFileName);

            if (PlatformHelper.IsWin32) {
                RunBuild(FormatBuildFile("program='cmd.exe' commandline='/c type &quot;" + tempFileName + "&quot;'", ""));
            } else {
                RunBuild(FormatBuildFile("program='cat' commandline=' &quot;" + tempFileName + "&quot;'", ""));
            }
            // if we get here then we passed, ie, no hang = bug fixed
        }

        private string FormatBuildFile(string attributes, string nestedElements) {
            return String.Format(CultureInfo.InvariantCulture, _format, attributes, nestedElements);
        }
    }
}
