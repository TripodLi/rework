using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace 返修台应用程序
{
    public partial class AddStation : Form
    {
        Form1 F = null;
        public AddStation(Form1 f)
        {
            F = f;
            InitializeComponent();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("请仔细检查工位名称是否和主线体的一致，这将直接影响数据追溯！确定保存？", "友情提示",
                     System.Windows.Forms.MessageBoxButtons.YesNo,
                     System.Windows.Forms.MessageBoxIcon.Warning)
             == System.Windows.Forms.DialogResult.Yes)
            {
                string stationName = station.Text.Trim();
                string sql1 = "select * from R_STATION_T where Station_Name='" + stationName+"'";
                DataTable dt = new DataTable();
                dt = clsCommon.dbSql.ExecuteDataTable(sql1);
                if (dt.Rows.Count == 0)
                {
                    string sql = "insert dbo.R_STATION_T(Station_Name,CreateTime,DeleteFlag)values('" + stationName + "',GETDATE(),'0');";
                    if (clsCommon.dbSql.ExecuteNonQuery(sql) > 0)
                    {
                        MessageBox.Show("保存成功！");
                        F.DataReflsh();
                        this.Close();
                        this.Dispose();
                    }
                    else
                    {
                        MessageBox.Show("保存出错！");
                    }
                }
                else
                {
                    MessageBox.Show("该工位已存在！");
                }
            }
        }

        private void AddStation_Load(object sender, EventArgs e)
        {

        }
    }
}
