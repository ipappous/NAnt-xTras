using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NAntxTras.Tests.Utils;
using NUnit.Framework;
using Tests.NAnt.Core;

namespace NAntxTras.Tests.Tasks
{
    internal class ImpExpDpTasksTest
    {

        [TestFixture]
        public class CallURITaskTest : BuildTestBase
        {
            private const string _dummyDBConnection = "DUMMY/DUMMY@EFSTESTBEDDB";

            private const string _sysDBConnection = "sys/sysdba@EFSTESTBEDDB as sysdba";

            private const string _formatSqlPlus = @"<?xml version='1.0' ?>
            <project>
           		<sqlplus  dbconnection= '{0}' {1} resultproperty='sqlPlusResult'  debug='{2}' failonerror='{3}' >
			        {4}
		</sqlplus>
            </project>";

            private const string _formatImpExp = @"<?xml version='1.0' ?>
            <project>
           		<{0}  dbconnection= '{1}' resultproperty='impexpResult' failonerror='true' >
			        {2}
		        </{0}>
            </project>";



            [Test]
            public void Test_Debug()
            {

            }

            [Test]
            public void Test_Full_Export_Import_Test()
            {
                string nested =
    @"
<includeerrorpattern pattern='ora-'/>
<includeerrorpattern pattern='sp2-'/>
<sqlscript> <![CDATA[
set verify off
set timing on
set feedback 2
set serveroutput on size 1000000

create or replace procedure kill_all_other_user_sessions(in_user in VARCHAR2) is

  user_not_found EXCEPTION;
   PRAGMA EXCEPTION_INIT(user_not_found, -1918);
	
   cursor c is
      select 'ALTER SYSTEM KILL SESSION ''' || substr(b.sid, 1, 5) || ', ' || substr(b.serial#, 1, 5) ||
                ''' -- from session ' || (Select sid from v$mystat where rownum = 1) cmd
         from sys.v_$session b, sys.v_$process a
       where b.paddr = a.addr
          and type = 'USER'
          and b.USERNAME = in_user
       order by spid;
   l_flag boolean := false;
   function dt return varchar2 is
   begin
     return to_char(sysdate, 'yyyy/mm/dd hh24:mi:ss')||'=> ';
   end dt;
begin
   dbms_output.enable(1000000);
   IF (in_user IS NULL) THEN
      dbms_output.put_line(dt||'User must be declared!');
      RETURN;
   END IF;
   while (not l_flag) loop
      for r in c loop
         begin
            dbms_output.put_line(dt||r.cmd);
            execute immediate r.cmd;
         exception
            when others then
               dbms_output.put_line(dt||r.cmd || ' Not Killed');
         end;
      end loop;
      begin
         dbms_output.put_line(dt||'Dropping User ' || in_user);
         execute immediate 'drop user ' || in_user || ' cascade';
         l_flag := true;
      exception
		 when user_not_found then
			l_flag := true;
         when others then
            dbms_output.put_line(dt||'User ' || in_user || ' not killed');
      end;
   end loop;
end kill_all_other_user_sessions;
/

EXEC  kill_all_other_user_sessions('&1');

create user &1
  identified by ""&2""
  profile DEFAULT
	ACCOUNT UNLOCK;

grant select on DBA_INDEXES   to &1;
grant select on DBA_OBJECTS   to &1;
grant select on DBA_SEQUENCES to &1;
grant select on DBA_SOURCE    to &1;
grant select on DBA_TABLES    to &1;
grant select on DBA_DIRECTORIES to &1;
grant execute on DBMS_LOCK    to &1;


grant execute on UTL_FILE          to &1;
grant execute on UTL_RECOMP        to &1;
grant select on V_$MYSTAT          to &1;
grant select on V_$PROCESS         to &1;
grant select on V_$SESSION         to &1;
grant select on v_$instance 	   to &1;
GRANT ""CONNECT"" TO ""&1"" WITH ADMIN OPTION;

GRANT ""RESOURCE"" TO ""&1"" WITH ADMIN OPTION;
GRANT DBA TO &1 WITH ADMIN OPTION;
grant alter system                 to &1;
grant create any view             to &1;
grant create any index             to &1;
grant create any sequence          to &1;
grant create any table             to &1;
grant create session               to &1;
grant create table                 to &1;
grant drop any index               to &1;
grant drop any sequence            to &1;
grant drop any table               to &1;
grant drop public database link    to &1;
grant create any  procedure        to &1;
grant drop any  procedure          to &1;
grant alter any  procedure          to &1;
grant execute any  procedure        to &1;
grant select any table             to &1;
grant unlimited tablespace         to &1;
grant query rewrite to ""&1"";
grant create any directory to &1;
grant create any type to &1;

grant select on v_$parameter to &1;
grant java_admin to &1;
grant java_deploy to &1;
grant create job to &1;

-- mkrit 20081114 Needed for WF_Utils.Log_Wf_Execution
BEGIN DBMS_JAVA.grant_permission('&1', 'java.io.FilePermission', '<<ALL FILES>>', 'read ,write, execute, delete'); END;
/
BEGIN Dbms_Java.Grant_Permission('&1', 'SYS:java.lang.RuntimePermission', 'writeFileDescriptor', ''); END;
/
BEGIN Dbms_Java.Grant_Permission('&1', 'SYS:java.lang.RuntimePermission', 'readFileDescriptor', ''); END;
/

begin
dbms_output.put_line('User successful Creation!!!');
end;
/

]]></sqlscript>
<arg value='DUMMY' />
<arg value='DUMMY' />
";
                string result = "";

                //create dummy schema
                result = RunBuild(FormatSqlPlusBuildFile(_sysDBConnection, "", "false", "true", nested));
                Assert.IsTrue(result.IndexOf("User successful Creation!!!") != -1,
                              "The Dummy user should be created without a problem");

                //add data
                TestUtils.CopyDataToTemp(TempDirName);
                result = RunBuild(FormatSqlPlusBuildFile(_dummyDBConnection, "workingdir='TestData'", "false", "true", ""));
                Assert.IsTrue(result.IndexOf("01. CreateTable.sql") != -1,
                              "scripts should be ran.");

                //test data creation
                nested =
@"
<includeerrorpattern pattern='ora-'/>
<includeerrorpattern pattern='sp2-'/>
<sqlscript> <![CDATA[
    select * from DUMMY.DUMMY;

]]></sqlscript>
";
                result = RunBuild(FormatSqlPlusBuildFile(_sysDBConnection, "", "false", "true", nested));
                Assert.IsTrue(result.IndexOf("Hello World!") != -1,
                              "Schema dummy should have data.");


                //create dump directory
                nested =
    @"
<includeerrorpattern pattern='ora-'/>
<includeerrorpattern pattern='sp2-'/>
<sqlscript> <![CDATA[
    create or replace directory &1 as '&2';

begin
dbms_output.put_line('Dump Dir OK!!!');
end;
/
]]></sqlscript>
<arg value='DUMMY_DMP_DIR' />
<arg value='c:\\DUMMYDROP' />
";
                result = RunBuild(FormatSqlPlusBuildFile(_sysDBConnection, "", "false", "true", nested));
                Assert.IsTrue(result.IndexOf("Dump Dir OK!!!") != -1,
                                     "Dump directory should be created without a problem");

//remove file if exists
nested =
@"
<sqlscript> <![CDATA[
 exec utl_file.fremove('DUMMY_DMP_DIR','DUMMY.dmp');

]]></sqlscript>
";
                result = RunBuild(FormatSqlPlusBuildFile(_sysDBConnection, "", "false", "false", nested));
                

                //export dump
                //expdp %sys_string% schemas=DUMMY directory=DUMMY_DMP_DIR dumpfile=DUMMY.dmp  parallel=1 
                nested =
@"
<includeerrorpattern pattern='ora-'/>
<includeerrorpattern pattern='sp2-'/>
<arg value='schemas=DUMMY' />
<arg value='directory=DUMMY_DMP_DIR' />
<arg value='dumpfile=DUMMY.dmp' />
<arg value='NOLOGFILE=YES' />
<arg value='parallel=1' />
";
                result = RunBuild(FormatImpExpBuildFile("expdp", _sysDBConnection, nested));
                   Assert.IsTrue(result.IndexOf(@"C:\DUMMYDROP\DUMMY.DMP") != -1,
                                     "Exported dummy.dmp");

                //import New user
                //impdp %sys_string%  directory=DUMMY_DMP_DIR remap_schema=DUMMY:DUMMYII exclude=user TRANSFORM=oid:n dumpfile=DUMMY.dmp PARALLEL=2 TABLE_EXISTS_ACTION=SKIP
                nested =
@"
<includeerrorpattern pattern='ora-'/>
<includeerrorpattern pattern='sp2-'/>
<arg value='directory=DUMMY_DMP_DIR' />
<arg value='remap_schema=DUMMY:DUMMYII' />
<arg value='dumpfile=DUMMY.dmp' />
<arg value='NOLOGFILE=YES' />
<arg value='parallel=2' />
<arg value='TABLE_EXISTS_ACTION=SKIP' />
";
                result = RunBuild(FormatImpExpBuildFile("impdp", _sysDBConnection, nested));

                //test data creation
                nested =
@"
<includeerrorpattern pattern='ora-'/>
<includeerrorpattern pattern='sp2-'/>
<sqlscript> <![CDATA[
  select * from DUMMYII.DUMMY;

]]></sqlscript>
";
                result = RunBuild(FormatSqlPlusBuildFile(_sysDBConnection, "", "false", "true", nested));
                Assert.IsTrue(result.IndexOf("Hello World!") != -1,
                              "Schema dummy II should have data!");


                //drop schemas

                nested =
@"
<includeerrorpattern pattern='ora-'/>
<includeerrorpattern pattern='sp2-'/>
<sqlscript> <![CDATA[
    drop user &1 cascade;
    drop user &2 cascade;

    begin
        dbms_output.put_line('Users successfuly Dropped!!!');
    end;
    /

]]></sqlscript>
<arg value='DUMMY' />
<arg value='DUMMYII' />
";
                result = RunBuild(FormatSqlPlusBuildFile(_sysDBConnection, "", "false", "true", nested));
                Assert.IsTrue(result.IndexOf("Users successfuly Dropped!!!") != -1,
                              "Users should be dropped without a problem");
            }



            private string FormatSqlPlusBuildFile(string dbconnection, string workDir, string debug, string failonerror,
                                       string nestedElements)
            {
                return String.Format(CultureInfo.InvariantCulture, _formatSqlPlus, dbconnection, workDir, debug, failonerror,
                                     nestedElements);
            }

            private string FormatImpExpBuildFile(string cmd, string dbconnection, string nestedElements)
            {
                return String.Format(CultureInfo.InvariantCulture, _formatImpExp, cmd, dbconnection, nestedElements);
            }

        }
    }
}
