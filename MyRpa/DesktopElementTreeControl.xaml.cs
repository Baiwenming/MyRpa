using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyRpa
{
    /// <summary>
    /// 桌面元素树节点
    /// </summary>
    public class ElementTreeNode
    {
        public string ElementName { get; set; }
        public string ElementType { get; set; }
        public AutomationElement Element { get; set; }
        public ObservableCollection<ElementTreeNode> Children { get; set; }

        public ElementTreeNode()
        {
            Children = new ObservableCollection<ElementTreeNode>();
        }
    }

    /// <summary>
    /// 元素属性项
    /// </summary>
    public class ElementProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// DesktopElementTreeControl.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopElementTreeControl : UserControl
    {
        private AutomationElement _rootElement;
        private Process _process;

        public DesktopElementTreeControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 加载应用程序元素
        /// </summary>
        public void LoadApplication(Process process, AutomationElement rootElement)
        {
            _process = process;
            _rootElement = rootElement;

            // 更新应用程序信息
            txtProcessName.Text = process?.ProcessName ?? "未知";
            txtProcessId.Text = process?.Id.ToString() ?? "";
            txtWindowTitle.Text = rootElement?.Current.Name ?? "";

            // 构建元素树
            BuildElementTree();
        }

        /// <summary>
        /// 构建元素树
        /// </summary>
        private void BuildElementTree()
        {
            if (_rootElement == null) return;

            try
            {
                // 清空现有树
                treeElements.Items.Clear();

                // 创建根节点
                var rootNode = CreateElementNode(_rootElement);

                // 递归构建子节点
                BuildChildNodes(rootNode, _rootElement, 3); // 限制初始递归深度以提高性能

                // 设置为数据源
                treeElements.Items.Add(rootNode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"构建元素树出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 递归构建子节点
        /// </summary>
        private void BuildChildNodes(ElementTreeNode parentNode, AutomationElement parentElement, int maxDepth)
        {
            if (maxDepth <= 0) return;

            try
            {
                // 获取所有子元素
                var children = DesktopElementSelector.GetChildElements(parentElement);

                foreach (var child in children)
                {
                    // 创建子节点
                    var childNode = CreateElementNode(child);
                    parentNode.Children.Add(childNode);

                    // 递归构建孙节点
                    BuildChildNodes(childNode, child, maxDepth - 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"构建子节点出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从AutomationElement创建节点
        /// </summary>
        private ElementTreeNode CreateElementNode(AutomationElement element)
        {
            string name = string.IsNullOrEmpty(element.Current.Name) ? "(无名称)" : element.Current.Name;
            string type = element.Current.ControlType.ProgrammaticName.Replace("ControlType.", "");

            return new ElementTreeNode
            {
                Element = element,
                ElementName = name,
                ElementType = type
            };
        }

        /// <summary>
        /// 显示元素的属性
        /// </summary>
        private void ShowElementProperties(AutomationElement element)
        {
            if (element == null) return;

            try
            {
                // 创建属性列表
                var properties = new List<ElementProperty>();

                // 添加基本属性
                properties.Add(new ElementProperty { Name = "名称", Value = element.Current.Name });
                properties.Add(new ElementProperty { Name = "控件类型", Value = element.Current.ControlType.ProgrammaticName });
                properties.Add(new ElementProperty { Name = "自动化ID", Value = element.Current.AutomationId });
                properties.Add(new ElementProperty { Name = "类名", Value = element.Current.ClassName });
                properties.Add(new ElementProperty { Name = "加速键", Value = element.Current.AcceleratorKey });
                properties.Add(new ElementProperty { Name = "访问键", Value = element.Current.AccessKey });
                properties.Add(new ElementProperty { Name = "边界矩形", Value = element.Current.BoundingRectangle.ToString() });
                properties.Add(new ElementProperty { Name = "框架ID", Value = element.Current.FrameworkId });
                properties.Add(new ElementProperty { Name = "已启用", Value = element.Current.IsEnabled.ToString() });
                properties.Add(new ElementProperty { Name = "是内容元素", Value = element.Current.IsContentElement.ToString() });
                properties.Add(new ElementProperty { Name = "是控件元素", Value = element.Current.IsControlElement.ToString() });
                properties.Add(new ElementProperty { Name = "在屏幕外", Value = element.Current.IsOffscreen.ToString() });
                properties.Add(new ElementProperty { Name = "可用于键盘", Value = element.Current.IsKeyboardFocusable.ToString() });
                properties.Add(new ElementProperty { Name = "进程ID", Value = element.Current.ProcessId.ToString() });

                // 尝试获取附加属性
                try
                {
                    object valuePattern;
                    if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valuePattern))
                    {
                        string value = ((ValuePattern)valuePattern).Current.Value;
                        properties.Add(new ElementProperty { Name = "值", Value = value });
                    }
                }
                catch { }

                // 设置为ListView的数据源
                lstProperties.ItemsSource = properties;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示元素属性出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 选中元素变更事件处理
        /// </summary>
        private void treeElements_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ElementTreeNode node && node.Element != null)
            {
                ShowElementProperties(node.Element);
            }
        }

        /// <summary>
        /// 树节点双击事件，高亮显示该元素
        /// </summary>
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is ElementTreeNode node)
            {
                // 高亮显示该元素
                HighlightElement(node.Element);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 高亮显示元素
        /// </summary>
        private void HighlightElement(AutomationElement element)
        {
            if (element == null) return;

            try
            {
                // 创建临时高亮窗口
                var highlightWindow = new HighlightWindow();
                highlightWindow.Show();

                // 设置位置
                highlightWindow.UpdatePosition(element.Current.BoundingRectangle);

                // 3秒后自动关闭
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) => {
                    highlightWindow.Close();
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"高亮元素出错: {ex.Message}");
            }
        }
    }
}