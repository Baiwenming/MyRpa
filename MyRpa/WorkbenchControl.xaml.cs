using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using CefSharp.Wpf;
using Microsoft.Win32;

namespace MyRpa
{
    public partial class WorkbenchControl : UserControl
    {
        private ChromiumWebBrowser _browser;
        private ElementSelector _elementSelector;
        private WorkflowManager _workflowManager;
        private BrowserAction _currentEditingAction;
        private ElementInfo _selectedElement;
        
        // 已选择元素列表
        private List<ElementListItem> _selectedElements = new List<ElementListItem>();
        
        // 构造函数
        public WorkbenchControl(ChromiumWebBrowser browser)
        {
            InitializeComponent();
            
            _browser = browser;
            _elementSelector = new ElementSelector(_browser);
            _workflowManager = new WorkflowManager(_browser);
            
            // 注册元素选择事件
            _elementSelector.ElementSelected += ElementSelector_ElementSelected;
            
            // 监听工作流事件
            _workflowManager.ActionStarted += WorkflowManager_ActionStarted;
            _workflowManager.ActionCompleted += WorkflowManager_ActionCompleted;
            _workflowManager.ActionFailed += WorkflowManager_ActionFailed;
            
            // 绑定操作列表数据源
            UpdateActionsList();
            
            // 初始化元素列表
            UpdateElementsList();
            
            // 初始化按钮状态
            UpdateElementSelectionButtons();
        }
        
        // 更新操作列表
        private void UpdateActionsList()
        {
            var items = new List<ActionListItem>();
            
            for (int i = 0; i < _workflowManager.Actions.Count; i++)
            {
                var action = _workflowManager.Actions[i];
                items.Add(new ActionListItem 
                { 
                    Index = i + 1, 
                    Action = action,
                    ActionType = action.ActionType.ToString(),
                    Name = action.Name,
                    Description = action.Description
                });
            }
            
            lstActions.ItemsSource = items;
        }
        
        // 选中元素事件处理
        private void ElementSelector_ElementSelected(object sender, ElementSelectedEventArgs e)
        {
            // 在UI线程上执行所有UI更新操作
            Application.Current.Dispatcher.Invoke(() =>
            {
                _selectedElement = e.SelectedElement;
            
            // 如果当前正在编辑一个操作，自动填充元素信息
            if (_currentEditingAction != null)
            {
                switch (_currentEditingAction)
                {
                    case ClickElementAction clickAction:
                        clickAction.TargetElement = _selectedElement;
                        break;
                    case InputTextAction inputAction:
                        inputAction.TargetElement = _selectedElement;
                        break;
                    case GetTextAction getTextAction:
                        getTextAction.TargetElement = _selectedElement;
                        break;
                    case SubmitFormAction submitAction:
                        submitAction.FormElement = _selectedElement;
                        break;
                    case ExecuteJavaScriptAction jsAction:
                        jsAction.TargetElement = _selectedElement;
                        break;
                }
                
                // 更新属性面板
                UpdatePropertyPanel();
            }
            
            // 将选中的元素添加到元素列表
            var elementItem = new ElementListItem(_selectedElement);
            _selectedElements.Add(elementItem);
            
            // 更新元素列表显示
            UpdateElementsList();
            
            // 显示选择的元素信息（改为简短消息）
            Console.WriteLine($"已选择元素: {_selectedElement}");
            
            // 更新按钮状态
            UpdateElementSelectionButtons();
            });
        }
        
        // 更新元素列表
        private void UpdateElementsList()
        {
            // 检查当前是否在UI线程，如果不是，则使用Dispatcher
            if (!CheckAccess())
            {
                Dispatcher.Invoke(UpdateElementsList);
                return;
            }
            
            // 在UI线程上执行
            lstSelectedElements.ItemsSource = null;
            lstSelectedElements.ItemsSource = _selectedElements;
        }
        
        // 工作流操作开始事件
        private void WorkflowManager_ActionStarted(object sender, WorkflowActionEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 更新UI以反映当前正在执行的操作
                foreach (ActionListItem item in lstActions.Items)
                {
                    if (item.Action == e.Action)
                    {
                        lstActions.SelectedItem = item;
                        lstActions.ScrollIntoView(item);
                        break;
                    }
                }
            });
        }
        
        // 工作流操作完成事件
        private void WorkflowManager_ActionCompleted(object sender, WorkflowActionEventArgs e)
        {
            // 可以添加日志记录或其他完成后的处理
        }
        
        // 工作流操作失败事件
        private void WorkflowManager_ActionFailed(object sender, WorkflowErrorEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"操作执行失败: {e.Error.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
        
        // 根据操作类型创建属性编辑器
        private void UpdatePropertyPanel()
        {
            // 检查当前是否在UI线程，如果不是，则使用Dispatcher
            if (!CheckAccess())
            {
                Dispatcher.Invoke(UpdatePropertyPanel);
                return;
            }
            
            if (_currentEditingAction == null)
            {
                propertyPanel.Children.Clear();
                propertyPanel.Children.Add(new TextBlock 
                { 
                    Text = "请选择一个操作来编辑属性", 
                    Margin = new Thickness(5),
                    Foreground = Brushes.Gray
                });
                btnApplyChanges.IsEnabled = false;
                return;
            }
            
            btnApplyChanges.IsEnabled = true;
            propertyPanel.Children.Clear();
            
            // 通用属性
            AddTextBoxProperty("名称", _currentEditingAction.Name, value => _currentEditingAction.Name = value);
            AddTextBoxProperty("描述", _currentEditingAction.Description, value => _currentEditingAction.Description = value);
            
            // 特定类型的属性
            switch (_currentEditingAction)
            {
                case ClickElementAction clickAction:
                    AddElementProperty("目标元素", clickAction.TargetElement);
                    break;
                    
                case InputTextAction inputAction:
                    AddElementProperty("目标元素", inputAction.TargetElement);
                    AddTextBoxProperty("输入文本", inputAction.Text, value => inputAction.Text = value);
                    break;
                    
                case GetTextAction getTextAction:
                    AddElementProperty("目标元素", getTextAction.TargetElement);
                    break;
                    
                case NavigateAction navigateAction:
                    AddTextBoxProperty("网址", navigateAction.Url, value => navigateAction.Url = value);
                    break;
                    
                case WaitAction waitAction:
                    AddNumericProperty("等待时间 (毫秒)", waitAction.WaitTimeMilliseconds, 
                        value => waitAction.WaitTimeMilliseconds = value);
                    break;
                    
                case SubmitFormAction submitAction:
                    AddElementProperty("表单元素", submitAction.FormElement);
                    break;
                    
                case ExecuteJavaScriptAction jsAction:
                    AddElementProperty("目标元素", jsAction.TargetElement);
                    AddScriptProperty("JavaScript脚本", jsAction.ScriptTemplate, value => jsAction.ScriptTemplate = value);
                    AddTextBoxProperty("输入参数", jsAction.InputParameter, value => jsAction.InputParameter = value);
                    if (!string.IsNullOrEmpty(jsAction.Result))
                    {
                        AddReadOnlyTextProperty("执行结果", jsAction.Result);
                    }
                    break;
            }
        }
        
        // 添加文本框属性
        private void AddTextBoxProperty(string label, string initialValue, Action<string> setter)
        {
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 3) });
            
            var textBox = new TextBox 
            { 
                Text = initialValue ?? "",
                Padding = new Thickness(3),
                Tag = setter
            };
            
            container.Children.Add(textBox);
            propertyPanel.Children.Add(container);
        }
        
        // 添加数值属性
        private void AddNumericProperty(string label, int initialValue, Action<int> setter)
        {
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 3) });
            
            var textBox = new TextBox 
            { 
                Text = initialValue.ToString(),
                Padding = new Thickness(3)
            };
            
            textBox.PreviewTextInput += (s, e) => 
            {
                e.Handled = !int.TryParse(e.Text, out _);
            };
            
            textBox.Tag = new Action<string>(value => 
            {
                if (int.TryParse(value, out int result))
                    setter(result);
            });
            
            container.Children.Add(textBox);
            propertyPanel.Children.Add(container);
        }
        
        // 添加元素属性
        private void AddElementProperty(string label, ElementInfo element)
        {
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 3) });
            
            var panel = new DockPanel { LastChildFill = true };
            
            var textBlock = new TextBlock 
            { 
                Text = element?.ToString() ?? "未选择",
                Padding = new Thickness(3),
                Background = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 200
            };
            
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var selectButton = new Button 
            { 
                Content = "新选择", 
                Padding = new Thickness(3, 0, 3, 0),
                Margin = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = textBlock
            };
            
            var fromListButton = new Button
            {
                Content = "从列表选择",
                Padding = new Thickness(3, 0, 3, 0),
                Margin = new Thickness(0, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = textBlock
            };
            
            selectButton.Click += ElementSelectButton_Click;
            fromListButton.Click += ElementFromListButton_Click;
            
            buttonPanel.Children.Add(selectButton);
            buttonPanel.Children.Add(fromListButton);
            
            DockPanel.SetDock(buttonPanel, Dock.Right);
            panel.Children.Add(buttonPanel);
            panel.Children.Add(textBlock);
            
            container.Children.Add(panel);
            propertyPanel.Children.Add(container);
        }
        
        // 元素选择按钮点击事件
        private void ElementSelectButton_Click(object sender, RoutedEventArgs e)
        {
            _elementSelector.StartElementSelection();
        }
        
        // 从列表选择元素按钮点击事件
        private void ElementFromListButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TextBlock textBlock)
            {
                if (_selectedElements.Count == 0)
                {
                    MessageBox.Show("元素列表为空，请先选择一些元素", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // 创建选择窗口
                var dialog = new Window
                {
                    Title = "选择元素",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };
                
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                var listView = new ListView
                {
                    Margin = new Thickness(10),
                    SelectionMode = SelectionMode.Single
                };
                
                var gridView = new GridView();
                gridView.Columns.Add(new GridViewColumn { Header = "类型", Width = 60, DisplayMemberBinding = new Binding("TagName") });
                gridView.Columns.Add(new GridViewColumn { Header = "标识", Width = 280, DisplayMemberBinding = new Binding("DisplayText") });
                listView.View = gridView;
                
                // 绑定数据
                listView.ItemsSource = _selectedElements;
                
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };
                
                var okButton = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
                var cancelButton = new Button { Content = "取消", Width = 80 };
                
                okButton.Click += (s, args) =>
                {
                    if (listView.SelectedItem is ElementListItem item)
                    {
                        // 设置当前选中的元素
                        _selectedElement = item.Element;
                        
                        // 更新显示
                        textBlock.Text = _selectedElement.ToString();
                        
                        // 如果当前正在编辑一个操作，设置相应的属性
                        if (_currentEditingAction != null)
                        {
                            switch (_currentEditingAction)
                            {
                                case ClickElementAction clickAction:
                                    clickAction.TargetElement = _selectedElement;
                                    break;
                                case InputTextAction inputAction:
                                    inputAction.TargetElement = _selectedElement;
                                    break;
                                case GetTextAction getTextAction:
                                    getTextAction.TargetElement = _selectedElement;
                                    break;
                                case SubmitFormAction submitAction:
                                    submitAction.FormElement = _selectedElement;
                                    break;
                                case ExecuteJavaScriptAction jsAction:
                                    jsAction.TargetElement = _selectedElement;
                                    break;
                            }
                        }
                        
                        dialog.DialogResult = true;
                    }
                    else
                    {
                        MessageBox.Show("请选择一个元素", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                };
                
                cancelButton.Click += (s, args) => dialog.DialogResult = false;
                
                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                
                Grid.SetRow(listView, 0);
                Grid.SetRow(buttonPanel, 1);
                
                grid.Children.Add(listView);
                grid.Children.Add(buttonPanel);
                
                dialog.Content = grid;
                dialog.ShowDialog();
            }
        }
        
        // 应用属性更改
        private void ApplyPropertyChanges()
        {
            if (_currentEditingAction == null)
                return;
                
            foreach (var child in propertyPanel.Children)
            {
                if (child is StackPanel container)
                {
                    foreach (var item in container.Children)
                    {
                        if (item is TextBox textBox && textBox.Tag is Action<string> setter)
                        {
                            setter(textBox.Text);
                        }
                    }
                }
            }
            
            // 更新列表显示
            UpdateActionsList();
        }
        
        #region 事件处理程序
        
        // 选择元素按钮点击
        private void btnSelectElement_Click(object sender, RoutedEventArgs e)
        {
            _elementSelector.StartElementSelection();
            UpdateElementSelectionButtons();
        }
        
        // 停止选择元素按钮点击
        private void btnStopElementSelection_Click(object sender, RoutedEventArgs e)
        {
            _elementSelector.StopElementSelection();
            MessageBox.Show("已停止元素选择", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateElementSelectionButtons();
        }
        
        // 添加点击操作
        private void btnAddClick_Click(object sender, RoutedEventArgs e)
        {
            var action = new ClickElementAction();
            if (_selectedElement != null)
                action.TargetElement = _selectedElement;
                
            _workflowManager.AddAction(action);
            UpdateActionsList();
            
            // 选中新添加的项
            lstActions.SelectedIndex = _workflowManager.Actions.Count - 1;
        }
        
        // 添加输入操作
        private void btnAddInput_Click(object sender, RoutedEventArgs e)
        {
            var action = new InputTextAction();
            if (_selectedElement != null)
                action.TargetElement = _selectedElement;
                
            _workflowManager.AddAction(action);
            UpdateActionsList();
            
            // 选中新添加的项
            lstActions.SelectedIndex = _workflowManager.Actions.Count - 1;
        }
        
        // 添加等待操作
        private void btnAddWait_Click(object sender, RoutedEventArgs e)
        {
            var action = new WaitAction();
            _workflowManager.AddAction(action);
            UpdateActionsList();
            
            // 选中新添加的项
            lstActions.SelectedIndex = _workflowManager.Actions.Count - 1;
        }
        
        // 添加提交表单操作
        private void btnAddSubmit_Click(object sender, RoutedEventArgs e)
        {
            var action = new SubmitFormAction();
            if (_selectedElement != null)
                action.FormElement = _selectedElement;
                
            _workflowManager.AddAction(action);
            UpdateActionsList();
            
            // 选中新添加的项
            lstActions.SelectedIndex = _workflowManager.Actions.Count - 1;
        }
        
        // 添加JavaScript操作
        private void btnAddJavaScript_Click(object sender, RoutedEventArgs e)
        {
            var action = new ExecuteJavaScriptAction();
            if (_selectedElement != null)
                action.TargetElement = _selectedElement;
                
            _workflowManager.AddAction(action);
            UpdateActionsList();
            
            // 选中新添加的项
            lstActions.SelectedIndex = _workflowManager.Actions.Count - 1;
        }
        
        // 执行选中操作
        private async void btnRunAction_Click(object sender, RoutedEventArgs e)
        {
            if (lstActions.SelectedItem is ActionListItem item)
            {
                try
                {
                    await _workflowManager.ExecuteActionAsync(item.Action);
                    MessageBox.Show("操作执行完成", "执行结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"执行操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("请先选择一个操作", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        // 执行全部操作
        private async void btnRunAll_Click(object sender, RoutedEventArgs e)
        {
            if (_workflowManager.Actions.Count == 0)
            {
                MessageBox.Show("没有可执行的操作", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                await _workflowManager.ExecuteAllAsync();
                MessageBox.Show("所有操作执行完成", "执行结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // 保存工作流
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "工作流文件 (*.rwf)|*.rwf|所有文件 (*.*)|*.*",
                DefaultExt = "rwf",
                Title = "保存工作流"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    _workflowManager.SaveToFile(saveDialog.FileName);
                    MessageBox.Show("工作流保存成功", "保存结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 加载工作流
        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "工作流文件 (*.rwf)|*.rwf|所有文件 (*.*)|*.*",
                DefaultExt = "rwf",
                Title = "加载工作流"
            };
            
            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    _workflowManager.LoadFromFile(openDialog.FileName);
                    UpdateActionsList();
                    MessageBox.Show("工作流加载成功", "加载结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 操作列表选择变更
        private void lstActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstActions.SelectedItem is ActionListItem item)
            {
                _currentEditingAction = item.Action;
                UpdatePropertyPanel();
            }
            else
            {
                _currentEditingAction = null;
                UpdatePropertyPanel();
            }
        }
        
        // 上移菜单项点击
        private void MenuItemMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (lstActions.SelectedIndex > 0)
            {
                int index = lstActions.SelectedIndex;
                _workflowManager.MoveAction(index, index - 1);
                UpdateActionsList();
                lstActions.SelectedIndex = index - 1;
            }
        }
        
        // 下移菜单项点击
        private void MenuItemMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (lstActions.SelectedIndex < _workflowManager.Actions.Count - 1 && lstActions.SelectedIndex >= 0)
            {
                int index = lstActions.SelectedIndex;
                _workflowManager.MoveAction(index, index + 1);
                UpdateActionsList();
                lstActions.SelectedIndex = index + 1;
            }
        }
        
        // 移除菜单项点击
        private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstActions.SelectedItem is ActionListItem item)
            {
                _workflowManager.RemoveAction(item.Action);
                UpdateActionsList();
            }
        }
        
        // 执行菜单项点击
        private async void MenuItemExecute_Click(object sender, RoutedEventArgs e)
        {
            if (lstActions.SelectedItem is ActionListItem item)
            {
                try
                {
                    await _workflowManager.ExecuteActionAsync(item.Action);
                    MessageBox.Show("操作执行完成", "执行结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"执行操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 应用更改按钮点击
        private void btnApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            ApplyPropertyChanges();
        }
        
        // 更新元素选择相关按钮状态
        private void UpdateElementSelectionButtons()
        {
            // 检查当前是否在UI线程，如果不是，则使用Dispatcher
            if (!CheckAccess())
            {
                Dispatcher.Invoke(UpdateElementSelectionButtons);
                return;
            }
            
            // 在UI线程上执行
            btnSelectElement.IsEnabled = !_elementSelector.IsSelecting;
            btnStopElementSelection.IsEnabled = _elementSelector.IsSelecting;
        }
        
        // 元素列表选择变更事件
        private void lstSelectedElements_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstSelectedElements.SelectedItem is ElementListItem item)
            {
                _selectedElement = item.Element;
                
                // 如果当前正在编辑操作，自动填充元素
                if (_currentEditingAction != null)
                {
                    switch (_currentEditingAction)
                    {
                        case ClickElementAction clickAction:
                            clickAction.TargetElement = _selectedElement;
                            break;
                        case InputTextAction inputAction:
                            inputAction.TargetElement = _selectedElement;
                            break;
                        case GetTextAction getTextAction:
                            getTextAction.TargetElement = _selectedElement;
                            break;
                        case SubmitFormAction submitAction:
                            submitAction.FormElement = _selectedElement;
                            break;
                        case ExecuteJavaScriptAction jsAction:
                            jsAction.TargetElement = _selectedElement;
                            break;
                    }
                    
                    // 更新属性面板
                    UpdatePropertyPanel();
                }
            }
        }
        
        // 删除元素菜单项点击
        private void MenuItemRemoveElement_Click(object sender, RoutedEventArgs e)
        {
            if (lstSelectedElements.SelectedItem is ElementListItem item)
            {
                _selectedElements.Remove(item);
                UpdateElementsList();
                
                // 如果是当前选中的元素，也清除选中
                if (_selectedElement == item.Element)
                {
                    _selectedElement = null;
                }
            }
        }
        
        // 清空元素列表菜单项点击
        private void MenuItemClearElements_Click(object sender, RoutedEventArgs e)
        {
            ClearElementsList();
        }
        
        // 清空元素列表按钮点击
        private void btnClearElements_Click(object sender, RoutedEventArgs e)
        {
            ClearElementsList();
        }
        
        // 清空元素列表
        private void ClearElementsList()
        {
            // 检查当前是否在UI线程，如果不是，则使用Dispatcher
            if (!CheckAccess())
            {
                Dispatcher.Invoke(ClearElementsList);
                return;
            }
            
            _selectedElements.Clear();
            UpdateElementsList();
            _selectedElement = null;
        }
        
        // 添加脚本编辑属性
        private void AddScriptProperty(string label, string scriptContent, Action<string> setter)
        {
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 3) });
            
            var textBox = new TextBox 
            { 
                Text = scriptContent ?? "",
                Padding = new Thickness(3),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 200,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                Tag = setter
            };
            
            container.Children.Add(textBox);
            propertyPanel.Children.Add(container);
        }
        
        // 添加只读文本属性
        private void AddReadOnlyTextProperty(string label, string content)
        {
            var container = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            container.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 3) });
            
            var textBox = new TextBox 
            { 
                Text = content ?? "",
                Padding = new Thickness(3),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 100,
                IsReadOnly = true,
                Background = Brushes.LightYellow
            };
            
            container.Children.Add(textBox);
            propertyPanel.Children.Add(container);
        }
        
        #endregion
    }
    
    // 操作列表项类
    public class ActionListItem
    {
        public int Index { get; set; }
        public BrowserAction Action { get; set; }
        public string ActionType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    
    // 元素列表项类
    public class ElementListItem
    {
        public ElementInfo Element { get; set; }
        public string TagName => Element.TagName;
        public string DisplayText 
        { 
            get 
            {
                if (!string.IsNullOrEmpty(Element.Id))
                    return $"#{Element.Id}";
                else if (!string.IsNullOrEmpty(Element.Name))
                    return $"[name='{Element.Name}']";
                else
                    return Element.XPath;
            } 
        }
        
        public ElementListItem(ElementInfo element)
        {
            Element = element;
        }
    }
}