using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;

namespace MyRpa
{
    /// <summary>
    /// 桌面元素选择事件参数
    /// </summary>
    public class DesktopElementSelectedEventArgs : EventArgs
    {
        public DesktopElementInfo SelectedElement { get; }
        
        public DesktopElementSelectedEventArgs(DesktopElementInfo selectedElement)
        {
            SelectedElement = selectedElement;
        }
    }
    
    /// <summary>
    /// 桌面元素选择器类
    /// </summary>
    public class DesktopElementSelector
    {
        // 事件定义
        public event EventHandler<DesktopElementSelectedEventArgs> ElementSelected;
        public event EventHandler SelectionCancelled;
        
        // 状态标志
        public bool IsCapturing { get; private set; }
        
        // 全局钩子处理
        private MouseHook _mouseHook;
        private KeyboardHook _keyboardHook;
        
        // 高亮窗口
        private HighlightWindow _highlightWindow;
        
        // 后台工作线程
        private CancellationTokenSource _cancellationTokenSource;
        private Task _captureTask;
        
        // AutomationElement缓存
        private AutomationElement _lastElement;
        private System.Windows.Point _lastPoint;
        
        public DesktopElementSelector()
        {
            // 初始化钩子
            _mouseHook = new MouseHook();
            _keyboardHook = new KeyboardHook();
            
            // 绑定事件
            _mouseHook.MouseMove += MouseHook_MouseMove;
            _mouseHook.MouseDown += MouseHook_MouseDown;
            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
        }
        
        /// <summary>
        /// 开始元素捕获
        /// </summary>
        public void StartElementCapture()
        {
            if (IsCapturing)
                return;
                
            IsCapturing = true;
            
            // 创建高亮窗口
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _highlightWindow = new HighlightWindow();
                _highlightWindow.Show();
            });
            
            // 安装钩子
            _mouseHook.Install();
            _keyboardHook.Install();
            
            // 创建取消令牌
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 在后台线程运行捕获过程
            _captureTask = Task.Run(() => CaptureProcedure(_cancellationTokenSource.Token));
        }
        
        /// <summary>
        /// 停止元素捕获
        /// </summary>
        public void StopElementCapture()
        {
            if (!IsCapturing)
                return;
                
            // 取消任务
            _cancellationTokenSource?.Cancel();
            
            // 卸载钩子
            _mouseHook.Uninstall();
            _keyboardHook.Uninstall();
            
            // 关闭高亮窗口
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _highlightWindow?.Close();
                _highlightWindow = null;
            });
            
            IsCapturing = false;
            
            // 触发取消事件
            SelectionCancelled?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// 元素捕获过程
        /// </summary>
        private void CaptureProcedure(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(50); // 降低CPU使用率
            }
        }
        
        /// <summary>
        /// 处理鼠标移动
        /// </summary>
        private void MouseHook_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsCapturing) return;
            
            try
            {
                // 获取鼠标位置下的元素
                var element = GetElementFromPoint(e.X, e.Y);
                if (element.Current.Name!=null) 
                {
                    Console.WriteLine(element.Current.Name);
                }
                if (element != null && _lastElement != element)
                {
                    _lastElement = element;
                    _lastPoint = new System.Windows.Point(e.X, e.Y);
                    
                    // 更新高亮窗口
                    UpdateHighlight(element);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"鼠标移动处理错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理鼠标点击
        /// </summary>
        private void MouseHook_MouseDown(object sender, MouseEventArgs e)
        {
            if (!IsCapturing || e.Button != MouseButtons.Left) return;
            
            try
            {
                // 元素选择
                var element = GetElementFromPoint(e.X, e.Y);
                if (element != null)
                {
                    // 提取信息并通知选中事件
                    var elementInfo = ExtractElementInfo(element);
                    
                    // 停止捕获
                    StopElementCapture();
                    
                    // 在UI线程上触发事件
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ElementSelected?.Invoke(this, new DesktopElementSelectedEventArgs(elementInfo));
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"鼠标点击处理错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理键盘按键
        /// </summary>
        private void KeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsCapturing) return;
            
            // ESC键取消选择
            if (e.KeyCode == Keys.Escape)
            {
                StopElementCapture();
            }
        }
        
        /// <summary>
        /// 从指定点获取UI元素
        /// </summary>
        private AutomationElement GetElementFromPoint(int x, int y)
        {
            try
            {
                return AutomationElement.FromPoint(new System.Windows.Point(x, y));
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 从AutomationElement提取元素信息
        /// </summary>
        private DesktopElementInfo ExtractElementInfo(AutomationElement element)
        {
            try
            {
                // 获取进程和窗口信息
                var processId = element.Current.ProcessId;
                AutomationElement rootElement = GetRootElement(element);
                string windowTitle = rootElement?.Current.Name ?? "";
                string processName = "";
                
                try 
                {
                    var process = System.Diagnostics.Process.GetProcessById(processId);
                    processName = process.ProcessName;
                }
                catch { }
                
                // 创建元素信息对象
                return new DesktopElementInfo
                {
                    AutomationId = element.Current.AutomationId,
                    ControlType = element.Current.ControlType.ProgrammaticName,
                    ClassName = element.Current.ClassName,
                    Name = element.Current.Name,
                    WindowTitle = windowTitle,
                    ProcessName = processName,
                    ProcessId = processId,
                    BoundingRectangle = element.Current.BoundingRectangle,
                    IsEnabled = element.Current.IsEnabled,
                    IsOffscreen = element.Current.IsOffscreen,
                    SearchConditions = new[] 
                    { 
                        $"AutomationId={element.Current.AutomationId}", 
                        $"Name={element.Current.Name}",
                        $"ControlType={element.Current.ControlType.ProgrammaticName}"
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提取元素信息错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取元素所属的根窗口元素
        /// </summary>
        private AutomationElement GetRootElement(AutomationElement element)
        {
            try
            {
                AutomationElement parent = TreeWalker.ControlViewWalker.GetParent(element);
                while (parent != null && 
                      parent != AutomationElement.RootElement && 
                      parent.Current.ControlType != ControlType.Window)
                {
                    parent = TreeWalker.ControlViewWalker.GetParent(parent);
                }
                
                return parent;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 更新元素高亮显示
        /// </summary>
        private void UpdateHighlight(AutomationElement element)
        {
            if (element == null || _highlightWindow == null) return;
            
            try
            {
                var rect = element.Current.BoundingRectangle;
                
                // 在UI线程上更新高亮窗口
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _highlightWindow.UpdatePosition(rect);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新高亮错误: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 元素高亮窗口
    /// </summary>
    public class HighlightWindow : Window
    {
        public HighlightWindow()
        {
            // 设置窗口属性
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 120, 215));
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));
            BorderThickness = new Thickness(2);
            
            // 使窗口不获取焦点
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                Win32Interop.SetWindowExNoActivate(hwnd);
            }
        }
        
        /// <summary>
        /// 窗口创建时设置不获取焦点
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            var hwnd = new WindowInteropHelper(this).Handle;
            Win32Interop.SetWindowExNoActivate(hwnd);
        }
        
        /// <summary>
        /// 更新高亮位置和大小
        /// </summary>
        public void UpdatePosition(Rect rect)
        {
            Left = rect.Left;
            Top = rect.Top;
            Width = rect.Width;
            Height = rect.Height;
        }
    }
    
    /// <summary>
    /// 鼠标钩子类
    /// </summary>
    public class MouseHook
    {
        public event MouseEventHandler MouseMove;
        public event MouseEventHandler MouseDown;
        
        // 底层钩子
        private IntPtr _hookId = IntPtr.Zero;
        
        // 鼠标事件回调
        private Win32Interop.HookProc _hookProc;
        
        public void Install()
        {
            _hookProc = MouseHookCallback;
            _hookId = Win32Interop.SetMouseHook(_hookProc);
        }
        
        public void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                Win32Interop.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
        
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = (Win32Interop.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32Interop.MSLLHOOKSTRUCT));
                
                switch ((int)wParam)
                {
                    case Win32Interop.WM_MOUSEMOVE:
                        MouseMove?.Invoke(this, new MouseEventArgs(MouseButtons.None, 0, hookStruct.pt.x, hookStruct.pt.y, 0));
                        break;
                    case Win32Interop.WM_LBUTTONDOWN:
                        MouseDown?.Invoke(this, new MouseEventArgs(MouseButtons.Left, 1, hookStruct.pt.x, hookStruct.pt.y, 0));
                        break;
                }
            }
            
            return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
    
    /// <summary>
    /// 键盘钩子类
    /// </summary>
    public class KeyboardHook
    {
        public event KeyEventHandler KeyDown;
        
        // 底层钩子
        private IntPtr _hookId = IntPtr.Zero;
        
        // 键盘事件回调
        private Win32Interop.HookProc _hookProc;
        
        public void Install()
        {
            _hookProc = KeyboardHookCallback;
            _hookId = Win32Interop.SetKeyboardHook(_hookProc);
        }
        
        public void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                Win32Interop.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }
        
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                
                if ((int)wParam == Win32Interop.WM_KEYDOWN)
                {
                    KeyDown?.Invoke(this, new KeyEventArgs((Keys)vkCode));
                }
            }
            
            return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
    
    /// <summary>
    /// Win32 API 互操作服务
    /// </summary>
    public static class Win32Interop
    {
        // Win32 常量
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_KEYDOWN = 0x0100;
        private const int GWLP_HWNDPARENT = -8;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        
        // 鼠标钩子常量
        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        
        // 委托定义
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        // 结构体定义
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        
        // API导入
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        // 辅助方法
        public static IntPtr SetMouseHook(HookProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        public static IntPtr SetKeyboardHook(HookProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        public static void SetWindowExNoActivate(IntPtr hwnd)
        {
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }
    }
} 