using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace MyRpa
{
    public class ElementSelector
    {
        private ChromiumWebBrowser _browser;
        private bool _isSelecting = false;
        public event EventHandler<ElementSelectedEventArgs> ElementSelected;
        public event EventHandler SelectionCancelled;
        
        // 添加公共属性以检查是否正在选择元素
        public bool IsSelecting => _isSelecting;

        public ElementSelector(ChromiumWebBrowser browser)
        {
            _browser = browser;
        }

        // 开始选择元素
        public void StartElementSelection()
        {
            if (_isSelecting)
                return;

            _isSelecting = true;

            // 注入元素选择器脚本
            InjectElementSelectionScript();
        }

        // 停止选择元素
        public void StopElementSelection()
        {
            if (!_isSelecting)
                return;

            _isSelecting = false;

            // 执行停止选择的脚本
            ExecuteStopSelectionScript();
            
            // 触发选择取消事件
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectionCancelled?.Invoke(this, EventArgs.Empty);
            });
        }

        // 注入选择器脚本
        private async void InjectElementSelectionScript()
        {
            if (!_browser.IsBrowserInitialized || _browser.IsLoading)
            {
                MessageBox.Show("浏览器尚未初始化或正在加载中，请稍后再试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                _isSelecting = false;
                return;
            }

            try
            {
                // 注册消息处理器
                _browser.JavascriptMessageReceived += Browser_JavascriptMessageReceived;
            
                // 创建用于元素选择的JavaScript代码 - 使用更强的样式设置方法确保高亮显示
                string script = @"
                    (function() {
                        if (window.__rpaElementSelector) {
                            window.__rpaStartElementSelection();
                            return;
                        }
                        
                        window.__rpaElementSelector = true;
                        
                        // 保存原来的样式
                        const originalStyles = new Map();
                        
                        // 当前悬停的元素
                        let currentElement = null;
                        
                        // 鼠标移动事件处理
                        function handleMouseMove(e) {
                            if (currentElement) {
                                // 恢复之前元素的样式
                                restoreStyle(currentElement);
                            }
                            
                            // 获取当前鼠标下的元素
                            currentElement = e.target;
                            
                            // 保存并更改样式
                            saveAndChangeStyle(currentElement);
                            
                            // 阻止事件冒泡
                            e.stopPropagation();
                        }
                        
                        // 鼠标点击事件处理
                        function handleMouseClick(e) {
                            // 阻止默认行为和事件冒泡
                            e.preventDefault();
                            e.stopPropagation();
                            
                            // 获取元素的唯一标识符
                            const elementInfo = {
                                tagName: e.target.tagName,
                                id: e.target.id,
                                name: e.target.getAttribute('name'),
                                className: e.target.className,
                                type: e.target.getAttribute('type'),
                                value: e.target.value,
                                innerHTML: e.target.innerHTML,
                                xpath: getXPath(e.target),
                                cssSelector: getCssSelector(e.target)
                            };
                            
                            // 发送元素信息到C#
                            try {
                                console.log('选中元素:', elementInfo);
                                
                                if (window.cefSharp && typeof window.cefSharp.postMessage === 'function') {
                                    window.cefSharp.postMessage(JSON.stringify({
                                        type: 'elementSelected',
                                        element: elementInfo
                                    }));
                                } else {
                                    console.error('cefSharp对象或postMessage方法不可用');
                                    alert('无法与应用程序通信，请重试');
                                }
                            } catch(err) {
                                console.error('发送消息到C#时出错:', err);
                            }
                            
                            // 不再停止选择，继续选择模式
                            // stopSelection();  // 注释掉此行，使选择可以继续
                            
                            // 提供视觉反馈，短暂闪烁选中的元素
                            const originalOutline = e.target.style.getPropertyValue('outline');
                            const originalOutlinePriority = e.target.style.getPropertyPriority('outline');
                            
                            e.target.style.setProperty('outline', '3px solid green', 'important');
                            
                            setTimeout(() => {
                                if (originalOutline) {
                                    e.target.style.setProperty('outline', originalOutline, originalOutlinePriority);
                                } else {
                                    e.target.style.removeProperty('outline');
                                }
                                // 恢复红色高亮
                                if (currentElement === e.target) {
                                    saveAndChangeStyle(currentElement);
                                }
                            }, 500);
                            
                            return false;
                        }
                        
                        // 保存原样式并改变样式
                        function saveAndChangeStyle(element) {
                            if (!element) return;
                            
                            try {
                                // 保存原始样式
                                originalStyles.set(element, {
                                    outlineWidth: element.style.outlineWidth,
                                    outlineStyle: element.style.outlineStyle,
                                    outlineColor: element.style.outlineColor,
                                    backgroundColor: element.style.backgroundColor,
                                    border: element.style.border
                                });
                                
                                // 应用高亮样式 - 使用!important确保样式生效
                                element.style.setProperty('outline', '2px solid red', 'important');
                                element.style.setProperty('outline-style', 'solid', 'important');
                                element.style.setProperty('outline-width', '2px', 'important');
                                element.style.setProperty('outline-color', 'red', 'important');
                                element.style.setProperty('background-color', 'rgba(255, 0, 0, 0.1)', 'important');
                            } catch(err) {
                                console.error('设置元素样式时出错:', err);
                            }
                        }
                        
                        // 恢复原样式
                        function restoreStyle(element) {
                            if (!element) return;
                            
                            try {
                                const originalStyle = originalStyles.get(element);
                                if (originalStyle) {
                                    element.style.outlineWidth = originalStyle.outlineWidth;
                                    element.style.outlineStyle = originalStyle.outlineStyle;
                                    element.style.outlineColor = originalStyle.outlineColor;
                                    element.style.backgroundColor = originalStyle.backgroundColor;
                                    element.style.border = originalStyle.border;
                                }
                            } catch(err) {
                                console.error('恢复元素样式时出错:', err);
                            }
                        }
                        
                        // 获取元素的XPath
                        function getXPath(element) {
                            if (!element) return '';
                            
                            if (element.id) {
                                return `//*[@id='${element.id}']`;
                            }
                            
                            // 如果没有ID，使用相对路径
                            let path = '';
                            let current = element;
                            
                            while (current && current.nodeType === Node.ELEMENT_NODE) {
                                let index = 0;
                                let sibling = current.previousSibling;
                                
                                while (sibling) {
                                    if (sibling.nodeType === Node.ELEMENT_NODE && sibling.tagName === current.tagName) {
                                        index++;
                                    }
                                    sibling = sibling.previousSibling;
                                }
                                
                                const tagName = current.tagName.toLowerCase();
                                const position = index === 0 ? '' : `[${index + 1}]`;
                                
                                path = `/${tagName}${position}${path}`;
                                current = current.parentNode;
                            }
                            
                            return path;
                        }
                        
                        // 获取元素的CSS选择器
                        function getCssSelector(element) {
                            if (!element) return '';
                            
                            if (element.id) {
                                return `#${element.id}`;
                            }
                            
                            if (element.className && typeof element.className === 'string') {
                                const classes = element.className.trim().split(/\s+/);
                                if (classes.length > 0) {
                                    return `.${classes.join('.')}`;
                                }
                            }
                            
                            // 如果没有ID和类，使用标签名和属性
                            let selector = element.tagName.toLowerCase();
                            
                            // 添加name属性
                            if (element.name) {
                                selector += `[name='${element.name}']`;
                            }
                            
                            // 为input添加type
                            if (element.tagName.toLowerCase() === 'input' && element.type) {
                                selector += `[type='${element.type}']`;
                            }
                            
                            return selector;
                        }
                        
                        // 启动元素选择
                        function startSelection() {
                            console.log('开始元素选择...');
                            document.addEventListener('mousemove', handleMouseMove, true);
                            document.addEventListener('click', handleMouseClick, true);
                            document.body.style.cursor = 'crosshair';
                        }
                        
                        // 停止元素选择
                        function stopSelection() {
                            console.log('停止元素选择...');
                            document.removeEventListener('mousemove', handleMouseMove, true);
                            document.removeEventListener('click', handleMouseClick, true);
                            document.body.style.cursor = 'default';
                            
                            // 恢复当前元素的样式
                            if (currentElement) {
                                restoreStyle(currentElement);
                                currentElement = null;
                            }
                            
                            // 清除所有保存的样式
                            originalStyles.clear();
                        }
                        
                        window.__rpaStartElementSelection = startSelection;
                        window.__rpaStopElementSelection = stopSelection;
                        
                        // 默认启动选择
                        startSelection();
                    })();
                ";

                // 执行脚本
                var response = await _browser.EvaluateScriptAsync(script);
                if (!response.Success)
                {
                    MessageBox.Show($"执行元素选择脚本失败: {response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    _isSelecting = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注入选择器脚本时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _isSelecting = false;
            }
        }

        // 停止选择脚本
        private async void ExecuteStopSelectionScript()
        {
            try
            {
                Console.WriteLine("正在停止元素选择...");
                
                // 确保浏览器已初始化
                if (!_browser.IsBrowserInitialized)
                {
                    Console.WriteLine("浏览器未初始化，无需执行停止脚本");
                    return;
                }
                
                // 执行停止选择的JavaScript - 使用自调用函数以解决return语法错误
                string script = @"
                (function() {
                    if (window.__rpaStopElementSelection) { 
                        window.__rpaStopElementSelection(); 
                        console.log('已执行停止选择脚本'); 
                        return true; 
                    } else { 
                        console.log('没有找到停止选择函数'); 
                        return false; 
                    }
                })();";
                
                var response = await _browser.EvaluateScriptAsync(script);
                
                if (response.Success)
                {
                    Console.WriteLine("停止选择脚本执行结果: " + (response.Result?.ToString() ?? "无返回值"));
                }
                else
                {
                    Console.WriteLine("停止选择脚本执行失败: " + response.Message);
                }
                
                // 移除消息处理器
                _browser.JavascriptMessageReceived -= Browser_JavascriptMessageReceived;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止选择脚本时出错：{ex.Message}");
                MessageBox.Show($"停止选择脚本时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 处理从JavaScript接收到的消息
        private void Browser_JavascriptMessageReceived(object sender, JavascriptMessageReceivedEventArgs e)
        {
            try
            {
                if (e.Message is string message)
                {
                    // 将JSON消息转换为.NET对象
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(message);
                    
                    if (data.type == "elementSelected")
                    {
                        // 创建元素信息对象
                        var elementInfo = new ElementInfo
                        {
                            TagName = data.element.tagName,
                            Id = data.element.id,
                            Name = data.element.name,
                            ClassName = data.element.className,
                            Type = data.element.type,
                            Value = data.element.value,
                            InnerHTML = data.element.innerHTML,
                            XPath = data.element.xpath,
                            CssSelector = data.element.cssSelector
                        };
                        
                        // 确保在UI线程上触发事件
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 触发元素选择事件
                            ElementSelected?.Invoke(this, new ElementSelectedEventArgs(elementInfo));
                        });
                        
                        // 不再重置选择状态，使选择可以继续
                        // _isSelecting = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理JavaScript消息时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 元素选择事件参数
    public class ElementSelectedEventArgs : EventArgs
    {
        public ElementInfo SelectedElement { get; }

        public ElementSelectedEventArgs(ElementInfo element)
        {
            SelectedElement = element;
        }
    }

    // 元素信息类
    public class ElementInfo
    {
        public string TagName { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string InnerHTML { get; set; }
        public string XPath { get; set; }
        public string CssSelector { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(DisplayName))
                return DisplayName;
                
            if (!string.IsNullOrEmpty(Id))
                return $"{TagName}#{Id}";
                
            if (!string.IsNullOrEmpty(Name))
                return $"{TagName}[name='{Name}']";
                
            return XPath;
        }
    }
} 