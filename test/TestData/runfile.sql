set linesize 1000
set verify off
set feedback 0
set serveroutput on size 1000000

begin
   dbms_output.put_line(to_char(sysdate, 'yyyy/mm/dd hh24:mi:ss')||': Start of &1 ...');
end;
/

set verify on
set feedback 1

@"&1"

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
set feedback 1