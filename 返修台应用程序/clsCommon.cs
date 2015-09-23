using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql;
using System.Xml;

namespace 返修台应用程序
{
    class clsCommon
    {
        public static string connectionString
        {
            get
            {
                XmlDocument xml = new XmlDocument();
                xml.Load("system.config");
                XmlNode xe = xml.DocumentElement;
                XmlNode node = xe.SelectSingleNode("dbString");
                return node.InnerText;
            }
        }

        public static DbUtility dbSql = new DbUtility(connectionString, DbProviderType.SqlServer);

        public static string userName;

        public static string userPermissions;

    }
}
