using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;

namespace MyRpa
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private WorkbenchControl _workbench;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // 设置初始网址
            txtUrl.Text = "https://www.baidu.com";
            
            // 初始化浏览器，并绑定事件
            Browser.FrameLoadEnd += Browser_FrameLoadEnd;
            
            // 让浏览器等待加载完成后立即显示
            Browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
            
            // 配置浏览器设置 - 在新版本中JavaScript默认已启用
            // 下面是正确的设置方式
            Browser.BrowserSettings = new BrowserSettings
            {
                Javascript = CefState.Enabled,
                JavascriptAccessClipboard = CefState.Enabled,
                JavascriptDomPaste = CefState.Enabled
            };
            
            // 为浏览器启用JS绑定和消息传递机制
            Browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            
            // 注册cefSharp对象，用于JavaScript与C#通信
            Browser.JavascriptObjectRepository.ResolveObject += (sender, e) =>
            {
                var repo = e.ObjectRepository;
                if (e.ObjectName == "cefSharp")
                {
                    try
                    {
                        // 注册一个空对象，使用postMessage进行通信
                        repo.Register("cefSharp", new object(), true);
                        Console.WriteLine("成功注册cefSharp对象");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"注册cefSharp对象失败: {ex.Message}");
                    }
                }
            };
            
            // 开启控制台消息捕获，方便调试
            Browser.ConsoleMessage += (s, e) =>
            {
                Console.WriteLine($"[Browser Console] {e.Message}");
            };
            
            // 初始化工作台
            _workbench = new WorkbenchControl(Browser);
            workbenchContainer.Content = _workbench;
            
            // 更新状态
            UpdateStatus("已加载");
            
            // 处理窗口关闭事件
            Closing += MainWindow_Closing;
        }
        
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 确保在窗口关闭时进行清理
            Browser.Dispose();
        }
        
        private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Browser.IsBrowserInitialized)
            {
                // 浏览器初始化完成后，导航到初始网址
                Browser.Address = txtUrl.Text;
            }
        }
        
        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                // 在主线程中更新UI
                Dispatcher.Invoke(() =>
                {
                    txtUrl.Text = e.Url;
                    UpdateStatus($"页面已加载：{e.Url}");
                });
            }
        }
        
        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl();
        }
        
        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrl();
            }
        }
        
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack)
            {
                Browser.Back();
            }
        }
        
        private void NavigateToUrl()
        {
            if (!string.IsNullOrEmpty(txtUrl.Text))
            {
                string url = txtUrl.Text;
                
                // 确保URL格式正确
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }
                
                UpdateStatus($"正在导航到：{url}");
                Browser.Address = url;
            }
        }
        
        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
        }
    }
}
