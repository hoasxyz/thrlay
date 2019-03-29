using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using System.IO;
using System.Data.OleDb;
using Microsoft.Office.Interop.Excel;

namespace Thrlay
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = "20";
            textBox2.Text = "60";
            textBox3.Text = "40";
            textBox4.Text = "0";
            textBox5.Text = "2";
            textBox6.Text = "0";
            textBox7.Text = "0.16667";
            textBox8.Text = "0.8";
            toolStripStatusLabel1.Text = System.Environment.UserName.ToString()+ "你好！ | " + DateTime.Now.ToLongDateString() + " | ";
            groupBox2.Text = "计算表格";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog input = new OpenFileDialog();
            input.InitialDirectory = "E:\\!大学\\大三下\\C#\\作业\\Thrlay";
            input.Filter = "Excel(*.xlsx)|*.xlsx|Excel(*.xls)|*.xls";
            string strPath;//文件完整的路径名
            if (input.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    strPath = input.FileName;
                    string strCon = "provider=Microsoft.ACE.OLEDB.12.0;data source=" + strPath + ";extended properties=excel 8.0";//关键是红色区域
                    OleDbConnection Con = new OleDbConnection(strCon);//建立连接
                    string strSql = "select * from [Sheet1$]";//表名的写法也应注意不同，对应的excel表为sheet1，在这里要在其后加美元符号$，并用中括号
                    OleDbCommand Cmd = new OleDbCommand(strSql, Con);//建立要执行的命令
                    OleDbDataAdapter da = new OleDbDataAdapter(Cmd);//建立数据适配器
                    DataSet ds = new DataSet();//新建数据集
                    da.Fill(ds, "thrlay");//把数据适配器中的数据读到数据集中的一个表中（此处表名为shyman，可以任取表名）
                                          //指定datagridview1的数据源为数据集ds的第一张表（也就是shyman表），也可以写ds.Table["shyman"]
                    dataGridView1.DataSource = ds.Tables[0];
                    //label8.Text = dataGridView1.Rows[17 + 1].Cells[1].Value.ToString();
                    //label9.Text = this.dataGridView1.RowCount.ToString();
                    if (!(dataGridView1.Columns[0].Name.Equals("日期") &&
                        dataGridView1.Columns[1].Name.Equals("P") &&
                        dataGridView1.Columns[2].Name.Equals("EP")))
                    {
                        MessageBox.Show("Excel文件内容格式不对！");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);//捕捉异常
                }
            }
        }

        string type;
        private void button2_Click(object sender, EventArgs e)
        {
            if ((string.IsNullOrWhiteSpace(textBox4.Text.ToString()) || string.IsNullOrWhiteSpace(textBox1.Text.ToString())))
            {
                MessageBox.Show("请输入必要的参数！", "错误提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            double WUM = Convert.ToDouble(textBox1.Text.ToString());
            double FE  = Convert.ToDouble(textBox8.Text.ToString());
            double C   = Convert.ToDouble(textBox7.Text.ToString());

            int rlen = Convert.ToInt32(dataGridView1.RowCount.ToString());

            double[] P = new double[rlen - 1];
            double[] EP = new double[rlen - 1];
            for (int i = 0; i <= (rlen - 2); i++)
            {
                P[i] = Convert.ToDouble(dataGridView1.Rows[i].Cells[1].Value.ToString());
                EP[i] = Convert.ToDouble(dataGridView1.Rows[i].Cells[2].Value.ToString());
            }
            //计算
            #region 三层蒸发 
            if (!string.IsNullOrWhiteSpace(textBox2.Text.ToString()) && !string.IsNullOrWhiteSpace(textBox3.Text.ToString()))
            {
                groupBox2.Text = ("您计算的是三层蒸发模式");
                type = "三层蒸发";
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                double WLM = Convert.ToDouble(textBox2.Text.ToString());
                double WDM = Convert.ToDouble(textBox3.Text.ToString());
                double[] EU = new double[rlen - 1];
                double[] EL = new double[rlen - 1];
                double[] ED = new double[rlen - 1];
                double[] E  = new double[rlen - 1];
                double[] WU = new double[rlen - 1];
                double[] WL = new double[rlen - 1];
                double[] WD = new double[rlen - 1];

                WU[0] = WUM * FE;
                WL[0] = WLM * FE;
                WD[0] = WDM * FE;

                //datagridview把第二行当作row的[1]...
                EU[0] = Convert.ToDouble(textBox4.Text.ToString());
                EL[0] = Convert.ToDouble(textBox5.Text.ToString());
                ED[0] = Convert.ToDouble(textBox6.Text.ToString());
                E [0] = EU[0] + EL[0] + ED[0];
                
                for (int i = 1; i <= rlen - 2; i++)
                {
                    WL[i] = WL[i - 1] - EL[i - 1];
                    WD[i] = WD[i - 1] - ED[i - 1];
                    WU[i] = WU[i - 1] + P[i - 1] - EU[i - 1];
                    if (WU[i] + P[i] < EP[i])
                    {
                        EU[i] = WU[i] + P[i];
                    }
                    if (WU[i] + P[i] >= EP[i])
                    {
                        EU[i] = EP[i];
                        EL[i] = 0;
                        ED[i] = 0;
                    }
                    else if ((WU[i] + P[i] < EP[i]) && (WL[i] >= C * WLM))
                    {
                        EU[i] = WU[i] + P[i];
                        EL[i] = (EP[i] - EU[i]) * WL[i] / WLM;
                        ED[i] = 0;
                    }
                    else if ((WU[i] + P[i] < EP[i]) && (WL[i] < C * WLM) && (WL[i] >= C * (EP[i] - EU[i])))
                    {
                        EU[i] = WU[i] + P[i];
                        EL[i] = C * (EP[i] - EU[i]);
                        ED[i] = 0;
                    }
                    else
                    {
                        EU[i] = WU[i] + P[i];
                        EL[i] = WL[i];
                        ED[i] = C * (EP[i] - EU[i]) - EL[i];
                    }
                    E[i] = EU[i] + EL[i] + ED[i];
                }
                //把数据写进表中
                if(dataGridView1.Columns.Count <= 3)
                {
                    dataGridView1.Columns.Add("EU", "EU".ToString());
                    dataGridView1.Columns.Add("EL", "EL".ToString());
                    dataGridView1.Columns.Add("ED", "ED".ToString());
                    dataGridView1.Columns.Add("E", "E".ToString());
                    dataGridView1.Columns.Add("WU", "WU".ToString());
                    dataGridView1.Columns.Add("WL", "WL".ToString());
                    dataGridView1.Columns.Add("WD", "WD".ToString());
                }

                for (int i = 0; i <= rlen - 2; i++)
                {
                    dataGridView1.Rows[i].Cells[1].Value = P [i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[2].Value = EP[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[3].Value = EU[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[4].Value = EL[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[5].Value = ED[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[6].Value = E [i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[7].Value = WU[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[8].Value = WL[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[9].Value = WD[i].ToString("0.00");
                }
            }
            #endregion

            #region 二层蒸发
            else if(!string.IsNullOrWhiteSpace(textBox2.Text.ToString()) && string.IsNullOrWhiteSpace(textBox3.Text.ToString()))
            {
                groupBox2.Text = ("您计算的是二层蒸发模式");
                type = "二层蒸发";
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                double WLM = Convert.ToDouble(textBox2.Text.ToString());
                double[] EU = new double[rlen - 1];
                double[] EL = new double[rlen - 1];
                double[] E  = new double[rlen - 1];
                double[] WU = new double[rlen - 1];
                double[] WL = new double[rlen - 1];

                WU[0] = WUM * FE;
                WL[0] = WLM * FE;
                EU[0] = Convert.ToDouble(textBox4.Text.ToString());
                EL[0] = Convert.ToDouble(textBox5.Text.ToString());
                if (!string.IsNullOrWhiteSpace(textBox6.Text.ToString()))
                {
                    MessageBox.Show("二层蒸发模式中不含ED项！");
                    return;
                }
                E[0] = EU[0] + EL[0];
                for (int i = 1; i <= rlen - 2; i++)
                {
                    WL[i] = WL[i - 1] - EL[i - 1];
                    WU[i] = WU[i - 1] + P[i - 1] - EU[i - 1];
                    if (WU[i] + P[i] < EP[i])
                    {
                        EU[i] = WU[i] + P[i];
                    }
                    if (WU[i] + P[i] >= EP[i])
                    {
                        EU[i] = EP[i];
                        EL[i] = 0;
                    }
                    else
                    {
                        EU[i] = WU[i] + P[i];
                        EL[i] = (EP[i] - EU[i]) * WL[i] / WLM;
                    }
                    E[i] = EU[i] + EL[i];
                }
                //把数据写进表中
                if (dataGridView1.Columns.Count <= 3)
                {
                    dataGridView1.Columns.Add("EU", "EU".ToString());
                    dataGridView1.Columns.Add("EL", "EL".ToString());
                    dataGridView1.Columns.Add("E", "E".ToString());
                    dataGridView1.Columns.Add("WU", "WU".ToString());
                    dataGridView1.Columns.Add("WL", "WL".ToString());
                }

                for (int i = 0; i <= rlen - 2; i++)
                {
                    dataGridView1.Rows[i].Cells[1].Value = P [i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[2].Value = EP[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[3].Value = EU[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[4].Value = EL[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[5].Value = E [i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[6].Value = WU[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[7].Value = WL[i].ToString("0.00");
                }
            }
            #endregion

            #region 一层蒸发
            else if(string.IsNullOrWhiteSpace(textBox2.Text.ToString()) && string.IsNullOrWhiteSpace(textBox3.Text.ToString()))
            {
                groupBox2.Text = ("您计算的是一层蒸发模式");
                type = "一层蒸发";
                double WM = Convert.ToDouble(textBox1.Text.ToString());
                double[] E = new double[rlen - 1];
                double[] W = new double[rlen - 1];
                if(!string.IsNullOrWhiteSpace(textBox2.Text.ToString()) || !string.IsNullOrWhiteSpace(textBox3.Text.ToString()))
                {
                    MessageBox.Show("一层蒸发模式不含EL和ED！");
                    return;
                }
                E[0]  = Convert.ToDouble(textBox4.Text.ToString());
                W[0] = WM * FE;
                for (int i = 1; i <= rlen - 2; i++)
                {
                    W[i] = W[i - 1] + P[i - 1] - E[i - 1];
                    if (W[i] + P[i] < EP[i])
                    {
                        E[i] = W[i] + P[i];
                    }
                    else
                    {
                        E[i] = EP[i - 1] * W[i - 1] / WM;
                    }
                }
                if (dataGridView1.Columns.Count <= 3)
                {
                    dataGridView1.Columns.Add("E", "E".ToString());
                    dataGridView1.Columns.Add("W", "W".ToString());
                }
                for (int i = 0; i <= rlen - 2; i++)
                {
                    dataGridView1.Rows[i].Cells[1].Value = P[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[2].Value = EP[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[3].Value = E[i].ToString("0.00");
                    dataGridView1.Rows[i].Cells[4].Value = W[i].ToString("0.00");
                }
            }
            #endregion
            else
            {
                MessageBox.Show("参数有误！");
                return;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog kk = new SaveFileDialog();
            kk.Title = "保存Excel文件";
            kk.Filter = "Excel(*.xls)|*.xls";
            kk.FilterIndex = 1;
            if (kk.ShowDialog() == DialogResult.OK)
            {
                string FileName = kk.FileName;  // + ".xls"
                if (File.Exists(FileName))
                    File.Delete(FileName);
                FileStream objFileStream;
                StreamWriter objStreamWriter;
                string strLine = "";
                objFileStream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                objStreamWriter = new StreamWriter(objFileStream, System.Text.Encoding.Unicode);
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    if (dataGridView1.Columns[i].Visible == true)
                    {
                        strLine = strLine + dataGridView1.Columns[i].HeaderText.ToString() + Convert.ToChar(9);
                    }
                }
                objStreamWriter.WriteLine(strLine);
                strLine = "";
                
                for (int i = 0; i < dataGridView1.Rows.Count; i++)

                {
                    if (dataGridView1.Columns[0].Visible == true)
                    {
                        if (dataGridView1.Rows[i].Cells[0].Value == null)
                            strLine = strLine + " " + Convert.ToChar(9);
                        else
                            strLine = strLine + dataGridView1.Rows[i].Cells[0].Value.ToString() + Convert.ToChar(9);
                    }
                    for (int j = 1; j < dataGridView1.Columns.Count; j++)
                    {
                        if (dataGridView1.Columns[j].Visible == true)
                        {
                            if (dataGridView1.Rows[i].Cells[j].Value == null)
                                strLine = strLine + " " + Convert.ToChar(9);
                            else
                            {
                                string rowstr = "";
                                rowstr = dataGridView1.Rows[i].Cells[j].Value.ToString();
                                if (rowstr.IndexOf("\r\n") > 0)
                                    rowstr = rowstr.Replace("\r\n", " ");
                                if (rowstr.IndexOf("\t") > 0)
                                    rowstr = rowstr.Replace("\t", " ");
                                strLine = strLine + rowstr + Convert.ToChar(9);
                            }
                        }
                    }
                    objStreamWriter.WriteLine(strLine);
                    strLine = "";
                }
                objStreamWriter.Close();
                objFileStream.Close();
                MessageBox.Show(this, "保存Excel成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/hoasxyz/thrlay");
        }
    }
}
