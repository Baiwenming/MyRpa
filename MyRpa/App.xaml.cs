using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace MyRpa
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeCef();
        }
        
        private void InitializeCef()
        {
            // 设置CEF的缓存和日志目录
            var settings = new CefSettings();
            
            // 设置缓存目录为用户AppData中的临时文件夹
            string cefCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyRpa", "CEF");
            settings.CachePath = cefCachePath;
            
            // 禁用GPU加速（解决某些系统上的显示问题）
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            
            // 启用远程调试端口（用于调试JavaScript）
            settings.CefCommandLineArgs.Add("remote-debugging-port", "9222");
            
            // 以下为跨域安全设置
            settings.CefCommandLineArgs.Add("disable-web-security", "1");
            
            // 注册CEF的JS处理程序
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "localfolder",
                SchemeHandlerFactory = new CefSharp.SchemeHandler.FolderSchemeHandlerFactory(
                    rootFolder: System.AppDomain.CurrentDomain.BaseDirectory
                )
            });
            
            // 初始化CEF
            Cef.Initialize(settings);
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // 在应用退出时关闭CEF
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
