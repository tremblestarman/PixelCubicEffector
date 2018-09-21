﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PCE_Video
{
    class Program
    {
        static private string CmdPath;
        static void Main(string[] args)
        {
            CmdPath = Environment.CurrentDirectory;
        }
        public static void RunCmd(string cmd, out string output)
        {
            cmd = cmd.Trim().TrimEnd('&') + "&exit";//说明：不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态  
            using (Process p = new Process())
            {
                p.StartInfo.FileName = CmdPath;
                p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动  
                p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息  
                p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息  
                p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出  
                p.StartInfo.CreateNoWindow = true;          //不显示程序窗口  
                p.Start();//启动程序  

                //向cmd窗口写入命令  
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.AutoFlush = true;

                //获取cmd窗口的输出信息  
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();//等待程序执行完退出进程  
                p.Close();
            }
        }
    }
}
