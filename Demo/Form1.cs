using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Threading;
using System.Text.RegularExpressions;


namespace Demo
{
    public partial class Form1 : Form

    {
        /************************************************全局变量**********************************************************/
        ArrayList d数据格式 = new ArrayList();
        j节目config led大屏设置 = new j节目config();
        string send_sate = "";//LED大屏幕发送状态
        float[] data = new float[3];//最新数据 0为颗粒物，1为二氧化硫，2为氮氧化物
        float[] ShowData = new float[3];//此时展示的数据 0为颗粒物，1为二氧化硫，2为氮氧化物
        int ModbusSentSate = 0;//  0为将要索取烟尘，1为二氧化硫，2为氮氧化物 3为需要更新
        Byte[] Modbus1 = new Byte[8] { 0x01, 0x03, 0x00, 0x09, 0x00, 0x02, 0x14, 0x09};
        Byte[] Modbus2 = new Byte[8] { 0x01, 0x03, 0x00, 0x11, 0x00, 0x02, 0x94, 0x0E };
        Byte[] Modbus3 = new Byte[8] { 0x01, 0x03, 0x00, 0x19, 0x00, 0x02, 0x15, 0xCC };

        /*****************************************************构造*******************************************************/
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            load_para();
            draw_pic();
            led_send();
        }

        public void load_para()//载入参数
        {
            {
                ///  load colum
                string filepath1 = Application.ExecutablePath;
                filepath1 = filepath1.Substring(0, filepath1.LastIndexOf('\\'));
                string inipath = filepath1 + "\\" + "config.txt";
                FileStream fs = new FileStream(inipath, FileMode.Open);
                StreamReader sw = new StreamReader(fs, Encoding.Default);

                string str = "";//屏幕参数设置
                str = sw.ReadLine();
                string[] aa = str.Split(',');
                try
                {
                    led大屏设置.ip = aa[1];
                    led大屏设置.width = Convert.ToInt16(aa[2]);
                    led大屏设置.height = Convert.ToInt16(aa[3]);
                    led大屏设置.FontColor = Convert.ToInt16(aa[4]);

                }
                catch { MessageBox.Show("读取大屏幕设置出错"); }

                str = sw.ReadLine();//ModBus串口监听设置
                aa = str.Split(',');
                try
                {
                    serialPort1.PortName = aa[1];
                    serialPort1.Open();
                    serialPort1.BaudRate = 57600;

                }
                catch { MessageBox.Show("打开" + aa[0] + "设备端口出错"); }



                str = sw.ReadLine();//定时器设置
                aa = str.Split(',');
                try
                {
                    timer1.Interval = Convert.ToInt16(aa[1]) * 1000;
                    timer1.Enabled = true;
                }
                catch { MessageBox.Show("设置定时器出错"); }


                //读取数据格式 
                d数据格式.Clear(); //读取数据格式 
                str = sw.ReadLine();
                for (int k = 0; k < 3; k++)
                {
                    aa = str.Split(',');
                    try
                    {
                        j节目config tmp = new j节目config();
                        tmp.left = Convert.ToInt16(aa[1]);
                        tmp.top = Convert.ToInt16(aa[2]);
                        tmp.FontName = (aa[3]);
                        tmp.FontSize = Convert.ToInt16(aa[4]);
                        tmp.dat_min = Convert.ToInt16(aa[5]);
                        tmp.dat_max = Convert.ToInt16(aa[6]);
                        tmp.dat_point = Convert.ToInt16(aa[7]);

                        d数据格式.Add(tmp);
                        str = sw.ReadLine();
                    }
                    catch { MessageBox.Show("读取数据格式信息出错"); }
                   
                }

                sw.Close();
            }

            // init show

        }

        private void button10_Click(object sender, EventArgs e)//载入参数 按钮
        {
            try
            {
                serialPort1.Close();
            }
            catch { }
            load_para();
        }

        private void timer1_Tick(object sender, EventArgs e)//定时器
        {
          //  MessageBox.Show(Convert.ToString(ModbusSentSate));

                switch (ModbusSentSate)
            {
                case 0:
                    try
                    {
                        ModbusSentSate = 1;
                        serialPort1.Write(Modbus1, 0, 8);
                    }
                    catch {  }
                    break;

                case 1:
                    try
                    {
                        ModbusSentSate = 2;
                        serialPort1.Write(Modbus2, 0, 8);
                    }
                    catch {  }
                    break;

                case 2:
                    try
                    {
                        ModbusSentSate = 3;
                        serialPort1.Write(Modbus3, 0, 8);
                    }
                    catch { }
                    break;
                case 3:
                    try
                    {
                        ModbusSentSate = 0;

                        ShowData[0] = data[0];
                        ShowData[1] = data[1];
                        ShowData[2] = data[2];

                        draw_pic();
                        led_send();

                    }
                    catch {  }
                    break;

            }

        }

        /****************************************************************************************************************/


        /***************************************************画图*******************************************************/
        public void draw_pic()//画图
        {
            Bitmap my_bitmap = new Bitmap("ModbusBase.bmp");
            Graphics my_pic = Graphics.FromImage(my_bitmap);
            SizeF d = new SizeF();
            int line_width = 1;//画笔宽度
            Pen my_pen = new Pen(Brushes.Red, line_width);

            label5.Text = "颗粒物:" + Convert.ToString(ShowData[0]);
            label6.Text = "二氧化硫:" + Convert.ToString(ShowData[1]);
            label7.Text = "氮氧化物:" + Convert.ToString(ShowData[2]);


            for (int i = 0; i < 3; i++)
            {
                j节目config tmp = (j节目config)d数据格式[i];//从config找到第i个数据的格式

                string str_temp = ShowData[i].ToString("f" + tmp.dat_point.ToString());//数据标准化

                /*MeasureString方法，只要指定了字体和字符串后，用这个方法就能获得一个矩形的区域，
                 * 这个区域是Graphics对象用DrawString方法在空间表面绘制字符串时所要的区域。*/
                d = my_pic.MeasureString(str_temp, new Font(tmp.FontName, tmp.FontSize, FontStyle.Bold));

                draw_string(my_pic, str_temp, tmp.left, tmp.top, tmp.FontSize);
            }


            if (File.Exists("new.bmp")) //如果已经存在
            {
                File.Delete("new.bmp");
            }
            my_bitmap.Save("new.bmp");

            FileStream fs = new FileStream("new.bmp", FileMode.Open,FileAccess.Read);//获取图片文件流
            Image img = Image.FromStream(fs); // 文件流转换成Image格式
            pictureBox1.Image = img;   //给 图片框设置要显示的图片
            fs.Close(); // 关闭流，释放图片资源


            my_bitmap.Dispose();

        }

        private void button1_Click(object sender, EventArgs e)//画图 按钮
        {
            draw_pic();
        }

        public void draw_string(Graphics pic, string str, int x, int y, int size)//画字
        {
            // int x = 0, y = 0;
            pic.DrawString(str, new Font("宋体", size, FontStyle.Bold), new SolidBrush(Color.Red), x, y);
        }

        /****************************************************************************************************************/



        /**********************************************LED********************************************************/
        void led_send()
        {
            int nResult;
            LedDll.COMMUNICATIONINFO CommunicationInfo = new LedDll.COMMUNICATIONINFO();//定义一通讯参数结构体变量用于对设定的LED通讯，具体对此结构体元素赋值说明见COMMUNICATIONINFO结构体定义部份注示
            //TCP通讯********************************************************************************
            CommunicationInfo.SendType = 0;//设为固定IP通讯模式，即TCP通讯
            CommunicationInfo.IpStr = led大屏设置.ip;//给IpStr赋值LED控制卡的IP
            CommunicationInfo.LedNumber = 1;//LED屏号为1，注意socket通讯和232通讯不识别屏号，默认赋1就行了，485必需根据屏的实际屏号进行赋值

            int hProgram;//节目句柄
            hProgram = LedDll.LV_CreateProgram(led大屏设置.width, led大屏设置.height, led大屏设置.FontColor);//根据传的参数创建节目句柄，64是屏宽点数，32是屏高点数，2是屏的颜色，注意此处屏宽高及颜色参数必需与设置屏参的屏宽高及颜色一致，否则发送时会提示错误
            //此处可自行判断有未创建成功，hProgram返回NULL失败，非NULL成功,一般不会失败

            nResult = LedDll.LV_AddProgram(hProgram, 1, 0, 1);//添加一个节目，参数说明见函数声明注示
            if (nResult != 0)
            {
                string ErrStr;
                ErrStr = LedDll.LS_GetError(nResult);
                MessageBox.Show(ErrStr);
                return;
            }
            LedDll.AREARECT AreaRect = new LedDll.AREARECT();//区域坐标属性结构体变量
            AreaRect.left = 0;
            AreaRect.top = 0;
            AreaRect.width = led大屏设置.width;
            AreaRect.height = led大屏设置.height;

            LedDll.LV_AddImageTextArea(hProgram, 1, 1, ref AreaRect, 0);


            LedDll.PLAYPROP PlayProp = new LedDll.PLAYPROP();
            PlayProp.InStyle = 0;
            PlayProp.DelayTime = 3;
            PlayProp.Speed = 4;
            //可以添加多个子项到图文区，如下添加可以选一个或多个添加

            nResult = LedDll.LV_AddFileToImageTextArea(hProgram, 1, 1, "new.bmp", ref PlayProp);


            nResult = LedDll.LV_Send(ref CommunicationInfo, hProgram);//发送，见函数声明注示
            LedDll.LV_DeleteProgram(hProgram);//删除节目内存对象，详见函数声明注示
            if (nResult != 0)//如果失败则可以调用LV_GetError获取中文错误信息
            {
                string ErrStr;
                ErrStr = LedDll.LS_GetError(nResult);
                send_sate = "发送状态：" + (ErrStr);
            }
            else
            {
                send_sate = "发送状态：" + ("发送成功");
            }

        }
        private void button2_Click(object sender, EventArgs e)//发送图片到屏幕
        {
                led_send();
                return;
        }

        /****************************************************************************************************************/



        /************************************************MODBUS**********************************************************/
        private bool CheckResponse(byte[] response)   //对收到的报文进行CRC校验
        {

            try
            { //Perform a basic CRC check:
                byte[] CRC = new byte[2];
                GetCRC(response, ref CRC);
                if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
                    return true;
                else
                    return false;
            }
            catch { return false; }

        }

        private void GetCRC(byte[] message, ref byte[] CRC)//根据报文有效信息返回CRC值
        {
            //Function expects a modbus message of any length as well as a 2 byte CRC array in which to 
            //return the CRC values:

            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < (message.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }

        public static float ToFloat(byte[] data)//Byte 转float
        {
            float a = 0;
            byte i;
            byte[] x = data;
            unsafe
            {
                void* pf;
                fixed (byte* px = x)
                {
                    pf = &a;
                    for (i = 0; i < data.Length; i++)
                    {
                        *((byte*)pf + i) = *(px + i);
                    }
                }
            }
            return a;
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)//串口分析
        {
             System.Threading.Thread.Sleep(100);//等待接收完成

            this.Invoke((EventHandler)(delegate
          {
              
            Byte[] ReceivedData = new Byte[serialPort1.BytesToRead];
            serialPort1.Read(ReceivedData, 0, ReceivedData.Length);

              if (CheckResponse(ReceivedData))//CRC校验正确的话
              {
                  if ((ModbusSentSate != 4) && (ReceivedData[0] == 1) && ((ReceivedData[1] == 4) || (ReceivedData[1] == 3)))
                  //01 04/03 且等待从机
                  {
                      short DataLenth = ReceivedData[2];//得到有效数据字节数
                      byte[] DataBytes = new byte[DataLenth];

                      for (int i = 0; i < DataLenth; i++)    //把有效数据放入数组
                      {
                          DataBytes[i] = ReceivedData[3 + i];
                      }

                      for (int i = 0; i < DataLenth; i++)  //IEEE754标准  挪位  以满足Byte转float 00 00 44 4d为82，00 00 44 FA为2000
                      {
                          byte temp;
                          if (((i + 1) % 2) == 0)
                          {
                              temp = DataBytes[i];
                              DataBytes[i] = DataBytes[i - 1];
                              DataBytes[i - 1] = temp;
                          }
                      }

                      float ModBusValue = ToFloat(DataBytes);//转小数

                      if (ModbusSentSate == 1)
                      {
                          data[0] = ModBusValue;
                          label2.Text = "颗粒物:" + Convert.ToString(data[0]);
                      }
                      if (ModbusSentSate == 2)
                      {
                          data[1] = ModBusValue;
                          label3.Text = "二氧化硫:" + Convert.ToString(data[1]);
                      }
                      if (ModbusSentSate == 3)
                      {
                          data[2] = ModBusValue;
                          label4.Text = "氮氧化物:" + Convert.ToString(data[2]);
                          //MessageBox.Show("氮氧化物已更新:" + Convert.ToString(ModBusValue));
                      }

                  }

              }
                serialPort1.DiscardInBuffer();//丢弃接收缓冲区数据
          }));

        }

        /****************************************************************************************************************/


        /************************************************DEBUG**********************************************************/
        private void button11_Click(object sender, EventArgs e)//填随机数
        {
            Random xxx = new Random();
            for (int i = 0; i < 3; i++)
            {
                data[i] = xxx.Next(0, 20) ;
            }
            
            label2.Text = "颗粒物:" + Convert.ToString(data[0]);
            label3.Text = "二氧化硫:" + Convert.ToString(data[1]);
            label4.Text = "氮氧化物:" + Convert.ToString(data[2]);
        }




        /****************************************************************************************************************/


















    }
}
