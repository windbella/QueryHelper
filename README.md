### DataTable To Query

DataTable, DataRow를 이용해 SQL Query문을 생성합니다.

DataTable 내용 전체에 해당하는 INSERT, DELETE문을 생성하거나

2개의 DataTable을 비교하여 다른 부분만 INSERT, DELETE, UPDATE 해주는 쿼리를 생성하는 부분을 포함합니다. 

단, DataTable 객체가 PrimaryKey를 가지고 있어야 합니다.

(PrimaryKey가 있어야 DELETE문의 조건절 생성이나 UDPATE 기능의 ROW 비교를 올바르게 할 수 있습니다.)

```
// 테이블 INSERT ( 매개 변수의 데이터를 삽입 )
public static string BuildInsert(DataTable dataTable)

// 테이블 로우 INSERT ( 매개 변수의 데이터를 삭제 )
public static string BuildInsert(DataRow dataRow)

// 테이블 DELETE ( 조건 사용 )
public static string BuildDelete(DataTable dataTable, string whereClause)

// 테이블 DELETE ( 매개 변수의 데이터 삭제 )
public static string BuildDelete(DataTable dataTable)

// 테이블 로우 DELETE ( 매개 변수의 데이터 삭제 )
public static string BuildDelete(DataRow dataRow)

// 테이블 업데이트 ( 매개 변수의 두 테이블을 비교하여 변경된 내용 삽입/삭제/수정 )
// exceptedColumn에 등록된 컬럼은 비교대상에서 제외됩니다.
public static string BuildUpdate(DataTable sourceDataTable, DataTable targetDataTable, IEnumerable<string> exceptedColumn = null)

// 테이블 업데이트 ( 매개 변수의 데이터로 수정 )
public static string BuildUpdate(DataRow dataRow, DataRow compareRow)

// 테이블 업데이트 ( 매개 변수의 데이터로 수정 )
public static string BuildUpdate(DataRow dataRow)

// 프로시저 호출
public static string BuildExcuteProcedure(string procedureName, IEnumerable<object> parameters, string dbType)
```
