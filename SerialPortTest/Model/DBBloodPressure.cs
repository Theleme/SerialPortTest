using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPortTest
{
    public class DBBloodPressure
    {
        /// <summary>
        /// 收缩压
        /// </summary>
        public double SYS { get; set; }

        /// <summary>
        /// 舒张压
        /// </summary>
        public double DIA { get; set; }

        /// <summary>
        /// 心率
        /// </summary>
        public double Pulse { get; set; }

        /// <summary>
        /// 是否睡眠（目前只有模式2可用）
        /// </summary>
        public bool IsSleep { get; set; }

        /// <summary>
        /// 是否自动测量
        /// </summary>
        public bool IsAuto { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 测量时间
        /// </summary>
        public DateTime? MeasureTime { get; set; }

        /// <summary>
        /// ID（测量模式）
        /// </summary>
        public int ID { get; set; }
    }
}
