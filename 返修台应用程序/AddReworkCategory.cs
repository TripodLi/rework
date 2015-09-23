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
    public partial class AddReworkCategory : Form
    {
        Form1 F = null;
        public AddReworkCategory(Form1 f)
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
            if (MessageBox.Show("请仔细检查返工类型，这将直接影响数据追溯！确定保存？", "友情提示",
                   System.Windows.Forms.MessageBoxButtons.YesNo,
                   System.Windows.Forms.MessageBoxIcon.Warning)
           == System.Windows.Forms.DialogResult.Yes)
            {
                string Name = reworkCategory.Text.Trim();
                string sql1 = "select * from R_CATEGORY_T where Category_Name='" + Name + "' and Production_ID=" + production.SelectedValue;
                DataTable dt = new DataTable();
                dt = clsCommon.dbSql.ExecuteDataTable(sql1);
                if (dt.Rows.Count == 0)
                {
                    string sql = "insert dbo.R_CATEGORY_T (Category_Name,Production_ID,CreateTime,DeleteFlag)values('" + Name + "'," + production.SelectedValue + ",GETDATE(),'0')";
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
                    MessageBox.Show("返修类型已存在！");
                }
            }
        }

        private void AddReworkCategory_Load(object sender, EventArgs e)
        {
            //初始化时加载所有产品类型
            string sql = "select Production_ID, Production_Name from dbo.R_PRODUCTION_T where DeleteFlag='0'";
            DataTable dt = new DataTable();
            dt = clsCommon.dbSql.ExecuteDataTable(sql);
            production.DataSource = dt;
            production.ValueMember = "Production_ID";
            production.DisplayMember = "Production_Name";
        }
    }
}
