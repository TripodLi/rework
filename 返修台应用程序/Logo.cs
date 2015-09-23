using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace 返修台应用程序
{
    public partial class Logo : Form
    {
        public Logo()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        private void Logo_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x02, 0);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Logo_Load(object sender, EventArgs e)
        {
            try
            {
                string sql = "select h_UserName from Users";
                using (DbDataReader usernames = clsCommon.dbSql.ExecuteReader(sql))
                {
                    while (usernames.Read())
                    {
                        cmbUsername.Items.Add(usernames[0]);
                    }
                }
            }
            catch
            {
                MessageBox.Show("连接数据库失败！");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool isEmpty = UserInputCheck();
            if (isEmpty == true)
            {
                string loginName = this.cmbUsername.Text.Trim();
                string userPwd = this.txtPassword.Text.Trim();
                DataTable dt = new DataTable();
                string sql = "select h_UserName,h_yUserPwd,h_Status,h_Permissions from Users where h_UserName='"
                    + loginName + "' and h_yUserPwd='" + userPwd + "'";
                dt = clsCommon.dbSql.ExecuteDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    if (int.Parse(dt.Rows[0]["h_Status"].ToString()) > 0)
                    {
                        clsCommon.userName = cmbUsername.Text;
                        clsCommon.userPermissions = dt.Rows[0]["h_Permissions"].ToString();
                        DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show("此账户已被锁定暂无法登录！", "登陆提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.cmbUsername.Text = "";
                        this.txtPassword.Text = "";
                        this.cmbUsername.Focus();
                    }
                }
                else
                {
                    MessageBox.Show("登陆失败：用户名或密码错误,请重新输入!", "登陆提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.cmbUsername.Text = "";
                    this.txtPassword.Text = "";
                    this.cmbUsername.Focus();
                }
            }
        }

        private bool UserInputCheck()
        {
            // 保存登录名称
            string loginName = this.cmbUsername.Text.Trim();
            // 保存用户密码
            string userPwd = this.txtPassword.Text.Trim();

            // 开始验证
            if (string.IsNullOrEmpty(loginName))
            {

                this.toolTip.ToolTipIcon = ToolTipIcon.Info;
                this.toolTip.ToolTipTitle = "登录提示";
                Point showLocation = new Point(
                    this.cmbUsername.Location.X + this.cmbUsername.Width,
                    this.cmbUsername.Location.Y);
                this.toolTip.Show("请您输入登录账户名！", this, showLocation, 2000);
                this.cmbUsername.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(userPwd))
            {

                this.toolTip.ToolTipIcon = ToolTipIcon.Info;
                this.toolTip.ToolTipTitle = "登录提示";
                Point showLocation = new Point(
                    this.txtPassword.Location.X + this.txtPassword.Width,
                    this.txtPassword.Location.Y);
                this.toolTip.Show("请您输入登陆账户密码！", this, showLocation, 2000);
                this.txtPassword.Focus();
                return false;
            }
            else if (userPwd.Length < 6)
            {
                this.toolTip.ToolTipIcon = ToolTipIcon.Warning;
                this.toolTip.ToolTipTitle = "登录警告";
                Point showLocation = new Point(
                    this.txtPassword.Location.X + this.txtPassword.Width,
                    this.txtPassword.Location.Y);
                this.toolTip.Show("用户密码长度不能小于六位！", this, showLocation, 2000);
                this.txtPassword.Focus();
                return false;
            }

            // 如果已通过以上所有验证则返回真
            return true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
