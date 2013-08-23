create or replace procedure insertdummy(anum in NUMBER, astring in nvarchar2) is
begin
  insert into dummy
    (id, dummyint, name)
  values
    (sdummy.nextval, anum, astring);
  commit;
end insertdummy;
/