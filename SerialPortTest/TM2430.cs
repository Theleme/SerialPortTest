using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace SerialPortTest
{
    public class TM2430
    {
        private SerialPortHelper _serialPortHelper = new SerialPortHelper();

        /// <summary>
        /// 获取血压数据（数据格式I）
        /// </summary>
        /// <returns></returns>
        public Tuple<List<DBBloodPressure>, string> GetResultStyleI()
        {
            List<DBBloodPressure> bps = new List<DBBloodPressure>();
            //发送读取数据命令：0x31,0x30
            var IsSend = SendCommand(0x31, 0x30);
            if (!IsSend.Item1)
            {
                return new Tuple<List<DBBloodPressure>, string>(bps, "无法通信，请确认是否连接出现问题！");
            }
            //发送确认指令
            var databyte = _serialPortHelper.SendDataReceived();
            //关闭串口
            _serialPortHelper.CloseSerialPort();
            if (databyte == null || databyte.Count() == 0)
            {
                //机器没有数据
                return new Tuple<List<DBBloodPressure>, string>(bps, "机器中无数据！");
            }
            else
            {
                int size = databyte.Count() / 22;//机器存储数据的格式，总共血压数
                for (int i = 0; i < size; i++)
                {

                    int year, month, day, hour, minute;

                    year = 1900 + ByteToInt(databyte[i * 22 + 10]) * 16 + ByteToInt(databyte[i * 22 + 11]);
                    month = ByteToInt(databyte[i * 22 + 12]) * 16 + ByteToInt(databyte[i * 22 + 13]);
                    day = ByteToInt(databyte[i * 22 + 14]) * 16 + ByteToInt(databyte[i * 22 + 15]);
                    hour = ByteToInt(databyte[i * 22 + 16]) * 16 + ByteToInt(databyte[i * 22 + 17]);
                    minute = ByteToInt(databyte[i * 22 + 18]) * 16 + ByteToInt(databyte[i * 22 + 19]);

                    DateTime dt = new DateTime(year, month, day, hour, minute, 0);

                    DBBloodPressure bp = new DBBloodPressure()
                    {
                        SYS = ByteToInt(databyte[i * 22 + 2]) * 16 + ByteToInt(databyte[i * 22 + 3]) + ByteToInt(databyte[i * 22 + 0]) * 16 + ByteToInt(databyte[i * 22 + 1]),
                        DIA = ByteToInt(databyte[i * 22 + 2]) * 16 + ByteToInt(databyte[i * 22 + 3]),
                        Pulse = ByteToInt(databyte[i * 22 + 4]) * 16 + ByteToInt(databyte[i * 22 + 5]),
                        ErrorCode = ByteToInt(databyte[i * 22 + 6]) * 16 + ByteToInt(databyte[i * 22 + 7]),
                        IsAuto = (databyte[i * 22 + 8] & 1) == 0,
                        IsSleep = (databyte[i * 22 + 9] & 2) == 2,
                        MeasureTime = dt,
                        ID = ByteToInt(databyte[i * 22 + 20]) * 16 + ByteToInt(databyte[i * 22 + 21])
                    };
                    bps.Add(bp);
                }
                return new Tuple<List<DBBloodPressure>, string>(bps, "获取数据成功！");
            }
        }

        /// <summary>
        /// 设置机器测量模式及测量间隔
        /// </summary>
        /// <param name="_tM2430Configs"></param>
        /// <param name="SettingMode"></param>
        /// <returns></returns>
        public Tuple<bool, string> SetMode(List<TM2430Config> _tM2430Configs, int SettingMode = 3)
        {
            int[] Modes = new int[3] { 1, 2, 3 };
            if (!Modes.Contains(SettingMode) || ((_tM2430Configs.Count() <= 0 || _tM2430Configs.Count() > 6) && SettingMode == 3))
            {
                return new Tuple<bool, string>(false, "机器设置参数错误！");
            }
            //清除数据
            var isClear = ClearData();
            if (!isClear.Item1)
            {
                return new Tuple<bool, string>(false, "无法清除血压计数据！");
            }
            //设置时间
            var isSetData = SetDate();
            if (!isSetData.Item1)
            {
                return new Tuple<bool, string>(false, "无法设置血压计时间！");
            }

            //发送设置测量条件命令
            var isSetTips = SendCommand(0x33, 0x30);
            if (!isSetTips.Item1)
            {
                return new Tuple<bool, string>(false, "设置命令发送失败！");
            }
            byte[] _tipBuffer = new byte[44] { 0x02,//SX(02)
                0x44,//识别D
                0x50 ,0x43,//转发地：PC
                0x30,0x30,0x32,0x32,//数据长度
                0x30,//数据种类
                //以下为数据
                0x30,0x31,//显示“ON”：“1”；如果是“OFF”则为：“0”
                0x30,0x32,//ID
                0x30,0x30,//间隔设置格式号码 “1”：7:00-22:00 15分钟间隔/22:00-7:00 30分钟间隔；“2”：全区间15分钟间隔/Sleep ON时30分钟间隔；“3”：任意设置
                0x30,0x30,//Sleep；设置Sleep ON时的时间间隔。模式1、3时，设置为0，模式2时，设置为“30”（0x33,0x30）
                0x30,0x30,//开始时间1
                0x30,0x30,//测量间隔1
                0x30,0x30,//开始时间2
                0x30,0x30,//测量间隔2
                0x30,0x30,//开始时间3
                0x30,0x30,//测量间隔3
                0x30,0x30,//开始时间4
                0x30,0x30,//测量间隔4
                0x30,0x30,//开始时间5
                0x30,0x30,//测量间隔5
                0x30,0x30,//开始时间6
                0x30,0x30,//测量间隔6
                0x30,0x30,//终止时间
                0//SUM
            };
            if (SettingMode == 2)//测量模式2：全区间15分钟间隔/Sleep ON时30分钟间隔
            {
                //测量模式：0 2
                _tipBuffer[13] = 0x30;
                _tipBuffer[14] = 0x32;
                //Sleep ON模式下 测量间隔：30分钟：1 E
                _tipBuffer[15] = 0x31;
                _tipBuffer[16] = 0x45;
                //测量间隔：30分钟：1 E
                _tipBuffer[19] = 0x31;
                _tipBuffer[20] = 0x45;
            }
            else if (SettingMode == 1)//测量模式1：7:00-22:00 15分钟间隔/22:00-7:00 30分钟间隔
            {
                //测量模式：0 1
                _tipBuffer[13] = 0x30;
                _tipBuffer[14] = 0x31;
                //开始时间1
                _tipBuffer[17] = 0x30;
                _tipBuffer[18] = 0x37;
                //测量间隔1
                _tipBuffer[19] = 0x30;
                _tipBuffer[20] = 0x46;
                //开始时间2
                _tipBuffer[21] = 0x31;
                _tipBuffer[22] = 0x36;
                //测量间隔2
                _tipBuffer[23] = 0x31;
                _tipBuffer[24] = 0x45;
                //开始时间3
                _tipBuffer[25] = 0x30;
                _tipBuffer[26] = 0x37;
                //测量间隔3
                _tipBuffer[27] = 0x30;
                _tipBuffer[28] = 0x30;
            }
            else//测量模式3：任意设置
            {
                if (_tM2430Configs.Count() == 1)//如果只有一条记录
                {
                    //测量模式：0 3
                    _tipBuffer[13] = 0x30;
                    _tipBuffer[14] = 0x33;
                    //开始时间1
                    _tipBuffer[17] = 0x30;
                    _tipBuffer[18] = 0x30;
                    //测量间隔1
                    _tipBuffer[19] = 0x31;
                    _tipBuffer[20] = 0x45;
                }
                else
                {
                    int _setTimeBegin = 17;
                    for (int i = 0; i < _tM2430Configs.Count(); i++)
                    {
                        _tipBuffer[_setTimeBegin++] = IntToHexChar(_tM2430Configs[i].Time / 16);
                        _tipBuffer[_setTimeBegin++] = IntToHexChar(_tM2430Configs[i].Time % 16);
                        _tipBuffer[_setTimeBegin++] = IntToHexChar(_tM2430Configs[i].Interval / 16);
                        _tipBuffer[_setTimeBegin++] = IntToHexChar(_tM2430Configs[i].Interval % 16);
                    }
                    _tipBuffer[_setTimeBegin++] = IntToHexChar(_tM2430Configs[0].Time / 16);
                    _tipBuffer[_setTimeBegin++] = IntToHexChar(_tM2430Configs[0].Time % 16);
                }
            }
            _tipBuffer[43] = CRC(_tipBuffer, 43);
            var isSetSuccess = SendData(_tipBuffer, 44);
            if (!isSetSuccess.Item1)
            {
                return new Tuple<bool, string>(false, "设置参数发送失败！");
            }
            else
            {
                return new Tuple<bool, string>(true, "写入机器成功！");
            }
        }

        #region ==写入设备之前操作（清除数据、设置时间）==
        /// <summary>
        /// 清除血压计的数据
        /// </summary>
        /// <returns></returns>
        public Tuple<bool, string> ClearData()
        {
            //发送清除数据命令：0x31,0x32
            var isClear = SendCommand(0x31, 0x32);
            return isClear;
        }

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <returns></returns>
        public Tuple<bool, string> SetDate()
        {
            //发送设置时间命令
            if (!SendCommand(0x33, 0x31).Item1)
            {
                return new Tuple<bool, string>(false, "命令发送失败！");
            }

            byte[] buffer = new byte[20] { 0x02, 0x44, 0x50, 0x43, 0x30, 0x30, 0x30, 0x41, 0x30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            DateTime now = DateTime.Now;

            //年
            buffer[9] = IntToHexChar((now.Year - 1900) / 16);
            buffer[10] = IntToHexChar((now.Year - 1900) % 16);

            //月
            buffer[11] = IntToHexChar(now.Month / 16);
            buffer[12] = IntToHexChar(now.Month % 16);

            //日
            buffer[13] = IntToHexChar(now.Day / 16);
            buffer[14] = IntToHexChar(now.Day % 16);

            //时
            buffer[15] = IntToHexChar(now.Hour / 16);
            buffer[16] = IntToHexChar(now.Hour % 16);

            //分
            buffer[17] = IntToHexChar(now.Minute / 16);
            buffer[18] = IntToHexChar(now.Minute % 16);

            buffer[19] = CRC(buffer, 19);

            var isSetData = SendData(buffer, 20);
            return isSetData;
        }
        #endregion

        #region ==装配命令并发送==
        //拼装命令
        private Tuple<bool, string> SendCommand(byte high, byte low)
        {
            byte[] buffer = new byte[7] { 0x02, 0x43, 0x50, 0x43, high, low, 0 };

            //CRC校验
            buffer[6] = CRC(buffer, 6);
            return SendData(buffer, 7);
        }

        //向机器发送命令
        private Tuple<bool, string> SendData(byte[] buffer, int len)
        {
            var isSendData = _serialPortHelper.ConfirmPort(buffer);
            return isSendData;
        }
        #endregion

        #region ==校验==
        //CRC校验
        private byte CRC(byte[] buffer, int len)
        {
            int i;
            byte r = 0;
            for (i = 1; i < len; i++)
            {
                r += buffer[i];
            }
            return r;
        }

        //XOR校验（异或校验）
        private byte XOR(byte[] buffer, int len)
        {
            int i;
            byte r = 0;
            for (i = 0; i < len; i++)
            {
                r ^= buffer[i];
            }
            return r;
        }
        #endregion
        #region ==转换==
        //将十进制的数转换为ASCII
        private byte IntToHexChar(int b)
        {
            byte hex1;
            if (b >= 0 && b <= 9)
                hex1 = Convert.ToByte(48 + b);
            else
                hex1 = Convert.ToByte(55 + b);

            return hex1;
        }

        //将ASCII码转换为十进制
        public int ByteToInt(byte byt)
        {
            int convertInt = 0;
            if (byt >= 48 && byt <= 57)
            {
                convertInt = byt - 48;
            }
            else if (byt >= 65 && byt <= 90)//十六进制的转成十进制（大写字符）
            {
                convertInt = byt - 65 + 10;

            }
            else if (byt >= 97 && byt <= 122)//十六进制的转成十进制（小写字符）
            {
                convertInt = byt - 97 + 10;

            }
            return convertInt;
        }
        #endregion
    }
}
