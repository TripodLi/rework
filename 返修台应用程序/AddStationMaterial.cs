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
    public partial class AddStationMaterial : Form
    {
        public AddStationMaterial()
        {
            InitializeComponent();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }

        private void AddStationMaterial_Load(object sender, EventArgs e)
        {
            //初始化时加载所有工位
            string sql = "select Station_ID,Station_Name from dbo.R_STATION_T where DeleteFlag='0';";
            DataTable dt = new DataTable();
            dt = clsCommon.dbSql.ExecuteDataTable(sql);
            station.DataSource = dt;
            station.ValueMember = "Station_ID";
            station.DisplayMember = "Station_Name";
        }
        /// <summary>
        /// 保存工位物料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("请仔细检查该工位下的物料，这将直接影响数据追溯！确定保存？", "友情提示",
                      System.Windows.Forms.MessageBoxButtons.YesNo,
                      System.Windows.Forms.MessageBoxIcon.Warning)
              == System.Windows.Forms.DialogResult.Yes)
            {
                string materialName = material.Text.Trim();
                string sql1 = "select * from R_STATION_MATERIAL_T where Material_Name='" + materialName + "' and Station_ID=" + station.SelectedValue;
                DataTable dt = new DataTable();
                dt = clsCommon.dbSql.ExecuteDataTable(sql1);
                if (dt.Rows.Count == 0)
                {
                    string sql = "insert dbo.R_STATION_MATERIAL_T(Material_Name,Station_ID,CreateTime,DeleteFlag)values('" + materialName + "'," + station.SelectedValue + ",GETDATE(),'0');";
                    if (clsCommon.dbSql.ExecuteNonQuery(sql) > 0)
                    {
                        if (MessageBox.Show("保存成功！是否继续？", "友情提示",
                         System.Windows.Forms.MessageBoxButtons.YesNo,
                         System.Windows.Forms.MessageBoxIcon.Warning)
                          == System.Windows.Forms.DialogResult.Yes)
                        {
                            material.Text = "";

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
                    MessageBox.Show("对应工位物料已存在！");
                }
            }
           
        }
        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            material.Text = "";
            station.SelectedIndex = 0;
        }
    }
}
