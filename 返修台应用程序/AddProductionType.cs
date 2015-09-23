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
    public partial class AddProductionType : Form
    {
        Form1 F = null;
        public AddProductionType(Form1 f)
        {
            F = f;
            InitializeComponent();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("请仔细检查产品类型，这将直接影响数据追溯！确定保存？", "友情提示",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning)
            == System.Windows.Forms.DialogResult.Yes)
            {
                string Name = productionName.Text.Trim();
                string sql1 = "select * from R_PRODUCTION_T where Production_Name='" + Name+"'";
                DataTable dt = new DataTable();
                dt = clsCommon.dbSql.ExecuteDataTable(sql1);
                if (dt.Rows.Count == 0)
                {
                    string sql = "insert dbo.R_PRODUCTION_T(Production_Name,CreateTime,DeleteFlag)values('" + Name + "',GETDATE(),'0');";
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
                    MessageBox.Show("该产品类型已存在！");
                }
               
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            productionName.Text = "";
        }
    }
}
