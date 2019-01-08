using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace SerialPortTest
{
    public class SerialPortHelper
    {
        private bool _receivedStaus = false;//接收状态
        private bool _comOpen;
        private string _portName;
        private void SetComStausAfterClose()//成功关闭串口或串口丢失后的设置
        {
            _comOpen = false;//串口状态设置为关闭状态 
        }
        private void SetComStausLose()//成功关闭串口或串口丢失后的设置
        {
            SetComStausAfterClose();//成功关闭串口或串口丢失后的设置
        }
        public SerialPort _serialPort { get; set; }
        public byte[] ReceivedDataByte { get; set; }
        public byte[] ReceivedStausByte { get; set; }

        public Tuple<bool,string> ConfirmPort(byte[] buffer)
        {
            string[] comPortNames = SerialPort.GetPortNames();
            if (comPortNames.Count() < 1)
            {
                return new Tuple<bool, string>(false, "无可用的COM口");
            }
            //获取上次成功发送数据的串口，不需要每次都遍历串口
            if (!string.IsNullOrEmpty(_portName) && comPortNames.Contains(_portName))
            {
                comPortNames = comPortNames.Where(m => m != _portName).ToArray();
                try
                {
                    ReceivedStausByte = null;
                    _receivedStaus = false;
                    if (OpenSerialPort(_portName))
                    {
                        try
                        {
                            if (SendData(buffer))
                            {
                                return new Tuple<bool, string>(true, "串口通信正常");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("串口通信出现异常" + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //return new Tuple<bool, string>(false, "无可用的COM口");
                }
            }
            for (int i = 0; i < comPortNames.Count(); i++)
            {
                try
                {
                    ReceivedStausByte = null;
                    _receivedStaus = false;
                    if (OpenSerialPort(comPortNames[i]))
                    {
                        try
                        {
                            if (SendData(buffer))
                            {
                                _portName = comPortNames[i];
                                return new Tuple<bool, string>(true, "串口通信正常");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("串口通信出现异常" + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //return new Tuple<bool, string>(false, "无可用的COM口");
                }
            }
            return new Tuple<bool, string>(false, "串口通信出现异常");
        }

        public bool OpenSerialPort(string PortName)
        {
            if (_comOpen == false) //ComPortIsOpen == false当前串口为关闭状态，按钮事件为打开串口
            {
                try //尝试打开串口
                {
                    _serialPort = new SerialPort()
                    {
                        PortName = PortName,//串口名称
                        BaudRate = 9600,//串行波特率
                        Parity = Parity.None,//奇偶校验检查协议
                        DataBits = 8,//每个字节的标准数据位长度
                        StopBits = StopBits.Two,//每个字节的标准停止位数
                        DtrEnable = true,//串行通信中启用数据终端就绪（DTR）信号
                        RtsEnable = true,//串行通信中启用请求发送（RTS）信号
                        ReadTimeout = 500, //读取操作未完成时发生超时之前的毫秒数
                        WriteTimeout = 500, //写入操作未完成时发生超时之前的毫秒数
                        ReadBufferSize = 51200, //输入缓冲区的大小
                        WriteBufferSize = 1024, //输入缓冲区的大小
                    };
                    _serialPort.ReceivedBytesThreshold = 1;
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler); //串口接收中断
                    _serialPort.Open();
                    if (_serialPort.IsOpen)
                    {
                        _comOpen = true; //串口打开状态字改为true   
                    }
                }
                catch (Exception exception) //如果串口被其他占用，则无法打开
                {
                    _comOpen = false;
                    Console.WriteLine(exception.Message);
                    return false;
                }
                return true;
            }
            return true;
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (!_receivedStaus)
            {
                if (_serialPort.BytesToRead >= 6)
                {
                    ReceivedStausByte = new byte[6];
                    _serialPort.Read(ReceivedStausByte, 0, 6);
                    _receivedStaus = true;
                }
            }
        }

        public bool SendData(byte[] dataPackeg)
        {
            _serialPort.Write(dataPackeg, 0, dataPackeg.Length);
            for (int i = 0; i < 10; i++)
            {
                if (_receivedStaus)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            try
            {
                if (ReceivedStausByte[5] != 6)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                //报错说明获取信息出现问题，所用串口非所需串口，释放串口
                Console.WriteLine(ex.Message);
                CloseSerialPort();
                return false;
            }
        }

        public byte[] SendDataReceived()
        {
            try
            {
                Thread.Sleep(500);//0.5s后读取数据并返回确认代码
                byte[] _receivedLen = new byte[9];//接收数据的长度
                _serialPort.Read(_receivedLen, 0, 9);
                int len = Convert.ToInt16(((char)_receivedLen[4]).ToString()) * 16 * 16 * 16 + Convert.ToInt16(((char)_receivedLen[5]).ToString()) * 16 * 16 + Convert.ToInt16(((char)_receivedLen[6]).ToString()) * 16 + Convert.ToInt16(((char)_receivedLen[7]).ToString());
                if (len > 0)
                {
                    ReceivedDataByte = new byte[len];
                    _serialPort.Read(ReceivedDataByte, 0, len);
                }
                byte[] received = new byte[6] { 0x01, 0x50, 0x43, 0x33, 0x30, 0x06 };
                _serialPort.Write(received, 0, 6);
                return ReceivedDataByte;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new byte[0];
            }
        }

        public bool CloseSerialPort()
        {
            try//尝试关闭串口
            {
                _serialPort.DiscardOutBuffer();//清发送缓存
                _serialPort.DiscardInBuffer();//清接收缓存
                //WaitClose = true;//激活正在关闭状态字，用于在串口接收方法的invoke里判断是否正在关闭串口
                _serialPort.Close();//关闭串口
                                          // WaitClose = false;//关闭正在关闭状态字，用于在串口接收方法的invoke里判断是否正在关闭串口
                SetComStausAfterClose();//成功关闭串口或串口丢失后的设置
                _comOpen = false;
            }
            catch//如果在未关闭串口前，串口就已丢失，这时关闭串口会出现异常
            {
                if (_serialPort.IsOpen == false)//判断当前串口状态，如果ComPort.IsOpen==false，说明串口已丢失
                {
                    SetComStausLose();
                }
                else//未知原因，但是串口的状态是已经关闭
                {
                }
            }
            return true;
        }
    }
}
