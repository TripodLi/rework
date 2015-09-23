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
    public partial class AddStationScrow : Form
    {
        public AddStationScrow()
        {
            InitializeComponent();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("请仔细检查该工位下的螺栓，这将直接影响数据追溯！确定保存？", "友情提示",
                     System.Windows.Forms.MessageBoxButtons.YesNo,
                     System.Windows.Forms.MessageBoxIcon.Warning)
             == System.Windows.Forms.DialogResult.Yes)
            {
                string scrowNumber = scrowNum.Text.Trim();
                string scrowName1 = scrowName.Text.Trim();
                string sql1 = "select * from R_STATION_SCREW_T where Screw_Name= '" + scrowName1 + "' and Station_ID=" + station.SelectedValue;
                DataTable dt = new DataTable();
                dt = clsCommon.dbSql.ExecuteDataTable(sql1);
                if (dt.Rows.Count == 0)
                {
                    string sql = "insert dbo.R_STATION_SCREW_T(Screw_Num,Screw_Name,Station_ID,CreateTime,DeleteFlag)values('" + Convert.ToInt32(scrowNumber) + "','" + scrowName1 + "'," + station.SelectedValue + ",GETDATE(),'0');";
                    if (clsCommon.dbSql.ExecuteNonQuery(sql) > 0)
                    {
                        if (MessageBox.Show("保存成功！是否继续？", "友情提示",
                         System.Windows.Forms.MessageBoxButtons.YesNo,
                         System.Windows.Forms.MessageBoxIcon.Warning)
                          == System.Windows.Forms.DialogResult.Yes)
                        {
                            scrowNum.Text = "";

                        }
                        else
                        {
                            this.Close();
                            this.Dispose();
                        }
                    }
                    else
                    {
                        MessageBox.Show("保存出错！");
                        this.Close();
                        this.Dispose();
                    }
                }
                else
                {
                    MessageBox.Show("该工位对应的螺栓已存在！");
                }
              
            }
        }

        private void AddStationScrow_Load(object sender, EventArgs e)
        {
            //初始化时加载所有工位
            string sql = "select Station_ID,Station_Name from dbo.R_STATION_T where DeleteFlag='0';";
            DataTable dt = new DataTable();
            dt = clsCommon.dbSql.ExecuteDataTable(sql);
            station.DataSource = dt;
            station.ValueMember = "Station_ID";
            station.DisplayMember = "Station_Name";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            scrowNum.Text = "";
            scrowName.Text = "";
        }
        //限制螺栓号只能输入数字
        private bool nonNumberEntered = false;
        private void scrowNum_KeyDown(object sender, KeyEventArgs e)
        {
            nonNumberEntered = false;
            if ((e.KeyCode < Keys.D0) || (e.KeyCode > Keys.D9 && e.KeyCode < Keys.NumPad0) || (e.KeyCode > Keys.NumPad9))
            {
                if (e.KeyCode != Keys.Back)
                {
                    nonNumberEntered = true;
                }
            }
        }

        private void scrowNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (nonNumberEntered)
            {
                e.Handled = true;
            }
        }

        private void station_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
