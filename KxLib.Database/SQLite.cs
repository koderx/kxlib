using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace KxLib.Database {
    public class SQLite {
        private SQLiteConnection conn = null;
        SQLiteTransaction trans = null;
        private String dbpath;
        public SQLite(String DBPath) {
            dbpath = DBPath;
            Open();
        }
        ~SQLite() {
            Close();
        }
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <param name="DBPath">数据文件地址</param>
        private void Open() {
            Close();
            string dbPath = "Data Source =" + dbpath;
            // 创建数据库实例，指定文件位置  
            conn = new SQLiteConnection(dbPath);
            // 打开数据库，若文件不存在会自动创建  
            conn.Open();
        }
        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        private void Close() {
            if (conn != null) {
                try {
                    conn.Close();
                } catch (Exception) {

                }
                conn = null;
            }
        }
        public SQLiteDataReader Select(String sql, Object[] bindings) {
            return Select(makeBindings(sql, bindings));
        }
        public SQLiteDataReader Select(String sql) {
            if (conn == null) {
                throw new Exception("那个, 你没打开数据库吧?");
            }
            SQLiteCommand cmd;
            if (trans != null) {
                cmd = new SQLiteCommand(sql, conn, trans);
            } else {
                cmd = new SQLiteCommand(sql, conn);
            }
            try {
                return cmd.ExecuteReader();
            } catch (Exception ex) {
                throw ex;
            }
        }
        //Dictionary<int,String>
        public List<String> GetColumn(String tableName) {
            DataTable schemaTable = null;
            var sql = $"select * from [{tableName}]";
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            List<String> columns = new List<string>();
            using (IDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly)) {
                schemaTable = reader.GetSchemaTable();
                foreach (DataRow row in schemaTable.Rows) {
                    columns.Add(row["ColumnName"].ToString());
                }
                return columns;
            }
        }
        public int Delete(String sql) {
            return ExecuteNonQuery(sql);
        }
        public int Insert(String sql, Object[] bindings) {
            return Insert(makeBindings(sql, bindings));
        }
        public int Insert(String sql) {
            return ExecuteNonQuery(sql);
        }
        public int Update(String sql, Object[] bindings) {
            return Update(makeBindings(sql, bindings));
        }
        public int Update(String sql) {
            return ExecuteNonQuery(sql);
        }
        /// <summary>
        /// 运行一个通用语句
        /// </summary>
        /// <param name="sql">SQL语句</param>
        public void Statement(String sql) {
            if (conn == null) {
                throw new Exception("那个, 你没打开数据库吧?");
            }
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        /// <summary>
        /// 开始一个事务
        /// </summary>
        public void BeginTransaction() {
            if (trans != null) {
                trans.Rollback();
                trans = null;
            }
            trans = conn.BeginTransaction();
        }
        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit() {
            trans.Commit();
            trans = null;
        }
        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback() {
            trans.Rollback();
            trans = null;
        }
        /// <summary>
        /// 开始一个事务执行语句, 自动提交和回滚
        /// </summary>
        /// <param name="callback">语句回调</param>
        public void Transaction(Action callback) {
            BeginTransaction();
            try {
                callback();
                Commit();
            } catch (Exception ex) {
                Rollback();
                throw ex;
            }
        }

        public QueryBuilder Builder(String tableName) {
            return new QueryBuilder(this, tableName);
        }

        #region 私有函数
        private int ExecuteNonQuery(String sql) {
            if (conn == null) {
                throw new Exception("那个, 你没打开数据库吧?");
            }
            int result = 0;
            SQLiteCommand cmd;
            if (trans != null) {
                cmd = new SQLiteCommand(sql, conn, trans);
            } else {
                cmd = new SQLiteCommand(sql, conn);
            }
            //Logger.Log(sql);
            try {
                result = cmd.ExecuteNonQuery();
            } catch (Exception ex) {
                throw ex;
            }
            return result;
        }
        private String makeBindings(String sql, Object[] bindings) {
            for (int i = 0; i < bindings.Length; i++) {
                if (bindings[i].GetType() == typeof(String)) {
                    var v = bindings[i].ToString();
                    if (v.IndexOf("\"")>=0) {
                        v=v.Replace("\"", "\"\"");
                    }
                    sql = sql.Replace("{" + i + "}", "\"" + v + "\"");
                } else {
                    sql = sql.Replace("{" + i + "}", bindings[i].ToString());
                }

            }
            return sql;
        }
        #endregion
    }

    public class QueryBuilder {
        protected enum TYPE {
            SELECT,COUNT,MAX,MIN,SUM,AVG
        }
        protected SQLite db;
        protected String tableName;
        protected TYPE type = TYPE.SELECT;
        protected List<String> columns;
        // 主键名称
        protected String primaryKey = "id";
        // 主键类型
        protected String keyType = "int";
        protected String[] operators = {
        "=", "<", ">", "<=", ">=", "<>", "!=",
        "like", "not like", "between", "ilike",
        "&", "|", "<<", ">>"};
        protected TYPE[] cales = { TYPE.COUNT, TYPE.MAX, TYPE.MIN, TYPE.SUM, TYPE .AVG};
        protected List<Dictionary<String, String>> where;
        public QueryBuilder(SQLite db, String tableName) {
            this.db = db;
            this.tableName = tableName;
            where = new List<Dictionary<String, String>>();
            columns = new List<string>();
        }
        public QueryBuilder Where(String column, Object value) {
            return Where(column, "=", value);
        }
        public QueryBuilder OrWhere(String column, Object value) {
            return Where(column, "=", value,"or");
        }
        public QueryBuilder Where(String column, String oper, Object value, String boolean = "and") {
            if (!operators.Contains(oper.ToLower())) {
                throw new ArgumentException("不存在这个操作符");
            }
            var whereItem = new Dictionary<String, String>();
            whereItem.Add("column", column);
            whereItem.Add("operator", oper);
            if (value.GetType() == typeof(int)) {
                whereItem.Add("value", value.ToString());
            } else {
                whereItem.Add("value", "\"" + value.ToString() + "\"");
            }
            whereItem.Add("boolean", boolean);
            this.where.Add(whereItem);
            return this;
        }
        public Model Find(object id) {
            return Where(primaryKey, id).First();
        }
        protected String makeSQL() {
            var sb=new StringBuilder();
            if (this.type == TYPE.SELECT) {
                // 类型
                sb.Append(this.type);
                sb.Append(" ");
                // 列
                if (columns.Count == 0) {
                    sb.Append("* ");
                } else {
                    foreach (var item in columns) {
                        sb.Append(item);
                        sb.Append(",");
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append(" ");
                }
                // 表
                sb.Append("from ");
                sb.Append(tableName);
                // 条件
                makeWhere(sb);
                return sb.ToString();
            } else if (cales.Contains(type)) {
                sb.Append("select ");
                sb.Append(type);
                sb.Append("(");
                sb.Append(") as kxresult from ");
                sb.Append(tableName);
                // 条件
                makeWhere(sb);
            }
            throw new Exception("创建Model错误");
        }
        protected void makeWhere(StringBuilder sb) {
            if (where.Count > 0) {
                sb.Append(" where ");
                for (var i = 0; i < where.Count; i++) {
                    if (i > 0) {
                        sb.Append(where[i]["boolean"]);
                        sb.Append(" ");
                    }
                    sb.Append(where[i]["column"]);
                    sb.Append(" ");
                    sb.Append(where[i]["operator"]);
                    sb.Append(" ");
                    sb.Append(where[i]["value"]);
                    sb.Append(" ");
                }
                sb.Remove(sb.Length - 1, 1);
            }
        }
        public int Count() {
            this.type = TYPE.COUNT;
            return (int)Cale();
        }
        public Model First() {
            this.type = TYPE.SELECT;
            return ExecuteOne();
        }
        public List<Model> All() {
            this.type = TYPE.SELECT;
            return Execute();
        }
        protected Object Cale() {
            var reader = db.Select(makeSQL());
            if (reader.Read()) {
                return reader.GetValue(0);
            }
            throw new Exception("查询出现错误");
        }
        public List<Model> Execute() {
            var columns = db.GetColumn(tableName);
            String sql = makeSQL();
            var reader = db.Select(sql);
            List<Model> lstModel = new List<Model>();
            while (reader.Read()) {
                Model m = new Model(db, tableName, sql);
                foreach (var col in columns) {
                    m.Add(col, reader[col].ToString());
                }
                lstModel.Add(m);
            }
            return lstModel;
        }
        public Model ExecuteOne() {
            var columns = db.GetColumn(tableName);
            String sql = makeSQL();
            var reader = db.Select(sql);
            Model m = new Model(db, tableName, sql);
            if (reader.Read()) {
                foreach (var col in columns) {
                    m.Add(col, reader[col].ToString());
                }
            } else {
                return null;
            }
            return m;
        }
    }

    public class Model : Dictionary<String, String> {
        private SQLite db;
        private String tableName, sql;
        public List<String> Columns;
        public String SQL {
            get { return sql; }
        }
        public Model(SQLite db, String tableName, String sql) {
            this.db = db;
            this.tableName = tableName;
            this.sql = sql;
        }

        public new String this[String key] {
            get {
                return base[key];
            }
            set {
                base[key] = value;
            }
        }
        public void Save() {

        }
    }
}
