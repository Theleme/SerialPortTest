using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPortTest
{
    public class TM2430Config
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// 测量间隔
        /// </summary>
        public int Interval { get; set; }
    }
}
