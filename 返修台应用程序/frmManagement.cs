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
    public partial class frmManagement : Form
    {
        public frmManagement()
        {
            InitializeComponent();
        }

        private void Management_Load(object sender, EventArgs e)
        {
            this.groupBox1.Enabled = false;
            this.toolStripSave.Enabled = false;
            dgv_User();
        }
        private void dgv_User()
        {
            string sql = "select h_ID,h_UserName,h_yUserPwd,h_Status,h_Department  from Users";
            this.dataGridView_user.AutoGenerateColumns = false;// 关闭自动创建列    
            this.dataGridView_user.DataSource = clsCommon.dbSql.ExecuteDataTable(sql);//绑定数据源
        }

        private void toolStripExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripAdd_Click(object sender, EventArgs e)
        {
            this.groupBox1.Enabled = true;
            this.toolStripSave.Enabled = true;
            this.toolStripEdit.Enabled = false;

        }
        #region 保存(包括新增用户、修改用户保存)
        /// <summary>
        /// 保存(包括新增用户、修改用户保存)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripSave_Click(object sender, EventArgs e)
        {  //TODO:添加
            if (this.h_UserName.Text.Trim() == string.Empty || this.h_UserPwd.Text.Trim() == string.Empty)
            {
                MessageBox.Show("用户名或密码不能为空!");
                this.h_UserName.Focus();
                return;
            }
            else if (this.h_UserPwd.Text.Length < 6)
            {
                MessageBox.Show("账户密码不能小于6位数字!");
                this.h_UserPwd.Focus();
                return;
            }

            if (this.toolStripEdit.Enabled == false)//添加帐户数据判断
            {
                string sql = "insert into Users(h_UserName,h_yUserPwd,h_Status) values('"
                    + h_UserName.Text + "','"
                    + h_UserPwd.Text + "','"
                    + Convert.ToInt32(h_Status.Checked) + "')";
                int res = clsCommon.dbSql.ExecuteNonQuery(sql);
                if (res > 0)
                {
                    MessageBox.Show("系统用户添加成功!");
                    dgv_User();
                }
            }
            else if (this.toolStripAdd.Enabled == false)//修改帐户数据判断
            {
                int rowIndex = int.Parse(this.dataGridView_user.SelectedRows[0].Cells[0].Value.ToString());
                string sql = "update Users set h_UserName='" + h_UserName.Text
                    + "',h_yUserPwd='" + h_UserPwd.Text
                    + "',h_Status='" + Convert.ToInt32(h_Status.Checked)
                    + "' where h_ID='" + rowIndex + "'";
                int res = clsCommon.dbSql.ExecuteNonQuery(sql);
                if (res > 0)
                {
                    MessageBox.Show("用户帐号修改成功！");
                    dgv_User();
                }
            }
            toolStripAdd.Enabled = true;
            toolStripDel.Enabled = true;
            toolStripEdit.Enabled = true;
            toolStripSave.Enabled = false;
            groupBox1.Enabled = false;
        }
        #endregion

        #region 删除用户操作
        /// <summary>
        /// 删除用户操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripDel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认删除此账户", "删除提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string rowIndex = this.dataGridView_user.SelectedRows[0].Cells[0].Value.ToString();
                string sql = "delete from Users where h_ID='" + rowIndex + "'";
                int res = clsCommon.dbSql.ExecuteNonQuery(sql);
                if (res > 0)
                {
                    MessageBox.Show("删除成功!");
                    dgv_User();
                }
            }
        }
        #endregion
        /// <summary>
        /// 修改账户信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripEdit_Click(object sender, EventArgs e)
        {
            try
            {
                this.toolStripAdd.Enabled = false;
                this.toolStripDel.Enabled = false;
                string rowIndex = this.dataGridView_user.SelectedRows[0].Cells[0].Value.ToString();//获取当前所选行索引值
                if (rowIndex != string.Empty)
                {
                    this.groupBox1.Enabled = true;
                    this.toolStripSave.Enabled = true;
                    string sql = "select h_UserName,h_yUserPwd,h_Status from Users where h_ID='" + rowIndex + "'";
                    DataTable dt = new DataTable();
                    dt = clsCommon.dbSql.ExecuteDataTable(sql);
                    this.h_UserName.Text = dt.Rows[0][0].ToString();
                    this.h_UserPwd.Text = dt.Rows[0][1].ToString();
                    int st = int.Parse(dt.Rows[0][2].ToString());
                    if (st > 0)
                    {
                        this.h_Status.Checked = true;
                    }
                }
            }
            catch (Exception EX)
            {
                throw EX;
            }
        }

        /// <summary>
        /// 选择数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView_user_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            this.h_UserName.Text = this.dataGridView_user[1, this.dataGridView_user.CurrentCell.RowIndex].Value.ToString();
            this.h_UserPwd.Text = this.dataGridView_user[2, this.dataGridView_user.CurrentCell.RowIndex].Value.ToString();
            if (int.Parse(this.dataGridView_user[3, this.dataGridView_user.CurrentCell.RowIndex].Value.ToString()) == 1)
            {
                this.h_Status.Checked = true;
            }
            if (int.Parse(this.dataGridView_user[3, this.dataGridView_user.CurrentCell.RowIndex].Value.ToString()) == 0)
            {
                this.h_Status.Checked = false;
            }
        }
    }
}
