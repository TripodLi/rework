using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyOPC;
using MyLog;
using System.Drawing.Drawing2D;
using System.Xml;

namespace 返修台应用程序
{
    public partial class Form1 : Form
    {
        #region 定义全局变量
        public static OPCCreate OPC = null;
        public int stepId = 0; //步序ID
        public int changeRow = -1;//当前选择行
        public int databindData = 0;
        DataTable tb2 = new DataTable();
        public int production_No; //全局变量 为记录返修开始的时候的产品类型
        public int rework_No; //全局变量 为记录返修开始的时候的返修类型
        int stepNo_Trace; //步序
        string stationName_Trace;//工位名称
        int stepCategory_Trace; //当前步类型
        int sleeveNo_Trace; //套筒号
        int shelfNo_Trace; //料架号
        int number_Trace; //数量
        int gunNo_Trace; //枪号
        int programNo_Trace; //程序号
        string Feacode_Trace; //特征码
        int reworkTimes_Trace; //返工次数
        int materialNo_Trace; //工位对应物料ID
        string materialName_Trace; //工位对应物料名称
        int scrowNo_Trace; //工位对应螺栓ID
        string scrowName_Trace; //工位对应螺栓名称
        int reworkNo_Trace; //返工类型ID
        string reworkName_Trace; //返工名称
        int productionNo_Trace;//产品ID
        string productionName_Trace;//产品名称
        int stationNo_Trace;//工位ID
        int stepconfigID_Trace; //当前步的ID
        string module_Trace;//当前模组号
        string workID;//员工号
        string packSN;
        Boolean myselfBeat;//心跳
        #endregion
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(pictureBox1.ClientRectangle);
            Region re = new System.Drawing.Region(gp);
            pictureBox1.Region = re;
            gp.Dispose();
            re.Dispose();
            cmbType.SelectedIndex = 0;  //选中返修类型的第一项
            // cmbStation.SelectedIndex = 0;  
            label3.Text = clsCommon.userName + "  您好！";//显示用户名
            workID = clsCommon.userName;
            if (clsCommon.userPermissions == "超级用户")//设置用户权限
            {
                员工管理ToolStripMenuItem.Visible = true;
            }
            else
            {
                员工管理ToolStripMenuItem.Visible = false;
            }
            DataReflsh();
        }
        #region 员工相关的基础配置页面
        private void 关闭ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            label5.Text = cmbType.SelectedItem.ToString().Trim();  //显示列表的名称随着类型的选择而变化
        }

        private void 登陆ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logo l = new Logo();
            l.ShowDialog();
        }

        private void 增加员工ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddEmp ae = new AddEmp();
            ae.ShowDialog();
        }
        #endregion
        private void OnEventDataChanged(object objItem, object objValue)
        {
            try
            {
                int itemCode = Convert.ToInt32(objItem);
                string itemValue = objValue.ToString();
                //((MethodInvoker)delegate() { DoOPCEvent(itemCode, itemValue); }).BeginInvoke(null, null);
                DoOPCEvent(itemCode, itemValue);
            }
            catch (Exception ex)
            {
                recordMessage("处理事件错误，项为:" + objItem + "，值为:" + objValue + "，错误：" + ex.Message);
            }
        }
        /// <summary>
        /// 控件绑定数据库值
        /// </summary>
        public void DataReflsh()
        {
            //初始化时加载所有产品类型
            string sql = "select Production_ID, Production_Name from dbo.R_PRODUCTION_T where DeleteFlag='0'";
            DataTable dt = new DataTable();
            dt = clsCommon.dbSql.ExecuteDataTable(sql);
            CB_ProductionType.DataSource = dt;
            CB_ProductionType.ValueMember = "Production_ID";
            CB_ProductionType.DisplayMember = "Production_Name";
            //初始化时加载返修类型
            string sql1 = "select Category_ID,Category_Name from  dbo.R_CATEGORY_T where DeleteFlag='0'";
            DataTable dt1 = new DataTable();
            dt1 = clsCommon.dbSql.ExecuteDataTable(sql1);
            CB_ReworkCategory.DataSource = dt1;
            CB_ReworkCategory.ValueMember = "Category_ID";
            CB_ReworkCategory.DisplayMember = "Category_Name";
            //初始化工位
            string sql2 = "select Station_ID,Station_Name from dbo.R_STATION_T where DeleteFlag='0'";
            DataTable dt2 = new DataTable();
            dt2 = clsCommon.dbSql.ExecuteDataTable(sql2);
            CB_StationNo.DataSource = dt2;
            CB_StationNo.ValueMember = "Station_ID";
            CB_StationNo.DisplayMember = "Station_Name";

            string sql3 = "select Station_ID,Station_Name from dbo.R_STATION_T where DeleteFlag='0'";
            DataTable dt3 = new DataTable();
            dt3 = clsCommon.dbSql.ExecuteDataTable(sql3);
            cmbStation.DataSource = dt3;
            cmbStation.ValueMember = "Station_ID";
            cmbStation.DisplayMember = "Station_Name";

            dataGridView3.Visible = false;
            dataGridView2.Visible = true;
        }
        /// <summary>
        /// 业务处理，值改变触发事件
        /// </summary>
        /// <param name="itemCode"></param>
        /// <param name="itemValue"></param>
        private void DoOPCEvent(int itemCode, string itemValue)
        {
            #region 对比员工号
            if (itemCode == 1 && Convert.ToInt32(itemValue) == 1) //控制字1 
            {
                string plcWorkerID = OPC.ReadItem(9).ToString(); //读取PLC扫描的员工号
                if (String.IsNullOrEmpty(plcWorkerID)) //如果为空 则延迟300毫秒
                {
                    System.Threading.Thread.Sleep(300);
                    plcWorkerID = OPC.ReadItem(9).ToString(); //读取PLC扫描的员工号
                    if (String.IsNullOrEmpty(plcWorkerID))//如果延迟后还是为空，则向PLC发送错误信息
                    {
                        OPC.WriteItem(24, 1);//往错误代码中写1，表示员工号为空
                        OPC.WriteItem(1, 9);//往控制字中写9 表示错误
                        recordMessage("员工号校验：员工号为空 不通过！");
                    }
                    else
                    {
                        if (plcWorkerID == clsCommon.userName) //扫描的员工号和登陆的员工号一致
                        {
                            OPC.WriteItem(1, 2);//控制字返回2 表示员工号校验通过
                            workID = plcWorkerID; //留给追溯用
                            recordMessage("员工号校验：登陆员工为： " + clsCommon.userName + " 扫描员工为：" + plcWorkerID + " 结果： 通过");
                        }
                        else  //扫描的员工号和登陆的用户不一致
                        {
                            OPC.WriteItem(1, 3); //控制字返回3 表示员工号校验不通过
                            recordMessage("员工号校验：登陆员工为： " + clsCommon.userName + " 扫描员工为：" + plcWorkerID + " 结果： 不通过");
                        }
                    }
                }
                else
                {
                    if (plcWorkerID == clsCommon.userName) //扫描的员工号和登陆的员工号一致
                    {
                        OPC.WriteItem(1, 2);//控制字返回2 表示员工号校验通过
                        workID = plcWorkerID; //留给追溯用
                        recordMessage("员工号校验：登陆员工为： " + clsCommon.userName + " 扫描员工为：" + plcWorkerID + " 结果： 通过");
                    }
                    else  //扫描的员工号和登陆的用户不一致
                    {
                        OPC.WriteItem(1, 3); //控制字返回3 表示员工号校验不通过
                        recordMessage("员工号校验：登陆员工为： " + clsCommon.userName + " 扫描员工为：" + plcWorkerID + " 结果： 不通过");
                    }
                }
            }
            #endregion
            #region 校验PACK-SN是否可以返修
            if (itemCode == 1 && Convert.ToInt32(itemValue) == 41) //控制字41 
            {
                string packsn = OPC.ReadItem(7).ToString(); //读取PLC给的PACK-SN
                if (String.IsNullOrEmpty(packsn)) //如果读取的PACK-SN为空，等待300毫秒
                {
                    System.Threading.Thread.Sleep(300);
                    packsn = OPC.ReadItem(7).ToString();
                    if (String.IsNullOrEmpty(packsn)) //如果延迟300毫秒，还为空，就给PLC错误信息
                    {
                        OPC.WriteItem(24, 7);//往错误代码中写7，表示PACK-SN为空
                        OPC.WriteItem(1, 49);//往控制字中写49 表示错误
                        recordMessage("PACK-SN 校验： PACK-SN 为空！ 结果：不通过");
                    }
                    else
                    {
                        string sql = "select COUNT(ID) from dbo.AutoASS_Keypart WHERE SN='" + packsn + "';";
                        DataTable dt = new DataTable();
                        dt = clsCommon.dbSql.ExecuteDataTable(sql);
                        if (Convert.ToInt32(dt.Rows[0][0].ToString()) > 0) //表示数据库中有数据，在主线体上做过
                        {
                            OPC.WriteItem(1, 42); //表示PACK-SN校验通过
                            packSN = packsn; //赋值 为追溯做铺垫
                            recordMessage("PACK-SN 校验：PACK-SN 为：" + packsn + " 主线记录：" + Convert.ToInt32(dt.Rows[0][0].ToString()) + "  结果：通过");
                        }
                        else
                        {
                            OPC.WriteItem(1, 43); //表示PACK-SN校验不通过
                            recordMessage("PACK-SN 校验：PACK-SN 为：" + packsn + " 主线记录：" + Convert.ToInt32(dt.Rows[0][0].ToString()) + "  结果：不通过");
                        }
                    }
                }
                else
                {
                    string sql = "select COUNT(ID) from dbo.AutoASS_Keypart WHERE SN='" + packsn + "';";
                    DataTable dt = new DataTable();
                    dt = clsCommon.dbSql.ExecuteDataTable(sql);
                    if (Convert.ToInt32(dt.Rows[0][0].ToString()) > 0) //表示数据库中有数据，在主线体上做过
                    {
                        OPC.WriteItem(1, 42); //表示PACK-SN校验通过
                        packSN = packsn; //赋值 为追溯做铺垫
                        recordMessage("PACK-SN 校验：PACK-SN 为：" + packsn + " 主线记录：" + Convert.ToInt32(dt.Rows[0][0].ToString()) + "  结果：通过");
                    }
                    else
                    {
                        OPC.WriteItem(1, 43); //表示PACK-SN校验不通过
                        recordMessage("PACK-SN 校验：PACK-SN 为：" + packsn + " 主线记录：" + Convert.ToInt32(dt.Rows[0][0].ToString()) + "  结果：不通过");
                    }
                }
            }
            #endregion
            #region 请求总步数和名称
            if (itemCode == 1 && Convert.ToInt32(itemValue) == 11) //控制字11 
            {
                productionNo_Trace = production_No; //产品类型ID
                reworkNo_Trace = rework_No;//返修类型ID
                string sql = "select Step_ID,Step_No,Station_Name,Step_Category,SleeveNo,Shelf_No,Number,Gun_No,Program_No,Feacode,ReworkTimes,Material_No,Material_Name,Scrow_No,Scrow_Name,Rework_No,Rework_Name,Production_No,Production_Name,Station_No  from dbo.R_STEP_T where DeleteFlag='0' and Production_No=" + productionNo_Trace + " and Rework_No=" + reworkNo_Trace + ";";
                DataTable dt = new DataTable();
                dt = clsCommon.dbSql.ExecuteDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    OPC.WriteItem(22, dt.Rows.Count);//写给PLC 总步数
                    OPC.WriteItem(21, rework_No);//写给PLC 返修类别编号
                    //    System.Threading.Thread.Sleep(100);
                    OPC.WriteItem(1, 12);//写入完成信号
                    recordMessage("请求总步数和名称： 产品类型：" + dt.Rows[0]["Production_Name"].ToString() + " 返修类型：" + dt.Rows[0]["Rework_Name"].ToString() + " 总步数：" + dt.Rows.Count + " 返工编号：" + rework_No + " 结果：成功");
                }
                else
                {
                    OPC.WriteItem(24, 2);//请求返修名称和总步数时参数错误
                    OPC.WriteItem(1, 19); //写给PLC 请求总步数和返修类别错误
                    recordMessage("请求总步数和名称： 产品类型：" + productionNo_Trace + " 返修类型：" + reworkNo_Trace + " 总步数：" + dt.Rows.Count + " 结果：失败");
                }
            }
            #endregion
            #region 请求步信息
            if (itemCode == 1 && Convert.ToInt32(itemValue) == 21) //控制字21 
            {
                string Askstep = OPC.ReadItem(17).ToString();
                if (String.IsNullOrEmpty(Askstep))
                {
                    System.Threading.Thread.Sleep(300); //如果读的请求步为空，延迟300毫秒
                    Askstep = OPC.ReadItem(17).ToString();
                    #region
                    if (String.IsNullOrEmpty(Askstep))
                    {
                        OPC.WriteItem(24, 3);//写入PLC 反馈错误信息，表示请求步时参数错误
                        OPC.WriteItem(1, 29);//写入PLC 步请求错误
                        recordMessage("请求步： 步：" + Askstep + " 结果：错误");
                    }
                    else
                    {
                        int step = Convert.ToInt32(OPC.ReadItem(17).ToString());
                        if (step <= 0) //检查请求的步数是否不合法
                        {
                            OPC.WriteItem(24, 3);//写入PLC 反馈错误信息，表示请求步时参数错误
                            OPC.WriteItem(1, 29);//写入PLC 步请求错误
                            recordMessage("请求步信息： 步：" + step + " 结果：错误");
                        }
                        else
                        {
                            string sql = "select Step_ID,Step_No,Station_Name,Step_Category,SleeveNo,Shelf_No,Number,Gun_No,Program_No,Feacode,ReworkTimes,Material_No,Material_Name,Scrow_No,Scrow_Name,Rework_No,Rework_Name,Production_No,Production_Name,Station_No,Module_No  from dbo.R_STEP_T where DeleteFlag='0' and Production_No=" + production_No + " and Rework_No=" + rework_No + " and Step_No=" + step + ";";
                            DataTable dt = new DataTable();
                            dt = clsCommon.dbSql.ExecuteDataTable(sql);
                            if (dt.Rows.Count == 1)  //请求的步正确
                            {
                                #region  给追溯的全局变量赋值 供追溯使用
                                stepNo_Trace = Convert.ToInt32(dt.Rows[0]["Step_No"].ToString());
                                stationName_Trace = dt.Rows[0]["Station_Name"].ToString();
                                stepCategory_Trace = Convert.ToInt32(dt.Rows[0]["Step_Category"].ToString());
                                sleeveNo_Trace = Convert.ToInt32(dt.Rows[0]["SleeveNo"].ToString());
                                shelfNo_Trace = Convert.ToInt32(dt.Rows[0]["Shelf_No"].ToString());
                                number_Trace = Convert.ToInt32(dt.Rows[0]["Number"].ToString());
                                gunNo_Trace = Convert.ToInt32(dt.Rows[0]["Gun_No"].ToString());
                                programNo_Trace = Convert.ToInt32(dt.Rows[0]["Program_No"].ToString());
                                Feacode_Trace = dt.Rows[0]["Feacode"].ToString();
                                reworkTimes_Trace = Convert.ToInt32(dt.Rows[0]["ReworkTimes"].ToString());
                                materialNo_Trace = Convert.ToInt32(dt.Rows[0]["Material_No"].ToString());
                                materialName_Trace = dt.Rows[0]["Material_Name"].ToString();
                                scrowNo_Trace = Convert.ToInt32(dt.Rows[0]["Scrow_No"].ToString());
                                scrowName_Trace = dt.Rows[0]["Scrow_Name"].ToString();
                                reworkNo_Trace = Convert.ToInt32(dt.Rows[0]["Rework_No"].ToString());
                                reworkName_Trace = dt.Rows[0]["Rework_Name"].ToString();
                                productionNo_Trace = Convert.ToInt32(dt.Rows[0]["Production_No"].ToString());
                                productionName_Trace = dt.Rows[0]["Production_Name"].ToString();
                                stationNo_Trace = Convert.ToInt32(dt.Rows[0]["Station_No"].ToString());
                                stepconfigID_Trace = Convert.ToInt32(dt.Rows[0]["Step_ID"].ToString());
                                module_Trace=dt.Rows[0]["Module_No"].ToString();
                                #endregion

                                #region 将步信息写给PLC
                                switch (stepCategory_Trace)
                                {
                                    case 1: //扫描KP
                                        OPC.WriteItem(19, 1);//写入当前步的类别
                                        OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                        OPC.WriteItem(15, number_Trace);//写入需要当前物料的数量
                                        OPC.WriteItem(13, Feacode_Trace);//写入物料的特征码
                                        OPC.WriteItem(1, 22);//写入交互完成信号
                                        recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 当前物料数量：" + number_Trace + " 特征码：" + Feacode_Trace + " 完成信号：" + 22);
                                        break;
                                    case 2: //拧紧
                                        OPC.WriteItem(19, 2);//写入当前步的类别
                                        OPC.WriteItem(12, scrowNo_Trace);//写入当前的螺栓编号
                                        OPC.WriteItem(18, gunNo_Trace);//写入枪号
                                        OPC.WriteItem(11, sleeveNo_Trace);//写入套筒号
                                        OPC.WriteItem(14, programNo_Trace);//程序号
                                        OPC.WriteItem(16, reworkTimes_Trace);//写入返工次数
                                        OPC.WriteItem(1, 22);//写入交互完成信号
                                        recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前螺栓：" + scrowName_Trace + " 枪号：" + gunNo_Trace + " 套筒号：" + sleeveNo_Trace + " 程序号：" + programNo_Trace + " 返工次数：" + reworkTimes_Trace + " 完成信号：" + 22);
                                        break;
                                    case 3: //扫描序列号
                                        OPC.WriteItem(19, 3);//写入当前步的类别
                                        OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                        OPC.WriteItem(1, 22);//写入交互完成信号
                                        recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 完成信号：" + 22);
                                        break;
                                    case 4: //扫描模组
                                        OPC.WriteItem(19, 4);//写入当前步的类别
                                        OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                        OPC.WriteItem(1, 22);//写入交互完成信号
                                        recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 完成信号：" + 22);
                                        break;
                                    case 5: //扫描CSC

                                        OPC.WriteItem(19, 5);//写入当前步的类别
                                        OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                        OPC.WriteItem(1, 22);//写入交互完成信号
                                        recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 完成信号：" + 22);
                                        break;
                                    case 6: //END
                                        OPC.WriteItem(19, 6);//写入当前步的类别
                                        OPC.WriteItem(1, 22);//写入交互完成信号
                                        recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 完成信号：" + 22);
                                        break;

                                }
                                #endregion
                            }
                            else //请求步不正确
                            {
                                OPC.WriteItem(24, 3);//写入PLC 反馈错误信息，表示请求步时参数错误
                                OPC.WriteItem(1, 29);//写入PLC 步请求错误
                                recordMessage("请求步信息： 步：" + step + " 结果：一个步对应多条配置，请检查配置的是否正确！");
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region
                    int step = Convert.ToInt32(OPC.ReadItem(17).ToString());
                    if (step <= 0) //检查请求的步数是否不合法
                    {
                        OPC.WriteItem(24, 3);//写入PLC 反馈错误信息，表示请求步时参数错误
                        OPC.WriteItem(1, 29);//写入PLC 步请求错误
                        recordMessage("请求步信息： 步：" + step + " 结果：错误");
                    }
                    else
                    {
                        string sql = "select Step_ID,Step_No,Station_Name,Step_Category,SleeveNo,Shelf_No,Number,Gun_No,Program_No,Feacode,ReworkTimes,Material_No,Material_Name,Scrow_No,Scrow_Name,Rework_No,Rework_Name,Production_No,Production_Name,Station_No,Module_No  from dbo.R_STEP_T where DeleteFlag='0' and Production_No=" + production_No + " and Rework_No=" + rework_No + " and Step_No=" + step + ";";
                        DataTable dt = new DataTable();
                        dt = clsCommon.dbSql.ExecuteDataTable(sql);
                        if (dt.Rows.Count == 1)  //请求的步正确
                        {
                            #region  给追溯的全局变量赋值 供追溯使用
                            stepNo_Trace = Convert.ToInt32(dt.Rows[0]["Step_No"].ToString());
                            stationName_Trace = dt.Rows[0]["Station_Name"].ToString();
                            stepCategory_Trace = Convert.ToInt32(dt.Rows[0]["Step_Category"].ToString());
                            sleeveNo_Trace = Convert.ToInt32(dt.Rows[0]["SleeveNo"].ToString());
                            shelfNo_Trace = Convert.ToInt32(dt.Rows[0]["Shelf_No"].ToString());
                            number_Trace = Convert.ToInt32(dt.Rows[0]["Number"].ToString());
                            gunNo_Trace = Convert.ToInt32(dt.Rows[0]["Gun_No"].ToString());
                            programNo_Trace = Convert.ToInt32(dt.Rows[0]["Program_No"].ToString());
                            Feacode_Trace = dt.Rows[0]["Feacode"].ToString();
                            reworkTimes_Trace = Convert.ToInt32(dt.Rows[0]["ReworkTimes"].ToString());
                            materialNo_Trace = Convert.ToInt32(dt.Rows[0]["Material_No"].ToString());
                            materialName_Trace = dt.Rows[0]["Material_Name"].ToString();
                            scrowNo_Trace = Convert.ToInt32(dt.Rows[0]["Scrow_No"].ToString());
                            scrowName_Trace = dt.Rows[0]["Scrow_Name"].ToString();
                            reworkNo_Trace = Convert.ToInt32(dt.Rows[0]["Rework_No"].ToString());
                            reworkName_Trace = dt.Rows[0]["Rework_Name"].ToString();
                            productionNo_Trace = Convert.ToInt32(dt.Rows[0]["Production_No"].ToString());
                            productionName_Trace = dt.Rows[0]["Production_Name"].ToString();
                            stationNo_Trace = Convert.ToInt32(dt.Rows[0]["Station_No"].ToString());
                            stepconfigID_Trace = Convert.ToInt32(dt.Rows[0]["Step_ID"].ToString());
                            module_Trace= dt.Rows[0]["Module_No"].ToString();
                            #endregion

                            #region 将步信息写给PLC
                            switch (stepCategory_Trace)
                            {
                                case 1: //扫描KP
                                    OPC.WriteItem(19, 1);//写入当前步的类别
                                    OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                    OPC.WriteItem(15, number_Trace);//写入需要当前物料的数量
                                    OPC.WriteItem(13, Feacode_Trace);//写入物料的特征码
                                    OPC.WriteItem(1, 22);//写入交互完成信号
                                    recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 当前物料数量：" + number_Trace + " 特征码：" + Feacode_Trace + " 完成信号：" + 22);
                                    break;
                                case 2: //拧紧
                                    OPC.WriteItem(19, 2);//写入当前步的类别
                                    OPC.WriteItem(12, scrowNo_Trace);//写入当前的螺栓编号
                                    OPC.WriteItem(18, gunNo_Trace);//写入枪号
                                    OPC.WriteItem(11, sleeveNo_Trace);//写入套筒号
                                    OPC.WriteItem(14, programNo_Trace);//程序号
                                    OPC.WriteItem(16, reworkTimes_Trace);//写入返工次数
                                    OPC.WriteItem(1, 22);//写入交互完成信号
                                    recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前螺栓：" + scrowName_Trace + " 枪号：" + gunNo_Trace + " 套筒号：" + sleeveNo_Trace + " 程序号：" + programNo_Trace + " 返工次数：" + reworkTimes_Trace + " 完成信号：" + 22);
                                    break;
                                case 3: //扫描序列号
                                    OPC.WriteItem(19, 3);//写入当前步的类别
                                    OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                    OPC.WriteItem(1, 22);//写入交互完成信号
                                    recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 完成信号：" + 22);
                                    break;
                                case 4: //扫描模组
                                    OPC.WriteItem(19, 4);//写入当前步的类别
                                    OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                    OPC.WriteItem(1, 22);//写入交互完成信号
                                    recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 完成信号：" + 22);
                                    break;
                                case 5: //扫描CSC

                                    OPC.WriteItem(19, 5);//写入当前步的类别
                                    OPC.WriteItem(12, materialNo_Trace);//写入当前的物料编号
                                    OPC.WriteItem(1, 22);//写入交互完成信号
                                    recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 当前物料：" + materialName_Trace + " 完成信号：" + 22);
                                    break;
                                case 6: //END
                                    OPC.WriteItem(19, 6);//写入当前步的类别
                                    OPC.WriteItem(1, 22);//写入交互完成信号
                                    recordMessage("请求步信息：步：" + step + " 当前步类别：" + stepCategory_Trace + " 完成信号：" + 22);
                                    break;

                            }
                            #endregion
                        }
                        else //请求步不正确
                        {
                            OPC.WriteItem(24, 3);//写入PLC 反馈错误信息，表示请求步时参数错误
                            OPC.WriteItem(1, 29);//写入PLC 步请求错误
                            recordMessage("请求步信息： 步：" + step + " 结果：一个步对应多条配置，请检查配置的是否正确！");
                        }
                    }
                    #endregion
                }

            }
            #endregion
            #region 校验PN号 从配置的PN号和数据库中的PN号进行对比
            if (itemCode == 1 && Convert.ToInt32(itemValue) == 51) //控制字51 PN校验
            {
                string kp = OPC.ReadItem(4).ToString();//当前KP号
                if (!String.IsNullOrEmpty(kp))
                {
                    string sql = "select MATERIAL_PN  from dbo.XH_MATERIAL_CHECK_T where MATERIAL_SN='" + kp + "';";
                    DataTable dt = new DataTable();
                    dt = clsCommon.dbSql.ExecuteDataTable(sql);
                    if (dt.Rows.Count == 1)  //有物料对应PN且是唯一的
                    {
                        string materialPn = dt.Rows[0]["MATERIAL_PN"].ToString();
                        if (materialPn == Feacode_Trace) //PN号一致的情况下
                        {
                            OPC.WriteItem(1, 52);//对比通过
                            recordMessage("PN对比：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 物料批次号：" + kp + "物料配置PN号： " + Feacode_Trace + " MIS数据库PN号： " + materialPn + " PN对比成功");
                        }
                        else
                        {
                            OPC.WriteItem(1, 53);//对比不通过
                            recordMessage("PN对比：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 物料批次号：" + kp + "物料配置PN号： " + Feacode_Trace + " MIS数据库PN号： " + materialPn + " PN对比失败");
                        }
                    }
                    else
                    {
                        OPC.WriteItem(1, 53);//对比不通过
                        recordMessage("PN对比：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 物料批次号：" + kp + "物料配置PN号： " + Feacode_Trace + " MIS数据库PN号条数： " + dt.Rows.Count + " PN对比失败");
                    }
                }
                else
                {
                    OPC.WriteItem(1, 59);//KP号为空
                    recordMessage("PN对比：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 物料批次号：" + kp + "物料配置PN号： " + Feacode_Trace + " KP号为空，校验错误。");
                }
            }
            #endregion
            #region 追溯
            if (itemCode == 1 && Convert.ToInt32(itemValue) == 31) //控制字31 
            {
                string csc = OPC.ReadItem(2).ToString(); //当前csc号
                string kp = OPC.ReadItem(4).ToString();//当前KP号
                string T = OPC.ReadItem(23).ToString();//当前扭矩值
                string A = OPC.ReadItem(5).ToString();//当前角度值
                string P = OPC.ReadItem(10).ToString();//当前程序号
                string C = OPC.ReadItem(3).ToString();//当前循环号
                string R = OPC.ReadItem(20).ToString();//当前结果
                switch (stepCategory_Trace)
                {
                    case 1: //普通物料追溯
                        if (!String.IsNullOrEmpty(kp))
                        {
                            #region 特殊处理JOB2模组物料更换
                            if (stationName_Trace == "OP1032B" || stationName_Trace == "OP1032A")
                            {
                                if (!String.IsNullOrEmpty(module_Trace))
                                {
                                    string slq1 = "select * from dbo.AutoASS_Keypart where SN like '%" + packSN + "' and Keypart_Name like '" + module_Trace + "模组' AND Keypart_NUM = '" + module_Trace + "'";
                                    DataTable dt = new DataTable();
                                    dt = clsCommon.dbSql.ExecuteDataTable(slq1);
                                    string moduleNo = dt.Rows[0]["Module"].ToString();
                                    string sql = "insert dbo.AutoASS_Keypart_Module(DT,Transfer,Mode,ST,SN,WID,TID,Keypart_ID,Keypart_Name,Keypart_PN,Keypart_NUM)values(GETDATE(),1,0,'"
                                  + stationName_Trace + "','" + moduleNo + "','" + workID + "','REWORK'," + materialNo_Trace + ",'" + materialName_Trace + "','" + Feacode_Trace + "','" + kp + "');";
                                    if (clsCommon.dbSql.ExecuteNonQuery(sql) > 0) //追溯成功
                                    {
                                        OPC.WriteItem(1, 32);//追溯完成
                                        recordMessage("追溯：工位：" + stationName_Trace + " 模组号:" + moduleNo + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 特征码：" + Feacode_Trace + " 批次号：" + kp + " 追溯完成");
                                    }
                                    else
                                    {
                                        OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                        OPC.WriteItem(1, 39);//追溯错误
                                        recordMessage("追溯：工位：" + stationName_Trace + " 模组号:" + moduleNo + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 特征码：" + Feacode_Trace + " 批次号：" + kp + " 追溯错误");
                                    }
                                }
                                else
                                {
                                    OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " 模组号:" + moduleNo + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 特征码：" + Feacode_Trace + " 批次号：" + kp + " 追溯错误");
                                }
                            }
                            #endregion
                            else
                            {
                           
                                string sql = "insert dbo.AutoASS_Keypart(DT,Transfer,Mode,ST,SN,WID,TID,Keypart_ID,Keypart_Name,Keypart_PN,Keypart_NUM)values(GETDATE(),1,0,'"
                                    + stationName_Trace + "','" + packSN + "','" + workID + "','REWORK'," + materialNo_Trace + ",'" + materialName_Trace + "','" + Feacode_Trace + "','" + kp + "');";
                                if (clsCommon.dbSql.ExecuteNonQuery(sql) > 0) //追溯成功
                                {
                                    OPC.WriteItem(1, 32);//追溯完成
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 特征码：" + Feacode_Trace + " 批次号：" + kp + " 追溯完成");
                                }
                                else
                                {
                                    OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 特征码：" + Feacode_Trace + " 批次号：" + kp + " 追溯错误");
                                }
                            }
                        }
                        else
                        {
                            OPC.WriteItem(24, 8);//写入PLC 反馈错误信息，表示追溯时参数错误
                            OPC.WriteItem(1, 39);//追溯错误
                            recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 特征码：" + Feacode_Trace + " 批次号：" + kp + " 追溯错误");
                        }
                        break;
                    case 2: //拧紧
                        #region 特殊处理JOB2的模组
                        if (stationName_Trace == "OP1032B" || stationName_Trace == "OP1032A")
                        {
                            if (!String.IsNullOrEmpty(module_Trace))
                            {
                                string slq1 = "select * from dbo.AutoASS_Keypart where SN like '%" + packSN + "' and Keypart_Name like '" + module_Trace + "模组' AND Keypart_NUM = '" + module_Trace + "'";
                                DataTable dt = new DataTable();
                                dt = clsCommon.dbSql.ExecuteDataTable(slq1);
                                string moduleNo = dt.Rows[0]["Module"].ToString();
                                string sql1 = "insert AutoASS_Bolt_Module(DT,Transfer,Mode,SN,ST,T,A,P,C,R,WID,TID,Keypart_ID,Keypart_Name)values(GETDATE(),1,0,'"
                                   + moduleNo + "','" + stationName_Trace + "','" + T + "','" + A + "','" + P + "','" + C + "','" + R + "','" + workID + "','REWORK'," + scrowNo_Trace + ",'" + scrowName_Trace + "');";
                                if (clsCommon.dbSql.ExecuteNonQuery(sql1) > 0) //追溯成功
                                {
                                    OPC.WriteItem(1, 32);//追溯完成
                                    recordMessage("追溯：工位：" + stationName_Trace + " 模组号:" + moduleNo + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace + " 追溯完成");
                                }
                                else
                                {
                                    OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " 模组号:" + moduleNo + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace + " 追溯错误");
                                }
                            }
                            else
                            {
                                OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                OPC.WriteItem(1, 39);//追溯错误
                                recordMessage("追溯：工位：" + stationName_Trace + " 模组号:" + moduleNo + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace + " 追溯错误");
                            }
                        }
                        #endregion
                        else
                        {
                            if (!String.IsNullOrEmpty(A) && !String.IsNullOrEmpty(T) && !String.IsNullOrEmpty(R) && !String.IsNullOrEmpty(P) && !String.IsNullOrEmpty(C))
                            {
                                string sql1 = "insert AutoASS_Bolt(DT,Transfer,Mode,SN,ST,T,A,P,C,R,WID,TID,Keypart_ID,Keypart_Name)values(GETDATE(),1,0,'"
                                    + packSN + "','" + stationName_Trace + "','" + T + "','" + A + "','" + P + "','" + C + "','" + R + "','" + workID + "','REWORK'," + scrowNo_Trace + ",'" + scrowName_Trace + "');";
                                if (clsCommon.dbSql.ExecuteNonQuery(sql1) > 0) //追溯成功
                                {
                                    OPC.WriteItem(1, 32);//追溯完成
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace + " A " + A + " T " + T + " P " + P + " C " + C + " R " + R + " 追溯完成");
                                }
                                else
                                {
                                    OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace + " A " + A + " T " + T + " P " + P + " C " + C + " R " + R + " 追溯错误");
                                }
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(300);
                                if (!String.IsNullOrEmpty(A) && !String.IsNullOrEmpty(T) && !String.IsNullOrEmpty(R) && !String.IsNullOrEmpty(P) && !String.IsNullOrEmpty(C))
                                {
                                    string sql1 = "insert AutoASS_Bolt(DT,Transfer,Mode,SN,ST,T,A,P,C,R,WID,TID,Keypart_ID,Keypart_Name)values(GETDATE(),1,0,'"
                                   + packSN + "','" + stationName_Trace + "','" + T + "','" + A + "','" + P + "','" + C + "','" + R + "','" + workID + "','REWORK'," + scrowNo_Trace + ",'" + scrowName_Trace + "');";
                                    if (clsCommon.dbSql.ExecuteNonQuery(sql1) > 0) //追溯成功
                                    {
                                        OPC.WriteItem(1, 32);//追溯完成
                                        recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace + " A " + A + " T " + T + " P " + P + " C " + C + " R " + R + " 追溯完成");
                                    }
                                    else
                                    {
                                        OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                        OPC.WriteItem(1, 39);//追溯错误
                                        recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace + " A " + A + " T " + T + " P " + P + " C " + C + " R " + R + " 追溯错误");
                                    }
                                }
                                else
                                {
                                    OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 螺栓编号：" + scrowNo_Trace + " 螺栓名称：" + scrowName_Trace+" A "+A+" T "+T +" P "+P+" C "+C+" R "+R + " 追溯错误");
                                }
                            }
                        }
                        break;
                    case 3://扫描序列号
                        if (!String.IsNullOrEmpty(kp))
                        {
                            string sql2 = "insert dbo.AutoASS_Keypart(DT,Transfer,Mode,ST,SN,WID,TID,Keypart_ID,Keypart_Name,Keypart_NUM)values(GETDATE(),1,0,'"
                                + stationName_Trace + "','" + packSN + "','" + workID + "','REWORK'," + materialNo_Trace + ",'" + materialName_Trace + "','" + kp + "')";
                            if (clsCommon.dbSql.ExecuteNonQuery(sql2) > 0) //追溯成功
                            {
                                OPC.WriteItem(1, 32);//追溯完成
                                recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + kp + " 追溯完成");
                            }
                            else
                            {
                                OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                OPC.WriteItem(1, 39);//追溯错误
                                recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + kp + " 追溯错误");
                            }
                        }
                        else
                        {
                            OPC.WriteItem(24, 8);//写入PLC 反馈错误信息，表示追溯时参数为空
                            OPC.WriteItem(1, 39);//追溯错误
                            recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + kp + " 追溯错误");
                        }
                        break;
                    case 4://扫描模组号
                        if (String.IsNullOrEmpty(csc)) //针对不同的产线数据库不一样
                        {
                            System.Threading.Thread.Sleep(300);//在次读csc，以确定不同的数据库
                            csc = OPC.ReadItem(2).ToString(); //当前csc号
                            if (String.IsNullOrEmpty(csc)) //PL7/PL8线
                            {
                                if (!String.IsNullOrEmpty(kp))
                                {
                                    string sql3 = "insert dbo.AutoASS_Keypart(DT,Transfer,Mode,ST,SN,WID,TID,CSC,Keypart_ID,Keypart_Name,Keypart_PN,Keypart_NUM)values(GETDATE(),1,0,'"
                                        + stationName_Trace + "','" + packSN + "','" + workID + "','REWORK','" + kp + "'," + materialNo_Trace + ",'" + materialName_Trace + "','" + kp + "','" + materialName_Trace.Substring(0, materialName_Trace.Length - 2) + "')";
                                    if (clsCommon.dbSql.ExecuteNonQuery(sql3) > 0) //追溯成功
                                    {
                                        OPC.WriteItem(1, 32);//追溯完成
                                        recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " KP：" + kp + " 追溯完成");
                                    }
                                    else
                                    {
                                        OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                        OPC.WriteItem(1, 39);//追溯错误
                                        recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " KP：" + kp + " 追溯错误");
                                    }
                                }
                                else
                                {
                                    OPC.WriteItem(24, 8);//写入PLC 反馈错误信息，表示追溯时kp为空
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " KP：" + kp + " 追溯错误");
                                }
                            }
                            else //JOB2 线
                            {
                                if (!String.IsNullOrEmpty(kp))
                                {
                                    string sql4 = "insert dbo.AutoASS_Keypart(DT,Transfer,Mode,ST,SN,WID,TID,CSC,Module,Keypart_ID,Keypart_Name,Keypart_PN,Keypart_NUM)values(GETDATE(),1,0,'"
                                        + stationName_Trace + "','" + packSN + "','" + workID + "','REWORK','" + csc + "','" + kp + "'," + materialNo_Trace + ",'" + materialName_Trace + "','" + kp + "','" + materialName_Trace.Substring(0, materialName_Trace.Length - 2) + "');";
                                    if (clsCommon.dbSql.ExecuteNonQuery(sql4) > 0) //追溯成功
                                    {
                                        OPC.WriteItem(1, 32);//追溯完成
                                        recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + csc + " 模组号：" + kp + " 追溯完成");
                                    }
                                    else
                                    {
                                        OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                        OPC.WriteItem(1, 39);//追溯错误
                                        recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + csc + " 模组号：" + kp + " 追溯错误");
                                    }
                                }
                                else
                                {
                                    OPC.WriteItem(24, 8);//写入PLC 反馈错误信息，表示追溯时参数错误
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + csc + " 模组号：" + kp + " 追溯错误");
                                }
                            }
                        }
                        else  //JOB2 线
                        {
                            if (!String.IsNullOrEmpty(kp))
                            {
                                string sql4 = "insert dbo.AutoASS_Keypart(DT,Transfer,Mode,ST,SN,WID,TID,CSC,Module,Keypart_ID,Keypart_Name,Keypart_PN,Keypart_NUM)values(GETDATE(),1,0,'"
                               + stationName_Trace + "','" + packSN + "','" + workID + "','REWORK','" + csc + "','" + kp + "'," + materialNo_Trace + ",'" + materialName_Trace + "','" + kp + "','" + materialName_Trace.Substring(0, materialName_Trace.Length - 2) + "');";
                                if (clsCommon.dbSql.ExecuteNonQuery(sql4) > 0) //追溯成功
                                {
                                    OPC.WriteItem(1, 32);//追溯完成
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + csc + " 模组号：" + kp + " 追溯完成");
                                }
                                else
                                {
                                    OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                    OPC.WriteItem(1, 39);//追溯错误
                                    recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + csc + " 模组号：" + kp + " 追溯错误");
                                }
                            }
                            else
                            {
                                OPC.WriteItem(24, 8);//写入PLC 反馈错误信息，表示追溯时参数错误
                                OPC.WriteItem(1, 39);//追溯错误
                                recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " 二维码：" + csc + " 模组号：" + kp + " 追溯错误");
                            }
                        }

                        break;
                    case 5:// 扫描CSC
                        if (!String.IsNullOrEmpty(kp))
                        {
                            string sql5 = "insert dbo.AutoASS_Keypart(DT,Transfer,Mode,ST,SN,WID,TID,CSC,Keypart_ID,Keypart_Name,Keypart_PN,Keypart_NUM)values(GETDATE(),1,0,'"
                                       + stationName_Trace + "','" + packSN + "','" + workID + "','REWORK','" + kp + "'," + materialNo_Trace + ",'" + materialName_Trace + "','CSC','" + kp + "')";
                            if (clsCommon.dbSql.ExecuteNonQuery(sql5) > 0) //追溯成功
                            {
                                OPC.WriteItem(1, 32);//追溯完成
                                recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " CSC：" + kp + " 追溯完成");
                            }
                            else
                            {
                                OPC.WriteItem(24, 4);//写入PLC 反馈错误信息，表示追溯时参数错误
                                OPC.WriteItem(1, 39);//追溯错误
                                recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " CSC：" + kp + " 追溯错误");
                            }
                        }
                        else
                        {
                            OPC.WriteItem(24, 8);//写入PLC 反馈错误信息，表示追溯时kp为空
                            OPC.WriteItem(1, 39);//追溯错误
                            recordMessage("追溯：工位：" + stationName_Trace + " PACK-SN:" + packSN + " 员工号：" + workID + " 物料编号：" + materialNo_Trace + " 物料名称：" + materialName_Trace + " CSC：" + kp + " 追溯错误");
                        }
                        break;
                }
            }
            #endregion

        }
        /// <summary>
        /// 日志记录
        /// </summary>
        /// <param name="message"></param>
        private void recordMessage(string message)
        {
            rtbDataHistory.AppendText(DateTime.Now.ToString("yy.MM.dd - HH:mm:ss") + "  ：  " + message + Environment.NewLine);
            Log.InformationLog.Info(message);
        }


        /// <summary>
        /// 工位数据查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            string sql = "";

            try
            {
                if (cmbType.SelectedIndex == 0)
                {
                    if (txtSn.Text != string.Empty)
                    {
                        sql = "select top " + Convert.ToInt32(nudSize.Value) +
                            " id as 编号," +
                            " DT as 日期," +
                            " Transfer as 上传标记," +
                            " Mode as 模式," +
                            " ST as 工位," +
                            " SN as SN," +
                            " WID as 员工号," +
                            " CSC as CSC," +
                            " Module as 模组," + 
                            " Keypart_ID as 物料编号," +
                            " Keypart_Name as 物料名称," +
                            " Keypart_PN as 物料PN," +
                            " Keypart_NUM as 物料批次号" +
                            " from AutoASS_Keypart where SN like '%" + txtSn.Text + "' and ST='" + cmbStation.Text + "' order by id desc";//sql语句
                    }
                    else
                    {
                        sql = "select top " + Convert.ToInt32(nudSize.Value) +
                            " id as 编号," +
                            " DT as 日期," +
                            " Transfer as 上传标记," +
                            " Mode as 模式," +
                            " ST as 工位," +
                            " SN as SN," +
                            " WID as 员工号," +
                            " CSC as CSC," +
                            " Module as 模组," +  
                            " Keypart_ID as 物料编号," +
                            " Keypart_Name as 物料名称," +
                            " Keypart_PN as 物料PN," +
                            " Keypart_NUM as 物料批次号" +
                            " from AutoASS_Keypart where ST='" + cmbStation.Text + "' order by id desc";//sql语句
                    }
                }
                else if (cmbType.SelectedIndex == 1)
                {
                    if (txtSn.Text != string.Empty)
                    {
                        sql = "select top " + Convert.ToInt32(nudSize.Value) +
                            " id as 编号," +
                            " DT as 日期," +
                            " Transfer as 上传标记," +
                            " Mode as 模式," +
                            " ST as 工位," +
                            " SN as SN," +
                            " T as 扭矩值," +
                            " A as 角度值," +
                            " P as 程序号," +
                            " C as 循环号," +
                            " R as 合格标记," +
                            " WID as 员工号," +
                            " Keypart_ID as 螺栓编号," +
                            " Keypart_Name as 螺栓名称" +
                            " from AutoASS_Bolt where SN like '%" + txtSn.Text + "' and ST='" + cmbStation.Text + "' order by id desc";//sql语句
                    }
                    else
                    {
                        sql = "select top " + Convert.ToInt32(nudSize.Value) +
                            " id as 编号," +
                            " DT as 日期," +
                            " Transfer as 上传标记," +
                            " Mode as 模式," +
                            " ST as 工位," +
                            " SN as SN," +
                            " T as 扭矩值," +
                            " A as 角度值," +
                            " P as 程序号," +
                            " C as 循环号," +
                            " R as 合格标记," +
                            " WID as 员工号," +
                            " Keypart_ID as 螺栓编号," +
                            " Keypart_Name as 螺栓名称" +
                            " from AutoASS_Bolt where ST='" + cmbStation.Text + "' order by id desc";//sql语句
                    }
                }

                dataGridView1.DataSource = clsCommon.dbSql.ExecuteDataTable(sql);
            }
            catch (Exception ex)
            {
                Log.ErrLog.Error("查询错误：" + ex.Message);
                MessageBox.Show("查询错误！", "提示");
            }
            finally
            {
                button1.Enabled = true;
            }
        }
        /// <summary>
        /// 心跳和日期
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (myselfBeat)
                {
                    OPC.WriteItem(0, false);
                    myselfBeat = false;
                }
                else
                {
                    OPC.WriteItem(0, true);
                    myselfBeat = true;
                }
                //心跳位
                bool lifeBeat = Convert.ToBoolean(OPC.ReadItem(25).ToString());
                if (lifeBeat)
                {
                    pictureBox1.BackColor = Color.Lime;
                }
                else
                {
                    pictureBox1.BackColor = panel1.BackColor;
                }
                //日期转换
                string weekDay;
                switch (DateTime.Now.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        weekDay = "星期一";
                        break;
                    case DayOfWeek.Tuesday:
                        weekDay = "星期二";
                        break;
                    case DayOfWeek.Wednesday:
                        weekDay = "星期三";
                        break;
                    case DayOfWeek.Thursday:
                        weekDay = "星期四";
                        break;
                    case DayOfWeek.Friday:
                        weekDay = "星期五";
                        break;
                    case DayOfWeek.Saturday:
                        weekDay = "星期六";
                        break;
                    case DayOfWeek.Sunday:
                        weekDay = "星期日";
                        break;
                    default:
                        weekDay = "";
                        break;
                }
                //  label4.Text = DateTime.Now.ToString() + "  " + weekDay;
                label4.Text = DateTime.Now.ToString();
            }
            catch
            {
                recordMessage("写心跳位失败,请重启程序！");
                timer1.Enabled = false;
            }
        }
        /// <summary>
        /// 显示员工管理窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 员工管理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmManagement fm = new frmManagement();
            fm.ShowDialog();
        }

        private void 注销ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
        #region 最小化不在任务栏显示
        /// <summary>
        /// 屏幕右下角图标方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                //this.ShowInTaskbar = true;
                this.Show();
                this.WindowState = System.Windows.Forms.FormWindowState.Normal;
                notifyIcon1.Visible = false;
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                //this.ShowInTaskbar = false;
                this.Hide();
                notifyIcon1.Visible = true;
            }
        }
        #endregion
        /// <summary>
        /// 获取配置的物料名称
        /// </summary>
        /// <param name="pn"></param>
        /// <returns></returns>
        public string GetMaterialNameByPn(string pn)
        {
            string result = null;
            XmlDocument xmlDoc = new XmlDocument();
            string addr = "MaterialPnConfig.xml";
            xmlDoc.Load(addr);
            XmlNode alarmNode = xmlDoc.SelectSingleNode("config");
            XmlNodeList keys = alarmNode.ChildNodes;
            foreach (XmlNode key in keys)
            {
                if (key.Attributes["pn"] != null && key.Attributes["pn"].Value.Length > 0 && key.Attributes["pn"].Value == pn)
                {
                    result = key.InnerText;
                }
            }
            return result;
        }
        /// <summary>
        /// 获取特殊PN对应的二维码
        /// </summary>
        /// <param name="pn"></param>
        /// <returns></returns>
        public string GetSpecialByPn(string pn)
        {
            string result = null;
            XmlDocument xmlDoc = new XmlDocument();
            string addr = "SpecialPnConfig.xml";
            xmlDoc.Load(addr);
            XmlNode alarmNode = xmlDoc.SelectSingleNode("config");
            XmlNodeList keys = alarmNode.ChildNodes;
            foreach (XmlNode key in keys)
            {
                if (key.Attributes["pn"] != null && key.Attributes["pn"].Value.Length > 0 && key.Attributes["pn"].Value == pn)
                {
                    result = key.InnerText;
                }
            }
            return result;
        }
        /// <summary>
        /// 页面逻辑和工位关联
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CB_SCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            #region 页面逻辑
            switch (CB_SCategory.SelectedIndex)
            {

                case 0: //扫描批次号
                    MaterialShelfpanel.Visible = true;
                    MaterialNumpanel.Visible = true;
                    FeatureCodepanel.Visible = true;
                    StationMaterialPanel.Visible = true;
                   

                    StationScrowPanel.Visible = false;
                    Sleevepanel.Visible = false;
                    Gunpanel.Visible = false;
                    Programpanel.Visible = false;
                    Reworkpanel.Visible = false;
                    if (CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]) == "OP1032A" || CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]) == "OP1032B")
                    {
                        panel3.Visible = true;
                    }
                    else
                    {
                        panel3.Visible = false;
                    }

                    string sql = "select Station_Material_ID,Material_Name  from dbo.R_STATION_MATERIAL_T where Station_ID= " + CB_StationNo.SelectedValue;
                    DataTable dt = new DataTable();
                    dt = clsCommon.dbSql.ExecuteDataTable(sql);
                    CB_StationMaterial.DataSource = dt;
                    CB_StationMaterial.ValueMember = "Station_Material_ID";
                    CB_StationMaterial.DisplayMember = "Material_Name";
                    break;
                case 1:
                    MaterialShelfpanel.Visible = false;
                    MaterialNumpanel.Visible = false;
                    FeatureCodepanel.Visible = false;
                    Reworkpanel.Visible = true;
                    Sleevepanel.Visible = true;
                    Gunpanel.Visible = true;
                    Programpanel.Visible = true;
                    StationMaterialPanel.Visible = false;
                    StationScrowPanel.Visible = true;
                    if (CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]) == "OP1032A" || CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]) == "OP1032B")
                    {
                        panel3.Visible = true;
                    }
                    else
                    {
                        panel3.Visible = false;
                    }

                    string sql1 = "select Station_Screw_ID,Screw_Name  from dbo.R_STATION_SCREW_T where Station_ID=" + CB_StationNo.SelectedValue;
                    DataTable dt1 = new DataTable();
                    dt1 = clsCommon.dbSql.ExecuteDataTable(sql1);
                    CB_StationScrow.DataSource = dt1;
                    CB_StationScrow.ValueMember = "Station_Screw_ID";
                    CB_StationScrow.DisplayMember = "Screw_Name";
                    break;
                case 2:
                    MaterialShelfpanel.Visible = false;
                    MaterialNumpanel.Visible = false;
                    FeatureCodepanel.Visible = false;
                    StationMaterialPanel.Visible = true;
                    panel3.Visible = false;

                    StationScrowPanel.Visible = false;
                    Sleevepanel.Visible = false;
                    Gunpanel.Visible = false;
                    Programpanel.Visible = false;
                    Reworkpanel.Visible = false;

                    string sql2 = "select Station_Material_ID,Material_Name  from dbo.R_STATION_MATERIAL_T where Station_ID= " + CB_StationNo.SelectedValue;
                    DataTable dt2 = new DataTable();
                    dt2 = clsCommon.dbSql.ExecuteDataTable(sql2);
                    CB_StationMaterial.DataSource = dt2;
                    CB_StationMaterial.ValueMember = "Station_Material_ID";
                    CB_StationMaterial.DisplayMember = "Material_Name";
                    break;
                case 3:
                    MaterialShelfpanel.Visible = false;
                    MaterialNumpanel.Visible = false;
                    FeatureCodepanel.Visible = false;
                    StationMaterialPanel.Visible = true;
                    panel3.Visible = false;

                    StationScrowPanel.Visible = false;
                    Sleevepanel.Visible = false;
                    Gunpanel.Visible = false;
                    Programpanel.Visible = false;
                    Reworkpanel.Visible = false;

                    string sql3 = "select Station_Material_ID,Material_Name  from dbo.R_STATION_MATERIAL_T where Station_ID= " + CB_StationNo.SelectedValue;
                    DataTable dt3 = new DataTable();
                    dt3 = clsCommon.dbSql.ExecuteDataTable(sql3);
                    CB_StationMaterial.DataSource = dt3;
                    CB_StationMaterial.ValueMember = "Station_Material_ID";
                    CB_StationMaterial.DisplayMember = "Material_Name";
                    break;

                case 4:
                    MaterialShelfpanel.Visible = false;
                    MaterialNumpanel.Visible = false;
                    FeatureCodepanel.Visible = false;
                    StationMaterialPanel.Visible = true;
                    panel3.Visible = false;

                    StationScrowPanel.Visible = false;
                    Sleevepanel.Visible = false;
                    Gunpanel.Visible = false;
                    Programpanel.Visible = false;
                    Reworkpanel.Visible = false;

                    string sql4 = "select Station_Material_ID,Material_Name  from dbo.R_STATION_MATERIAL_T where Station_ID= " + CB_StationNo.SelectedValue;
                    DataTable dt4 = new DataTable();
                    dt4 = clsCommon.dbSql.ExecuteDataTable(sql4);
                    CB_StationMaterial.DataSource = dt4;
                    CB_StationMaterial.ValueMember = "Station_Material_ID";
                    CB_StationMaterial.DisplayMember = "Material_Name";
                    break;
                default:
                    MaterialShelfpanel.Visible = false;
                    MaterialNumpanel.Visible = false;
                    FeatureCodepanel.Visible = false;
                    Reworkpanel.Visible = false;
                    StationMaterialPanel.Visible = false;
                    panel3.Visible = false;

                    StationScrowPanel.Visible = false;
                    Sleevepanel.Visible = false;
                    Gunpanel.Visible = false;
                    Programpanel.Visible = false;
                    label3.Visible = false;
                    break;

            }
            #endregion
            #region

            #endregion
        }
        /// <summary>
        /// 监听OPC，开启业务处理方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox6_Click(object sender, EventArgs e)
        {
            if (dataGridView3.Rows.Count == 0)
            {
                MessageBox.Show("该产品没有配置流程，请配置流程！");
            }
            else
            {
                #region OPC连接初始化
                try
                {
                    production_No = Convert.ToInt32(CB_ProductionType.SelectedValue.ToString());
                    rework_No = Convert.ToInt32(CB_ReworkCategory.SelectedValue.ToString());
                    tabControl1.SelectTab(0); //切换到第一个页面
                    OPC = new OPCCreate();
                    OPC.EventDataChanged += new EventDataChanged(OnEventDataChanged);
                    OPC.Run();
                    timer1.Enabled = true;
                    recordMessage("OPC Server连接成功！");
                    OPC.WriteItem(8, clsCommon.userName);  //OPC写入登陆员工号
                }
                catch (Exception)
                {
                    MessageBox.Show("OPC Server连接失败，请检查PLC是否正常通讯。");
                    recordMessage("OPC Server连接失败，请检查PLC是否正常通讯。");
                }

                #endregion
            }

        }
        #region 显示各个基础配置的
        private void 工位物料配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddStationMaterial asm = new AddStationMaterial();
            asm.ShowDialog();
        }

        private void 工位螺栓配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddStationScrow ass = new AddStationScrow();
            ass.ShowDialog();
        }

        private void 产品类型配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddProductionType apt = new AddProductionType(this);
            apt.ShowDialog();
        }

        private void 返修类别配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddReworkCategory arc = new AddReworkCategory(this);
            arc.ShowDialog();
        }

        private void 增加工位ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddStation astion = new AddStation(this);
            astion.ShowDialog();
        }
        #endregion
        /// <summary>
        /// 提交配置到DATAGRIDVIEW2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (dataGridView2.DataSource == null) //第一次绑定，没有存入过数据库
            {
                if (changeRow == -1)
                {
                    #region 新增配置
                   
                    //类别
                    if (CB_SCategory.SelectedIndex >= 0)
                    {
                        this.dataGridView2.Rows.Add();
                        //步序
                        if (NUM_StepNo.Value > 0)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[0].Value = NUM_StepNo.Value;
                        }
                        else
                        {
                            MessageBox.Show("步序必须大于0！");
                        }
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[1].Value = CB_SCategory.SelectedIndex + 1;
                        //套筒号
                        if (TB_SleeveNo.SelectedIndex >= 0)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[2].Value = TB_SleeveNo.SelectedIndex + 1;
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[2].Value = 0;
                        }

                        //料格号
                        if (CB_MaterialShelfNo.SelectedIndex >= 0)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[3].Value = CB_MaterialShelfNo.SelectedIndex + 1;
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[3].Value = 0;
                        }

                        //数量
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[4].Value = TB_MaterialNum.Value;

                        //枪号
                        if (CB_GunNo.SelectedIndex >= 0)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[5].Value = CB_GunNo.SelectedIndex + 1;
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[5].Value = 0;
                        }

                        //程序号
                        if (!String.IsNullOrEmpty(TB_ProgramNo.Text))
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[6].Value = Convert.ToInt32(TB_ProgramNo.Text);
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[6].Value = 0;
                        }

                        //特征码
                        if (!String.IsNullOrEmpty(TB_FeatureCode.Text))
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[7].Value = TB_FeatureCode.Text;
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[7].Value = "";
                        }

                        //返工次数
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[8].Value = TB_ReworkTimes.Value;
                        //当前工位
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[9].Value = CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]);
                        //当前工位对应物料编号
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[10].Value = CB_StationMaterial.SelectedValue;
                        //当前工位对应物料名称
                        if (CB_SCategory.SelectedIndex == 1 || CB_SCategory.SelectedIndex == 5)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[11].Value = "";

                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[11].Value = CB_StationMaterial.GetItemText(CB_StationMaterial.Items[CB_StationMaterial.SelectedIndex]);
                        }

                        //当前工位对应螺栓编号
                        if (CB_SCategory.SelectedIndex == 1)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[12].Value = CB_StationScrow.SelectedValue;
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[12].Value = 0;
                        }
                        //当前工位对应螺栓名称
                        if (CB_SCategory.SelectedIndex == 1)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[13].Value = CB_StationScrow.GetItemText(CB_StationScrow.Items[CB_StationScrow.SelectedIndex]);
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[13].Value = "";
                        }
                        //对应返修类型编号
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[14].Value = CB_ReworkCategory.SelectedValue;
                        //对应返修类型名称
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[15].Value = CB_ReworkCategory.GetItemText(CB_ReworkCategory.Items[CB_ReworkCategory.SelectedIndex]);
                        //对应产品名称编号
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[16].Value = CB_ProductionType.SelectedValue;
                        //对应产品名称
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[17].Value = CB_ProductionType.GetItemText(CB_ProductionType.Items[CB_ProductionType.SelectedIndex]);
                        //工位编号
                        this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[18].Value = CB_StationNo.SelectedValue;
                        //步序编号
                        if (stepId == 0)
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[19].Value = 0;
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[19].Value = stepId;
                        }
                        //模组号
                        if (!String.IsNullOrEmpty(TB_Module.Text))
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[20].Value = TB_Module.Text;
                        }
                        else
                        {
                            this.dataGridView2.Rows[this.dataGridView2.Rows.Count - 1].Cells[20].Value = "";
                        }
                        dataGridView2.Refresh();
                        CB_SCategory.SelectedIndex = -1;
                        CB_MaterialShelfNo.SelectedIndex = -1;
                        CB_GunNo.SelectedIndex = -1;
                        TB_ProgramNo.Text = "";
                        TB_MaterialNum.Value = 0;
                        TB_FeatureCode.Text = "";
                        TB_Module.Text = "";
                        TB_SleeveNo.SelectedIndex = -1;
                        TB_ReworkTimes.Value = 0;
                        CB_StationMaterial.SelectedIndex = -1;
                        CB_StationScrow.SelectedIndex = -1;
                        NUM_StepNo.Value = NUM_StepNo.Value + 1;
                        changeRow = -1;
                        stepId = 0;
                    }
                    else
                    {
                        MessageBox.Show("请选择当前步类别！");
                    }

                  
                    #endregion
                }

                else
                {
                    #region 修改配置
                    if (NUM_StepNo.Value > 0)
                    {
                        this.dataGridView2.Rows[changeRow].Cells[0].Value = NUM_StepNo.Value;
                    }
                    else
                    {
                        MessageBox.Show("步序必须大于0！");
                    }


                    //类别
                    if (CB_SCategory.SelectedIndex >= 0)
                    {
                        this.dataGridView2.Rows[changeRow].Cells[1].Value = CB_SCategory.SelectedIndex + 1;
                        //套筒号
                        if (TB_SleeveNo.SelectedIndex >= 0)
                        {
                            this.dataGridView2.Rows[changeRow].Cells[2].Value = TB_SleeveNo.SelectedIndex + 1;
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[2].Value = 0;
                        }

                        //料格号
                        if (CB_MaterialShelfNo.SelectedIndex >= 0)
                        {
                            this.dataGridView2.Rows[changeRow].Cells[3].Value = CB_MaterialShelfNo.SelectedIndex + 1;
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[3].Value = 0;
                        }

                        //数量
                        this.dataGridView2.Rows[changeRow].Cells[4].Value = TB_MaterialNum.Value;

                        //枪号
                        if (CB_GunNo.SelectedIndex >= 0)
                        {
                            this.dataGridView2.Rows[changeRow].Cells[5].Value = CB_GunNo.SelectedIndex + 1;
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[5].Value = 0;
                        }

                        //程序号
                        if (!String.IsNullOrEmpty(TB_ProgramNo.Text))
                        {
                            this.dataGridView2.Rows[changeRow].Cells[6].Value = Convert.ToInt32(TB_ProgramNo.Text);
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[6].Value = 0;
                        }

                        //特征码
                        if (!String.IsNullOrEmpty(TB_FeatureCode.Text))
                        {
                            this.dataGridView2.Rows[changeRow].Cells[7].Value = TB_FeatureCode.Text;
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[7].Value = "";
                        }

                        //返工次数
                        this.dataGridView2.Rows[changeRow].Cells[8].Value = TB_ReworkTimes.Value;
                        //当前工位
                        this.dataGridView2.Rows[changeRow].Cells[9].Value = CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]);
                        //当前工位对应物料编号
                        this.dataGridView2.Rows[changeRow].Cells[10].Value = CB_StationMaterial.SelectedValue;
                        //当前工位对应物料名称
                        if (CB_SCategory.SelectedIndex == 1 || CB_SCategory.SelectedIndex == 5)
                        {
                            this.dataGridView2.Rows[changeRow].Cells[11].Value = "";

                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[11].Value = CB_StationMaterial.GetItemText(CB_StationMaterial.Items[CB_StationMaterial.SelectedIndex]);
                        }

                        //当前工位对应螺栓编号
                        if (CB_SCategory.SelectedIndex == 1)
                        {
                            this.dataGridView2.Rows[changeRow].Cells[12].Value = CB_StationScrow.SelectedValue;
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[12].Value = 0;
                        }
                        //当前工位对应螺栓名称
                        if (CB_SCategory.SelectedIndex == 1)
                        {
                            this.dataGridView2.Rows[changeRow].Cells[13].Value = CB_StationScrow.GetItemText(CB_StationScrow.Items[CB_StationScrow.SelectedIndex]);
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[13].Value = "";
                        }
                        //对应返修类型编号
                        this.dataGridView2.Rows[changeRow].Cells[14].Value = CB_ReworkCategory.SelectedValue;
                        //对应返修类型名称
                        this.dataGridView2.Rows[changeRow].Cells[15].Value = CB_ReworkCategory.GetItemText(CB_ReworkCategory.Items[CB_ReworkCategory.SelectedIndex]);
                        //对应产品名称编号
                        this.dataGridView2.Rows[changeRow].Cells[16].Value = CB_ProductionType.SelectedValue;
                        //对应产品名称
                        this.dataGridView2.Rows[changeRow].Cells[17].Value = CB_ProductionType.GetItemText(CB_ProductionType.Items[CB_ProductionType.SelectedIndex]);
                        //工位编号
                        this.dataGridView2.Rows[changeRow].Cells[18].Value = CB_StationNo.SelectedValue;
                        //步序编号
                        if (stepId == 0)
                        {
                            this.dataGridView2.Rows[changeRow].Cells[19].Value = 0;
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[19].Value = stepId;
                        }
                        //模组号
                        if (!String.IsNullOrEmpty(TB_Module.Text))
                        {
                            this.dataGridView2.Rows[changeRow].Cells[20].Value = TB_Module.Text;
                        }
                        else
                        {
                            this.dataGridView2.Rows[changeRow].Cells[20].Value = "";
                        }
                        dataGridView2.Refresh();
                        CB_SCategory.SelectedIndex = -1;
                        CB_MaterialShelfNo.SelectedIndex = -1;
                        CB_GunNo.SelectedIndex = -1;
                        TB_ProgramNo.Text = "";
                        TB_MaterialNum.Value = 0;
                        TB_FeatureCode.Text = "";
                        TB_Module.Text = "";
                        TB_SleeveNo.SelectedIndex = -1;
                        TB_ReworkTimes.Value = 0;
                        CB_StationMaterial.SelectedIndex = -1;
                        CB_StationScrow.SelectedIndex = -1;
                        NUM_StepNo.Value = NUM_StepNo.Value + 1;
                        changeRow = -1;
                        stepId = 0;
                    }
                    else
                    {
                        MessageBox.Show("请选择当前步类别！");
                    }

                  
                    #endregion
                }
            }
            else  //存入过数据库，后期修改
            {
                #region
                if (changeRow == -1)
                {
                    #region 新增配置
                    //类别
                    if (CB_SCategory.SelectedIndex >= 0)
                    {
                        tb2.Rows.Add();
                        //步序
                        if (NUM_StepNo.Value > 0)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Step_No"] = NUM_StepNo.Value;
                        }
                        else
                        {
                            MessageBox.Show("步序必须大于0！");
                        }
                        tb2.Rows[tb2.Rows.Count - 1]["Step_Category"] = CB_SCategory.SelectedIndex + 1;
                        //套筒号
                        if (TB_SleeveNo.SelectedIndex >= 0)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["SleeveNo"] = TB_SleeveNo.SelectedIndex + 1;
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["SleeveNo"] = 0;
                        }

                        //料格号
                        if (CB_MaterialShelfNo.SelectedIndex >= 0)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Shelf_No"] = CB_MaterialShelfNo.SelectedIndex + 1;
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Shelf_No"] = 0;
                        }

                        //数量
                        tb2.Rows[tb2.Rows.Count - 1]["Number"] = TB_MaterialNum.Value;

                        //枪号
                        if (CB_GunNo.SelectedIndex >= 0)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Gun_No"] = CB_GunNo.SelectedIndex + 1;
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Gun_No"] = 0;
                        }

                        //程序号
                        if (!String.IsNullOrEmpty(TB_ProgramNo.Text))
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Program_No"] = Convert.ToInt32(TB_ProgramNo.Text);
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Program_No"] = 0;
                        }

                        //特征码
                        if (!String.IsNullOrEmpty(TB_FeatureCode.Text))
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Feacode"] = TB_FeatureCode.Text;
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Feacode"] = "";
                        }

                        //返工次数
                        tb2.Rows[tb2.Rows.Count - 1]["ReworkTimes"] = TB_ReworkTimes.Value;
                        //当前工位
                        tb2.Rows[tb2.Rows.Count - 1]["Station_Name"] = CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]);
                        if (CB_SCategory.SelectedIndex == 1 || CB_SCategory.SelectedIndex == 5)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Material_No"] = 0;
                        }
                        else
                        {
                            //当前工位对应物料编号
                            tb2.Rows[tb2.Rows.Count - 1]["Material_No"] = CB_StationMaterial.SelectedValue;
                        }

                        //当前工位对应物料名称
                        if (CB_SCategory.SelectedIndex == 1 || CB_SCategory.SelectedIndex == 5)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Material_Name"] = "";

                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Material_Name"] = CB_StationMaterial.GetItemText(CB_StationMaterial.Items[CB_StationMaterial.SelectedIndex]);
                        }

                        //当前工位对应螺栓编号
                        if (CB_SCategory.SelectedIndex == 1)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Scrow_No"] = CB_StationScrow.SelectedValue;
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Scrow_No"] = 0;
                        }
                        //当前工位对应螺栓名称
                        if (CB_SCategory.SelectedIndex == 1)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Scrow_Name"] = CB_StationScrow.GetItemText(CB_StationScrow.Items[CB_StationScrow.SelectedIndex]);
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Scrow_Name"] = "";
                        }
                        //对应返修类型编号
                        tb2.Rows[tb2.Rows.Count - 1]["Rework_No"] = CB_ReworkCategory.SelectedValue;
                        //对应返修类型名称
                        tb2.Rows[tb2.Rows.Count - 1]["Rework_Name"] = CB_ReworkCategory.GetItemText(CB_ReworkCategory.Items[CB_ReworkCategory.SelectedIndex]);
                        //对应产品名称编号
                        tb2.Rows[tb2.Rows.Count - 1]["Production_No"] = CB_ProductionType.SelectedValue;
                        //对应产品名称
                        tb2.Rows[tb2.Rows.Count - 1]["Production_Name"] = CB_ProductionType.GetItemText(CB_ProductionType.Items[CB_ProductionType.SelectedIndex]);
                        //工位编号
                        tb2.Rows[tb2.Rows.Count - 1]["Station_No"] = CB_StationNo.SelectedValue;
                        //步序编号
                        if (stepId == 0)
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Step_ID"] = 0;
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Step_ID"] = stepId;
                        }
                        //模组号
                        if (!String.IsNullOrEmpty(TB_Module.Text))
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Module_No"] = TB_Module.Text;
                        }
                        else
                        {
                            tb2.Rows[tb2.Rows.Count - 1]["Module_No"] = "";
                        }
                        dataGridView2.Refresh();
                        CB_SCategory.SelectedIndex = -1;
                        CB_MaterialShelfNo.SelectedIndex = -1;
                        CB_GunNo.SelectedIndex = -1;
                        TB_ProgramNo.Text = "";
                        TB_MaterialNum.Value = 0;
                        TB_FeatureCode.Text = "";
                        TB_Module.Text = "";
                        TB_SleeveNo.SelectedIndex = -1;
                        TB_ReworkTimes.Value = 0;
                        CB_StationMaterial.SelectedIndex = -1;
                        CB_StationScrow.SelectedIndex = -1;
                        NUM_StepNo.Value = NUM_StepNo.Value + 1;
                        changeRow = -1;
                        stepId = 0;
                    }
                    else
                    {
                        MessageBox.Show("请选择当前步类别！");
                    }

                  
                    #endregion
                }
                else
                {
                    #region 修改配置
                    //步序
                    if (NUM_StepNo.Value > 0)
                    {
                        tb2.Rows[changeRow]["Step_No"] = NUM_StepNo.Value;
                    }
                    else
                    {
                        MessageBox.Show("步序必须大于0！");
                    }


                    //类别
                    if (CB_SCategory.SelectedIndex >= 0)
                    {
                        tb2.Rows[changeRow]["Step_Category"] = CB_SCategory.SelectedIndex + 1;
                    }
                    else
                    {
                        MessageBox.Show("请选择当前步类别！");
                    }

                    //套筒号
                    if (TB_SleeveNo.SelectedIndex >= 0)
                    {
                        tb2.Rows[changeRow]["SleeveNo"] = TB_SleeveNo.SelectedIndex + 1;
                    }
                    else
                    {
                        tb2.Rows[changeRow]["SleeveNo"] = 0;
                    }

                    //料格号
                    if (CB_MaterialShelfNo.SelectedIndex >= 0)
                    {
                        tb2.Rows[changeRow]["Shelf_No"] = CB_MaterialShelfNo.SelectedIndex + 1;
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Shelf_No"] = 0;
                    }

                    //数量
                    tb2.Rows[changeRow]["Number"] = TB_MaterialNum.Value;

                    //枪号
                    if (CB_GunNo.SelectedIndex >= 0)
                    {
                        tb2.Rows[changeRow]["Gun_No"] = CB_GunNo.SelectedIndex + 1;
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Gun_No"] = 0;
                    }

                    //程序号
                    if (!String.IsNullOrEmpty(TB_ProgramNo.Text))
                    {
                        tb2.Rows[changeRow]["Program_No"] = Convert.ToInt32(TB_ProgramNo.Text);
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Program_No"] = 0;
                    }

                    //特征码
                    if (!String.IsNullOrEmpty(TB_FeatureCode.Text))
                    {
                        tb2.Rows[changeRow]["Feacode"] = TB_FeatureCode.Text;
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Feacode"] = "";
                    }

                    //返工次数
                    tb2.Rows[changeRow]["ReworkTimes"] = TB_ReworkTimes.Value;
                    //当前工位
                    tb2.Rows[changeRow]["Station_Name"] = CB_StationNo.GetItemText(CB_StationNo.Items[CB_StationNo.SelectedIndex]);
                    if (CB_SCategory.SelectedIndex == 1 || CB_SCategory.SelectedIndex == 5)
                    {
                        tb2.Rows[changeRow]["Material_No"] = 0;
                    }
                    else
                    {
                        //当前工位对应物料编号
                        tb2.Rows[changeRow]["Material_No"] = CB_StationMaterial.SelectedValue;
                    }

                    //当前工位对应物料名称
                    if (CB_SCategory.SelectedIndex == 1 || CB_SCategory.SelectedIndex == 5)
                    {
                        tb2.Rows[changeRow]["Material_Name"] = "";

                    }
                    else
                    {
                        tb2.Rows[changeRow]["Material_Name"] = CB_StationMaterial.GetItemText(CB_StationMaterial.Items[CB_StationMaterial.SelectedIndex]);
                    }

                    //当前工位对应螺栓编号
                    if (CB_SCategory.SelectedIndex == 1)
                    {
                        tb2.Rows[changeRow]["Scrow_No"] = CB_StationScrow.SelectedValue;
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Scrow_No"] = 0;
                    }
                    //当前工位对应螺栓名称
                    if (CB_SCategory.SelectedIndex == 1)
                    {
                        tb2.Rows[changeRow]["Scrow_Name"] = CB_StationScrow.GetItemText(CB_StationScrow.Items[CB_StationScrow.SelectedIndex]);
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Scrow_Name"] = "";
                    }
                    //对应返修类型编号
                    tb2.Rows[changeRow]["Rework_No"] = CB_ReworkCategory.SelectedValue;
                    //对应返修类型名称
                    tb2.Rows[changeRow]["Rework_Name"] = CB_ReworkCategory.GetItemText(CB_ReworkCategory.Items[CB_ReworkCategory.SelectedIndex]);
                    //对应产品名称编号
                    tb2.Rows[changeRow]["Production_No"] = CB_ProductionType.SelectedValue;
                    //对应产品名称
                    tb2.Rows[changeRow]["Production_Name"] = CB_ProductionType.GetItemText(CB_ProductionType.Items[CB_ProductionType.SelectedIndex]);
                    //工位编号
                    tb2.Rows[changeRow]["Station_No"] = CB_StationNo.SelectedValue;
                    //步序编号
                    if (stepId == 0)
                    {
                        tb2.Rows[changeRow]["Step_ID"] = 0;
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Step_ID"] = stepId;
                    }
                    //模组号
                    if (!String.IsNullOrEmpty(TB_Module.Text))
                    {
                        tb2.Rows[changeRow]["Module_No"] = TB_Module.Text;
                    }
                    else
                    {
                        tb2.Rows[changeRow]["Module_No"] = "";
                    }
                    dataGridView2.Refresh();
                    CB_SCategory.SelectedIndex = -1;
                    CB_MaterialShelfNo.SelectedIndex = -1;
                    CB_GunNo.SelectedIndex = -1;
                    TB_ProgramNo.Text = "";
                    TB_MaterialNum.Value = 0;
                    TB_FeatureCode.Text = "";
                    TB_Module.Text = "";
                    TB_SleeveNo.SelectedIndex = -1;
                    TB_ReworkTimes.Value = 0;
                    CB_StationMaterial.SelectedIndex = -1;
                    CB_StationScrow.SelectedIndex = -1;
                    NUM_StepNo.Value = NUM_StepNo.Value + 1;
                    changeRow = -1;
                    stepId = 0;
                    #endregion
                }
                #endregion
            }

        }
        #region 限制只能输入数字
        private bool NumberEntered = false;
        private void TB_ProgramNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (NumberEntered)
            {
                e.Handled = true;
            }
        }

        private void TB_ProgramNo_KeyDown(object sender, KeyEventArgs e)
        {
            NumberEntered = false;
            if ((e.KeyCode < Keys.D0) || (e.KeyCode > Keys.D9 && e.KeyCode < Keys.NumPad0) || (e.KeyCode > Keys.NumPad9))
            {
                if (e.KeyCode != Keys.Back)
                {
                    NumberEntered = true;
                }
            }
        }
        #endregion
        //保存配置
        private void pictureBox7_Click(object sender, EventArgs e)
        {
            if (dataGridView2.Visible == true && dataGridView3.Visible == false)
            {
                if (dataGridView2.DataSource == null)
                {
                    #region 没有绑定数据源的情况下 校验
                    bool a;
                    int count = dataGridView2.Rows.Count;
                    if (count > 0)
                    {
                        int[] step = new int[count];
                        for (int i = 0; i < count; i++)
                        {
                            step[i] = Convert.ToInt32(dataGridView2.Rows[i].Cells[0].Value.ToString());
                        }
                        try
                        {
                            a = CheckStepNo(step);
                            if (a)
                            {
                                Save();
                                DataBind();
                                dataGridView2.Rows.Clear();
                            }
                            else
                            {
                                MessageBox.Show("步序错误，需连续且从1开始！");
                            }
                        }
                        catch
                        {
                            MessageBox.Show("检查步序出错！");
                        }
                    }
                    else
                    {
                        MessageBox.Show("请配置具体信息！");
                    }
                    #endregion
                }
                else
                {
                    #region 绑定数据源的情况下
                    bool a;
                    int count = tb2.Rows.Count;
                    if (count > 0)
                    {
                        int[] step = new int[count];
                        for (int i = 0; i < count; i++)
                        {
                            step[i] = Convert.ToInt32(tb2.Rows[i]["Step_No"].ToString());
                        }
                        try
                        {
                            a = CheckStepNo(step);
                            if (a)
                            {
                                Save();
                                DataBind();
                                tb2.Rows.Clear();
                            }
                            else
                            {
                                MessageBox.Show("步序错误，需连续且从1开始！");
                            }
                        }
                        catch
                        {
                            MessageBox.Show("检查步序出错！");
                        }
                    }
                    else
                    {
                        MessageBox.Show("请配置具体信息！");
                    }
                    #endregion
                }
            }
            else
            {
                MessageBox.Show("请先点击编辑图标，再保存！");
            }


        }
        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            try
            {
                if (dataGridView2.DataSource == null)
                {
                    #region 没有绑定数据源的情况下
                    for (int i = 0; i < this.dataGridView2.Rows.Count; i++)
                    {
                        int stepNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[0].Value);
                        string stationName = this.dataGridView2.Rows[i].Cells[9].Value.ToString();
                        int stepCategory = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[1].Value);
                        int sleeveNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[2].Value);
                        int shelfNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[3].Value);
                        int number = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[4].Value);
                        int gunNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[5].Value);
                        int programNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[6].Value);
                        string Feacode = this.dataGridView2.Rows[i].Cells[7].Value.ToString();
                        int reworkTimes = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[8].Value);
                        int materialNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[10].Value);
                        string materialName = this.dataGridView2.Rows[i].Cells[11].Value.ToString();
                        int scrowNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[12].Value);
                        string scrowName = this.dataGridView2.Rows[i].Cells[13].Value.ToString();
                        int reworkNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[14].Value);
                        string reworkName = this.dataGridView2.Rows[i].Cells[15].Value.ToString();
                        int productionNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[16].Value);
                        string productionName = this.dataGridView2.Rows[i].Cells[17].Value.ToString();
                        int stationNo = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[18].Value);
                        int stepconfigID = Convert.ToInt32(this.dataGridView2.Rows[i].Cells[19].Value);
                        string moduleNo = this.dataGridView2.Rows[i].Cells[20].Value.ToString();
                        if (stepconfigID == 0) // 表示新增记录
                        {
                            string sql1 = "select Step_ID from dbo.R_STEP_T where Step_No=" + stepNo + "  and Station_Name='" + stationName + "' and Step_Category=" + stepCategory + "  and SleeveNo=" + sleeveNo + " and Shelf_No=" + shelfNo + " and Number=" + number + " and Gun_No=" + gunNo + " and Program_No=" + programNo + " and Feacode='" + Feacode + "' and ReworkTimes=" + reworkTimes + " and Material_No=" + materialNo + " and Material_Name='" + materialName + "' and Scrow_No=" + scrowNo + " and Scrow_Name='" + scrowName + "' and Rework_No=" + reworkNo + " and Rework_Name='" + reworkName + "' and Production_No=" + productionNo + " and Production_Name='" + productionName + "' and Station_No=" + stationNo +" and Module_No='"+moduleNo+ "' and DeleteFlag='0';";
                            DataTable dt = new DataTable();
                            dt = clsCommon.dbSql.ExecuteDataTable(sql1);
                            if (dt.Rows.Count > 0)
                            {
                                MessageBox.Show("该配置已存在！");
                            }
                            else
                            {
                                string sql = "insert dbo.R_STEP_T(Step_No,Station_Name,Step_Category,SleeveNo,Shelf_No,Number,Gun_No,Program_No,Feacode,ReworkTimes,Material_No,Material_Name,Scrow_No,Scrow_Name,Rework_No,Rework_Name,Production_No,Production_Name,CreateTime,DeleteFlag,Station_No,Module_No)values(" +
                                +stepNo + ",'" + stationName + "'," + stepCategory + "," + sleeveNo + "," + shelfNo + "," + number + "," + gunNo + "," + programNo + ",'" + Feacode + "'," + reworkTimes + "," + materialNo + ",'" + materialName + "'," + scrowNo + ",'" + scrowName + "'," + reworkNo + ",'" + reworkName + "'," + productionNo + ",'" + productionName + "',GETDATE(),'0'," + stationNo+",'"+moduleNo + "')";
                                clsCommon.dbSql.ExecuteNonQuery(sql);
                            }
                        }
                        else  //表示记录修改
                        {
                            string sql2 = "update dbo.R_STEP_T set  Step_No=" + stepNo + " , Station_Name='" + stationName + "' ,Step_Category=" + stepCategory + " ,SleeveNo=" + sleeveNo + " ,Shelf_No=" + shelfNo + " ,Number=" + number + " ,Gun_No=" + gunNo + " ,Program_No=" + programNo + " ,Feacode='" + Feacode + "' ,ReworkTimes=" + reworkTimes + " ,Material_No=" + materialNo + " ,Material_Name='" + materialName + "' ,Scrow_No=" + scrowNo + " ,Scrow_Name='" + scrowName + "' ,Rework_No=" + reworkNo + " ,Rework_Name='" + reworkName + "' ,Production_No=" + productionNo + " ,Production_Name='" + productionName + "' ,Station_No=" + stationNo+",Module_No='"+moduleNo + "' where Step_ID=" + stepconfigID;
                            clsCommon.dbSql.ExecuteNonQuery(sql2);
                        }

                    }
                    MessageBox.Show("保存成功！");
                    #endregion
                }
                else
                {
                    #region 绑定数据源的情况下
                    for (int i = 0; i < tb2.Rows.Count; i++)
                    {
                        int stepNo = Convert.ToInt32(tb2.Rows[i]["Step_No"].ToString());
                        string stationName = tb2.Rows[i]["Station_Name"].ToString();
                        int stepCategory = Convert.ToInt32(tb2.Rows[i]["Step_Category"].ToString());
                        int sleeveNo = Convert.ToInt32(tb2.Rows[i]["SleeveNo"].ToString());
                        int shelfNo = Convert.ToInt32(tb2.Rows[i]["Shelf_No"].ToString());
                        int number = Convert.ToInt32(tb2.Rows[i]["Number"].ToString());
                        int gunNo = Convert.ToInt32(tb2.Rows[i]["Gun_No"].ToString());
                        int programNo = Convert.ToInt32(tb2.Rows[i]["Program_No"].ToString());
                        string Feacode = tb2.Rows[i]["Feacode"].ToString();
                        int reworkTimes = Convert.ToInt32(tb2.Rows[i]["ReworkTimes"].ToString());
                        int materialNo = Convert.ToInt32(tb2.Rows[i]["Material_No"].ToString());
                        string materialName = tb2.Rows[i]["Material_Name"].ToString();
                        int scrowNo = Convert.ToInt32(tb2.Rows[i]["Scrow_No"].ToString());
                        string scrowName = tb2.Rows[i]["Scrow_Name"].ToString();
                        int reworkNo = Convert.ToInt32(tb2.Rows[i]["Rework_No"].ToString());
                        string reworkName = tb2.Rows[i]["Rework_Name"].ToString();
                        int productionNo = Convert.ToInt32(tb2.Rows[i]["Production_No"].ToString());
                        string productionName = tb2.Rows[i]["Production_Name"].ToString();
                        int stationNo = Convert.ToInt32(tb2.Rows[i]["Station_No"].ToString());
                        int stepconfigID = Convert.ToInt32(tb2.Rows[i]["Step_ID"].ToString());
                        string moduleNo = tb2.Rows[i]["Module_No"].ToString();
                        if (stepconfigID == 0) // 表示新增记录
                        {
                            string sql1 = "select Step_ID from dbo.R_STEP_T where Step_No=" + stepNo + "  and Station_Name='" + stationName + "' and Step_Category=" + stepCategory + "  and SleeveNo=" + sleeveNo + " and Shelf_No=" + shelfNo + " and Number=" + number + " and Gun_No=" + gunNo + " and Program_No=" + programNo + " and Feacode='" + Feacode + "' and ReworkTimes=" + reworkTimes + " and Material_No=" + materialNo + " and Material_Name='" + materialName + "' and Scrow_No=" + scrowNo + " and Scrow_Name='" + scrowName + "' and Rework_No=" + reworkNo + " and Rework_Name='" + reworkName + "' and Production_No=" + productionNo + " and Production_Name='" + productionName + "' and Station_No=" + stationNo+" and Module_No='"+moduleNo + "' and DeleteFlag='0';";
                            DataTable dt = new DataTable();
                            dt = clsCommon.dbSql.ExecuteDataTable(sql1);
                            if (dt.Rows.Count > 0)
                            {
                                MessageBox.Show("该配置已存在！");
                            }
                            else
                            {
                                string sql = "insert dbo.R_STEP_T(Step_No,Station_Name,Step_Category,SleeveNo,Shelf_No,Number,Gun_No,Program_No,Feacode,ReworkTimes,Material_No,Material_Name,Scrow_No,Scrow_Name,Rework_No,Rework_Name,Production_No,Production_Name,CreateTime,DeleteFlag,Station_No,Module_No)values(" +
                                +stepNo + ",'" + stationName + "'," + stepCategory + "," + sleeveNo + "," + shelfNo + "," + number + "," + gunNo + "," + programNo + ",'" + Feacode + "'," + reworkTimes + "," + materialNo + ",'" + materialName + "'," + scrowNo + ",'" + scrowName + "'," + reworkNo + ",'" + reworkName + "'," + productionNo + ",'" + productionName + "',GETDATE(),'0'," + stationNo+",'"+moduleNo + "')";
                                clsCommon.dbSql.ExecuteNonQuery(sql);
                            }
                        }
                        else  //表示记录修改
                        {
                            string sql2 = "update dbo.R_STEP_T set  Step_No=" + stepNo + " , Station_Name='" + stationName + "' ,Step_Category=" + stepCategory + " ,SleeveNo=" + sleeveNo + " ,Shelf_No=" + shelfNo + " ,Number=" + number + " ,Gun_No=" + gunNo + " ,Program_No=" + programNo + " ,Feacode='" + Feacode + "' ,ReworkTimes=" + reworkTimes + " ,Material_No=" + materialNo + " ,Material_Name='" + materialName + "' ,Scrow_No=" + scrowNo + " ,Scrow_Name='" + scrowName + "' ,Rework_No=" + reworkNo + " ,Rework_Name='" + reworkName + "' ,Production_No=" + productionNo + " ,Production_Name='" + productionName + "' ,Station_No=" + stationNo+",Module_No='"+moduleNo + "' where Step_ID=" + stepconfigID;
                            clsCommon.dbSql.ExecuteNonQuery(sql2);
                        }

                    }
                    MessageBox.Show("保存成功！");
                    #endregion
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("数据保存出错！");
            }
        }
        /// <summary>
        /// 检查步序的连续性
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public bool CheckStepNo(int[] array)
        {
            Array.Sort(array);  //将数组中的元素从大到小排列
            //  Array.Reverse(array); //将数组中的元素进行翻转
            if (array.Length == 1 && array[0] == 1)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (i == array.Length - 1)
                    {
                        return true;
                    }
                    else
                    {
                        if (array[i] + 1 != array[i + 1] || array[0] != 1)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

        }

        /// <summary>
        /// 双击选中行 填充到每个配置控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView2_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (dataGridView2.DataSource == null)
            {
                #region 没有绑定数据源的情况下
                int row = dataGridView2.SelectedRows[0].Index;
                changeRow = row;//赋值给全局变量 方便修改这一行的记录
                CB_ProductionType.SelectedValue = Convert.ToInt32(dataGridView2.Rows[row].Cells[16].Value);
                CB_ReworkCategory.SelectedValue = Convert.ToInt32(dataGridView2.Rows[row].Cells[14].Value);
                CB_StationNo.SelectedValue = Convert.ToInt32(dataGridView2.Rows[row].Cells[18].Value);
                NUM_StepNo.Value = Convert.ToInt32(dataGridView2.Rows[row].Cells[0].Value);
                CB_SCategory.SelectedIndex = Convert.ToInt32(dataGridView2.Rows[row].Cells[1].Value) - 1;
                TB_SleeveNo.SelectedIndex = Convert.ToInt32(dataGridView2.Rows[row].Cells[2].Value) - 1;
                CB_MaterialShelfNo.SelectedIndex = Convert.ToInt32(dataGridView2.Rows[row].Cells[3].Value) - 1;
                TB_MaterialNum.Value = Convert.ToInt32(dataGridView2.Rows[row].Cells[4].Value);
                CB_GunNo.SelectedIndex = Convert.ToInt32(dataGridView2.Rows[row].Cells[5].Value) - 1;
                TB_ProgramNo.Text = dataGridView2.Rows[row].Cells[6].Value.ToString();
                TB_FeatureCode.Text = dataGridView2.Rows[row].Cells[7].Value.ToString();
                TB_ReworkTimes.Value = Convert.ToInt32(dataGridView2.Rows[row].Cells[8].Value);
                CB_StationMaterial.SelectedValue = Convert.ToInt32(dataGridView2.Rows[row].Cells[10].Value);
                CB_StationScrow.SelectedValue = Convert.ToInt32(dataGridView2.Rows[row].Cells[12].Value);
                TB_Module.Text = dataGridView2.Rows[row].Cells[20].Value.ToString();
                stepId = Convert.ToInt32(dataGridView2.Rows[row].Cells[19].Value);
                #endregion
            }
            else
            {
                #region 绑定数据源的情况下
                int row = dataGridView2.SelectedRows[0].Index;
                changeRow = row;//赋值给全局变量 方便修改这一行的记录
                CB_ProductionType.SelectedValue = Convert.ToInt32(tb2.Rows[row]["Production_No"].ToString());
                CB_ReworkCategory.SelectedValue = Convert.ToInt32(tb2.Rows[row]["Rework_No"].ToString());
                CB_StationNo.SelectedValue = Convert.ToInt32(tb2.Rows[row]["Station_No"].ToString());
                NUM_StepNo.Value = Convert.ToInt32(tb2.Rows[row]["Step_No"].ToString());
                CB_SCategory.SelectedIndex = Convert.ToInt32(tb2.Rows[row]["Step_Category"].ToString()) - 1;
                TB_SleeveNo.SelectedIndex = Convert.ToInt32(tb2.Rows[row]["SleeveNo"].ToString()) - 1;
                CB_MaterialShelfNo.SelectedIndex = Convert.ToInt32(tb2.Rows[row]["Shelf_No"].ToString()) - 1;
                TB_MaterialNum.Value = Convert.ToInt32(tb2.Rows[row]["Number"].ToString());
                CB_GunNo.SelectedIndex = Convert.ToInt32(tb2.Rows[row]["Gun_No"].ToString()) - 1;
                TB_ProgramNo.Text = tb2.Rows[row]["Program_No"].ToString();
                TB_FeatureCode.Text = tb2.Rows[row]["Feacode"].ToString();
                TB_ReworkTimes.Value = Convert.ToInt32(tb2.Rows[row]["ReworkTimes"].ToString());
                CB_StationMaterial.SelectedValue = Convert.ToInt32(tb2.Rows[row]["Material_No"].ToString());
                CB_StationScrow.SelectedValue = Convert.ToInt32(tb2.Rows[row]["Scrow_No"].ToString());
                TB_Module.Text = tb2.Rows[row]["Module_No"].ToString();
                stepId = Convert.ToInt32(tb2.Rows[row]["Step_ID"].ToString());
                #endregion
            }

        }
        /// <summary>
        /// 绑定数据源
        /// </summary>
        public void DataBind()
        {
            string sql = "select Step_ID,Step_No,Station_Name,Step_Category,SleeveNo,Shelf_No,Number,Gun_No,Program_No,Feacode,ReworkTimes,Material_No,Material_Name,Scrow_No,Scrow_Name,Rework_No,Rework_Name,Production_No,Production_Name,Station_No,Module_No  from dbo.R_STEP_T where DeleteFlag='0' and Production_No=" + CB_ProductionType.SelectedValue + " and Rework_No=" + CB_ReworkCategory.SelectedValue + ";";
            DataTable dt = new DataTable();
            dt = clsCommon.dbSql.ExecuteDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                dataGridView3.DataSource = dt;
                dataGridView2.Visible = false;
                dataGridView3.Visible = true;
                dataGridView3.Rows[0].Selected = false;
                foreach (DataGridViewColumn c in dataGridView3.Columns)
                    c.SortMode = DataGridViewColumnSortMode.Automatic;
            }
            else
            {
                dataGridView2.Visible = true;
                dataGridView3.Visible = false;
                dataGridView2.Rows.Clear();
            }
        }
        /// <summary>
        /// 加载完成后返修类型的改变 同时改变结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Shown(object sender, EventArgs e)
        {
            databindData = 1;
        }
        /// <summary>
        /// 返工类型改变 引发查询结果的改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CB_ReworkCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (databindData == 1)
            {
                DataBind();
                production_No = Convert.ToInt32(CB_ProductionType.SelectedValue.ToString());
                rework_No = Convert.ToInt32(CB_ReworkCategory.SelectedValue.ToString());
            }
        }
        /// <summary>
        /// 整理步序，将步序从小到大排列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 步序整理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView2.DataSource == null)
            {
                for (int i = 0; i < dataGridView2.Rows.Count; i++)
                {
                    dataGridView2.Rows[i].Cells[0].Value = i + 1;
                }
            }
            else
            {
                for (int i = 0; i < tb2.Rows.Count; i++)
                {
                    tb2.Rows[i]["Step_No"] = i + 1;
                }
            }

        }
        /// <summary>
        /// 将选中的一行上移
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (dataGridView2.Visible = true && dataGridView3.Visible == false)
            {
                #region 没有绑定数据源的情况下 交换两行数据
                if (dataGridView2.DataSource == null)
                {
                    int row = dataGridView2.SelectedRows[0].Index;
                    if (row <= 0) //选中的是第一行
                    {
                        MessageBox.Show("已是第一行，无法上移！");
                    }
                    else
                    {
                        int stepNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[0].Value);
                        string stationName = this.dataGridView2.Rows[row].Cells[9].Value.ToString();
                        int stepCategory = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[1].Value);
                        int sleeveNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[2].Value);
                        int shelfNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[3].Value);
                        int number = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[4].Value);
                        int gunNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[5].Value);
                        int programNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[6].Value);
                        string Feacode = this.dataGridView2.Rows[row].Cells[7].Value.ToString();
                        int reworkTimes = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[8].Value);
                        int materialNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[10].Value);
                        string materialName = this.dataGridView2.Rows[row].Cells[11].Value.ToString();
                        int scrowNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[12].Value);
                        string scrowName = this.dataGridView2.Rows[row].Cells[13].Value.ToString();
                        int reworkNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[14].Value);
                        string reworkName = this.dataGridView2.Rows[row].Cells[15].Value.ToString();
                        int productionNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[16].Value);
                        string productionName = this.dataGridView2.Rows[row].Cells[17].Value.ToString();
                        int stationNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[18].Value);
                        int stepconfigID = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[19].Value);
                        string moduleNo = this.dataGridView2.Rows[row].Cells[20].Value.ToString();

                        this.dataGridView2.Rows[row].Cells[0].Value = this.dataGridView2.Rows[row - 1].Cells[0].Value;
                        this.dataGridView2.Rows[row].Cells[9].Value = this.dataGridView2.Rows[row - 1].Cells[9].Value;
                        this.dataGridView2.Rows[row].Cells[1].Value = this.dataGridView2.Rows[row - 1].Cells[1].Value;
                        this.dataGridView2.Rows[row].Cells[2].Value = this.dataGridView2.Rows[row - 1].Cells[2].Value;
                        this.dataGridView2.Rows[row].Cells[3].Value = this.dataGridView2.Rows[row - 1].Cells[3].Value;
                        this.dataGridView2.Rows[row].Cells[4].Value = this.dataGridView2.Rows[row - 1].Cells[4].Value;
                        this.dataGridView2.Rows[row].Cells[5].Value = this.dataGridView2.Rows[row - 1].Cells[5].Value;
                        this.dataGridView2.Rows[row].Cells[6].Value = this.dataGridView2.Rows[row - 1].Cells[6].Value;
                        this.dataGridView2.Rows[row].Cells[7].Value = this.dataGridView2.Rows[row - 1].Cells[7].Value;
                        this.dataGridView2.Rows[row].Cells[8].Value = this.dataGridView2.Rows[row - 1].Cells[8].Value;
                        this.dataGridView2.Rows[row].Cells[10].Value = this.dataGridView2.Rows[row - 1].Cells[10].Value;
                        this.dataGridView2.Rows[row].Cells[11].Value = this.dataGridView2.Rows[row - 1].Cells[11].Value;
                        this.dataGridView2.Rows[row].Cells[12].Value = this.dataGridView2.Rows[row - 1].Cells[12].Value;
                        this.dataGridView2.Rows[row].Cells[13].Value = this.dataGridView2.Rows[row - 1].Cells[13].Value;
                        this.dataGridView2.Rows[row].Cells[14].Value = this.dataGridView2.Rows[row - 1].Cells[14].Value;
                        this.dataGridView2.Rows[row].Cells[15].Value = this.dataGridView2.Rows[row - 1].Cells[15].Value;
                        this.dataGridView2.Rows[row].Cells[16].Value = this.dataGridView2.Rows[row - 1].Cells[16].Value;
                        this.dataGridView2.Rows[row].Cells[17].Value = this.dataGridView2.Rows[row - 1].Cells[17].Value;
                        this.dataGridView2.Rows[row].Cells[18].Value = this.dataGridView2.Rows[row - 1].Cells[18].Value;
                        this.dataGridView2.Rows[row].Cells[19].Value = this.dataGridView2.Rows[row - 1].Cells[19].Value;
                        this.dataGridView2.Rows[row].Cells[20].Value = this.dataGridView2.Rows[row - 1].Cells[20].Value;

                        this.dataGridView2.Rows[row - 1].Cells[0].Value = stepNo;
                        this.dataGridView2.Rows[row - 1].Cells[9].Value = stationName;
                        this.dataGridView2.Rows[row - 1].Cells[1].Value = stepCategory;
                        this.dataGridView2.Rows[row - 1].Cells[2].Value = sleeveNo;
                        this.dataGridView2.Rows[row - 1].Cells[3].Value = shelfNo;
                        this.dataGridView2.Rows[row - 1].Cells[4].Value = number;
                        this.dataGridView2.Rows[row - 1].Cells[5].Value = gunNo;
                        this.dataGridView2.Rows[row - 1].Cells[6].Value = programNo;
                        this.dataGridView2.Rows[row - 1].Cells[7].Value = Feacode;
                        this.dataGridView2.Rows[row - 1].Cells[8].Value = reworkTimes;
                        this.dataGridView2.Rows[row - 1].Cells[10].Value = materialNo;
                        this.dataGridView2.Rows[row - 1].Cells[11].Value = materialName;
                        this.dataGridView2.Rows[row - 1].Cells[12].Value = scrowNo;
                        this.dataGridView2.Rows[row - 1].Cells[13].Value = scrowName;
                        this.dataGridView2.Rows[row - 1].Cells[14].Value = reworkNo;
                        this.dataGridView2.Rows[row - 1].Cells[15].Value = reworkName;
                        this.dataGridView2.Rows[row - 1].Cells[16].Value = productionNo;
                        this.dataGridView2.Rows[row - 1].Cells[17].Value = productionName;
                        this.dataGridView2.Rows[row - 1].Cells[18].Value = stationNo;
                        this.dataGridView2.Rows[row - 1].Cells[19].Value = stepconfigID;
                        this.dataGridView2.Rows[row - 1].Cells[20].Value = moduleNo;

                        this.dataGridView2.Rows[row - 1].Selected = true;
                        this.dataGridView2.Rows[row].Selected = false;

                    }
                #endregion
                }
                else
                {
                    #region 上移具体操作
                    if (this.dataGridView2.CurrentRow == null)
                    {

                        MessageBox.Show("请选择要需要操作的步序所在行");

                    }
                    else
                    {
                        if (this.dataGridView2.CurrentRow.Index <= 0)
                        {

                            MessageBox.Show("此步序已在顶端，不能再上移！");

                        }

                        else
                        {

                            int nowIndex = this.dataGridView2.CurrentRow.Index;

                            object[] _rowData = (this.dataGridView2.DataSource as DataTable).Rows[nowIndex].ItemArray;

                            (this.dataGridView2.DataSource as DataTable).Rows[nowIndex].ItemArray = (this.dataGridView2.DataSource as DataTable).Rows[nowIndex - 1].ItemArray;

                            (this.dataGridView2.DataSource as DataTable).Rows[nowIndex - 1].ItemArray = _rowData;

                            this.dataGridView2.CurrentCell = this.dataGridView2.Rows[nowIndex - 1].Cells[0];//设定当前行

                        }
                    }
                    #endregion
                }
            }
            else
            {
                MessageBox.Show("请先点击编辑图标，然后再上移！");
            }

        }
        /// <summary>
        /// 将选中的一行下移
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (dataGridView2.Visible = true && dataGridView3.Visible == false)
            {
                if (dataGridView2.DataSource == null)
                {
                    #region 没有绑定数据源的情况下 交换两行数据
                    int row = dataGridView2.SelectedRows[0].Index;
                    if (row >= this.dataGridView2.Rows.Count - 1) //选中的是最后一行
                    {
                        MessageBox.Show("已是最后一行，无法上移！");
                    }
                    else
                    {
                        int stepNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[0].Value);
                        string stationName = this.dataGridView2.Rows[row].Cells[9].Value.ToString();
                        int stepCategory = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[1].Value);
                        int sleeveNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[2].Value);
                        int shelfNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[3].Value);
                        int number = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[4].Value);
                        int gunNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[5].Value);
                        int programNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[6].Value);
                        string Feacode = this.dataGridView2.Rows[row].Cells[7].Value.ToString();
                        int reworkTimes = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[8].Value);
                        int materialNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[10].Value);
                        string materialName = this.dataGridView2.Rows[row].Cells[11].Value.ToString();
                        int scrowNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[12].Value);
                        string scrowName = this.dataGridView2.Rows[row].Cells[13].Value.ToString();
                        int reworkNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[14].Value);
                        string reworkName = this.dataGridView2.Rows[row].Cells[15].Value.ToString();
                        int productionNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[16].Value);
                        string productionName = this.dataGridView2.Rows[row].Cells[17].Value.ToString();
                        int stationNo = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[18].Value);
                        int stepconfigID = Convert.ToInt32(this.dataGridView2.Rows[row].Cells[19].Value);
                        string moduleNo = this.dataGridView2.Rows[row].Cells[20].Value.ToString();

                        this.dataGridView2.Rows[row].Cells[0].Value = this.dataGridView2.Rows[row + 1].Cells[0].Value;
                        this.dataGridView2.Rows[row].Cells[9].Value = this.dataGridView2.Rows[row + 1].Cells[9].Value;
                        this.dataGridView2.Rows[row].Cells[1].Value = this.dataGridView2.Rows[row + 1].Cells[1].Value;
                        this.dataGridView2.Rows[row].Cells[2].Value = this.dataGridView2.Rows[row + 1].Cells[2].Value;
                        this.dataGridView2.Rows[row].Cells[3].Value = this.dataGridView2.Rows[row + 1].Cells[3].Value;
                        this.dataGridView2.Rows[row].Cells[4].Value = this.dataGridView2.Rows[row + 1].Cells[4].Value;
                        this.dataGridView2.Rows[row].Cells[5].Value = this.dataGridView2.Rows[row + 1].Cells[5].Value;
                        this.dataGridView2.Rows[row].Cells[6].Value = this.dataGridView2.Rows[row + 1].Cells[6].Value;
                        this.dataGridView2.Rows[row].Cells[7].Value = this.dataGridView2.Rows[row + 1].Cells[7].Value;
                        this.dataGridView2.Rows[row].Cells[8].Value = this.dataGridView2.Rows[row + 1].Cells[8].Value;
                        this.dataGridView2.Rows[row].Cells[10].Value = this.dataGridView2.Rows[row + 1].Cells[10].Value;
                        this.dataGridView2.Rows[row].Cells[11].Value = this.dataGridView2.Rows[row + 1].Cells[11].Value;
                        this.dataGridView2.Rows[row].Cells[12].Value = this.dataGridView2.Rows[row + 1].Cells[12].Value;
                        this.dataGridView2.Rows[row].Cells[13].Value = this.dataGridView2.Rows[row + 1].Cells[13].Value;
                        this.dataGridView2.Rows[row].Cells[14].Value = this.dataGridView2.Rows[row + 1].Cells[14].Value;
                        this.dataGridView2.Rows[row].Cells[15].Value = this.dataGridView2.Rows[row + 1].Cells[15].Value;
                        this.dataGridView2.Rows[row].Cells[16].Value = this.dataGridView2.Rows[row + 1].Cells[16].Value;
                        this.dataGridView2.Rows[row].Cells[17].Value = this.dataGridView2.Rows[row + 1].Cells[17].Value;
                        this.dataGridView2.Rows[row].Cells[18].Value = this.dataGridView2.Rows[row + 1].Cells[18].Value;
                        this.dataGridView2.Rows[row].Cells[19].Value = this.dataGridView2.Rows[row + 1].Cells[19].Value;
                        this.dataGridView2.Rows[row].Cells[20].Value = this.dataGridView2.Rows[row + 1].Cells[20].Value;

                        this.dataGridView2.Rows[row + 1].Cells[0].Value = stepNo;
                        this.dataGridView2.Rows[row + 1].Cells[9].Value = stationName;
                        this.dataGridView2.Rows[row + 1].Cells[1].Value = stepCategory;
                        this.dataGridView2.Rows[row + 1].Cells[2].Value = sleeveNo;
                        this.dataGridView2.Rows[row + 1].Cells[3].Value = shelfNo;
                        this.dataGridView2.Rows[row + 1].Cells[4].Value = number;
                        this.dataGridView2.Rows[row + 1].Cells[5].Value = gunNo;
                        this.dataGridView2.Rows[row + 1].Cells[6].Value = programNo;
                        this.dataGridView2.Rows[row + 1].Cells[7].Value = Feacode;
                        this.dataGridView2.Rows[row + 1].Cells[8].Value = reworkTimes;
                        this.dataGridView2.Rows[row + 1].Cells[10].Value = materialNo;
                        this.dataGridView2.Rows[row + 1].Cells[11].Value = materialName;
                        this.dataGridView2.Rows[row + 1].Cells[12].Value = scrowNo;
                        this.dataGridView2.Rows[row + 1].Cells[13].Value = scrowName;
                        this.dataGridView2.Rows[row + 1].Cells[14].Value = reworkNo;
                        this.dataGridView2.Rows[row + 1].Cells[15].Value = reworkName;
                        this.dataGridView2.Rows[row + 1].Cells[16].Value = productionNo;
                        this.dataGridView2.Rows[row + 1].Cells[17].Value = productionName;
                        this.dataGridView2.Rows[row + 1].Cells[18].Value = stationNo;
                        this.dataGridView2.Rows[row + 1].Cells[19].Value = stepconfigID;
                        this.dataGridView2.Rows[row + 1].Cells[20].Value = moduleNo;

                        this.dataGridView2.Rows[row + 1].Selected = true;
                        this.dataGridView2.Rows[row].Selected = false;
                        int a = this.dataGridView2.CurrentRow.Index;
                    }
                    #endregion
                }
                else
                {
                    #region 绑定数据源的情况下
                    if (this.dataGridView2.CurrentRow == null)
                    {

                        MessageBox.Show("请选择要需要操作的步序所在行");

                    }
                    else
                    {
                        if (this.dataGridView2.CurrentRow.Index >= this.dataGridView2.Rows.Count - 1)
                        {

                            MessageBox.Show("此步序已在底端，不能再下移！");

                        }

                        else
                        {

                            int nowIndex = this.dataGridView2.CurrentRow.Index;

                            object[] _rowData = (this.dataGridView2.DataSource as DataTable).Rows[nowIndex].ItemArray;

                            (this.dataGridView2.DataSource as DataTable).Rows[nowIndex].ItemArray = (this.dataGridView2.DataSource as DataTable).Rows[nowIndex + 1].ItemArray;

                            (this.dataGridView2.DataSource as DataTable).Rows[nowIndex + 1].ItemArray = _rowData;

                            this.dataGridView2.CurrentCell = this.dataGridView2.Rows[nowIndex + 1].Cells[0];//设定当前行

                        }
                    }
                    #endregion
                }
            }
            else
            {
                MessageBox.Show("请先点击编辑图标，然后再下移！");
            }

        }
        /// <summary>
        /// 编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox5_Click(object sender, EventArgs e)
        {
            //绑定表格
            DataTable tb1 = (DataTable)dataGridView3.DataSource;
            tb2 = tb1.Copy();
            this.dataGridView2.DataSource = tb2;
            this.dataGridView2.ClearSelection();
            dataGridView2.Visible = true;
            dataGridView3.Visible = false;
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox8_Click(object sender, EventArgs e)
        {
            if (dataGridView2.Visible = true && dataGridView3.Visible == false)
            {
                if (dataGridView2.DataSource == null) //配置没有保存的时候删除
                {
                    #region 没有保存数据库的情况
                    if (MessageBox.Show("此配置删除后不可恢复，确定要删除?", "安全提示",
                               System.Windows.Forms.MessageBoxButtons.YesNo,
                               System.Windows.Forms.MessageBoxIcon.Warning)
                       == System.Windows.Forms.DialogResult.Yes)
                    {
                        int row = dataGridView2.SelectedRows[0].Index;
                        this.dataGridView2.Rows.RemoveAt(row);
                    }
                    #endregion
                }
                else
                {
                    #region 保存了数据库后编辑
                    int row = dataGridView2.SelectedRows[0].Index;
                    if (Convert.ToInt32(tb2.Rows[row]["Step_ID"].ToString()) != 0)
                    {
                        if (MessageBox.Show("此配置删除后不可恢复，确定要删除?", "安全提示",
                                System.Windows.Forms.MessageBoxButtons.YesNo,
                                System.Windows.Forms.MessageBoxIcon.Warning)
                        == System.Windows.Forms.DialogResult.Yes)
                        {
                            string sql = "update dbo.R_STEP_T set DeleteFlag='1' where Step_ID=" + Convert.ToInt32(tb2.Rows[row]["Step_ID"].ToString()) + ";";
                            clsCommon.dbSql.ExecuteDataTable(sql);
                            tb2.Rows.RemoveAt(row);
                            this.dataGridView2.Refresh();
                            //if (tb2.Rows.Count == 0)
                            //{
                            //    this.dataGridView2.DataSource = null;
                            //}
                            //  this.dataGridView2.Rows.RemoveAt(row);
                        }
                    }
                    else
                    {
                        if (MessageBox.Show("此配置删除后不可恢复，确定要删除?", "安全提示",
                               System.Windows.Forms.MessageBoxButtons.YesNo,
                               System.Windows.Forms.MessageBoxIcon.Warning)
                       == System.Windows.Forms.DialogResult.Yes)
                        {
                            tb2.Rows.RemoveAt(row);
                            this.dataGridView2.Refresh();
                        }
                    }
                    #endregion
                }
            }
            else
            {
                MessageBox.Show("请先点击编辑图标，然后再删除！");
            }

        }
    }
}
