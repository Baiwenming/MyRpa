using System;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;

namespace MyRpa
{
    // 操作类型枚举
    public enum ActionType
    {
        Click,
        Input,
        GetText,
        Select,
        Navigate,
        Wait,
        SubmitForm,
        JavaScript
    }

    // 浏览器操作基类
    public abstract class BrowserAction
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ActionType ActionType { get; protected set; }
        
        public abstract Task ExecuteAsync(ChromiumWebBrowser browser);
    }

    // 点击元素操作
    public class ClickElementAction : BrowserAction
    {
        public ElementInfo TargetElement { get; set; }
        
        public ClickElementAction()
        {
            ActionType = ActionType.Click;
            Name = "点击元素";
        }
        
        public override async Task ExecuteAsync(ChromiumWebBrowser browser)
        {
            if (TargetElement == null)
                throw new InvalidOperationException("没有设置目标元素");

            string script = "";
                
            // 根据不同选择器类型尝试查找元素
            if (!string.IsNullOrEmpty(TargetElement.Id))
            {
                script = $"document.getElementById('{TargetElement.Id}').click();";
            }
            else if (!string.IsNullOrEmpty(TargetElement.CssSelector))
            {
                script = $"document.querySelector('{TargetElement.CssSelector}').click();";
            }
            else if (!string.IsNullOrEmpty(TargetElement.XPath))
            {
                script = @"
                    (function() {
                        const element = document.evaluate(
                            """+TargetElement.XPath+@""", 
                            document, 
                            null, 
                            XPathResult.FIRST_ORDERED_NODE_TYPE, 
                            null
                        ).singleNodeValue;
                        
                        if (element) {
                            element.click();
                            return true;
                        }
                        return false;
                    })();
                ";
            }
            
            var response = await browser.EvaluateScriptAsync(script);
        }
    }
    
    // 输入文本操作
    public class InputTextAction : BrowserAction
    {
        public ElementInfo TargetElement { get; set; }
        public string Text { get; set; }
        
        public InputTextAction()
        {
            ActionType = ActionType.Input;
            Name = "输入文本";
        }
        
        public override async Task ExecuteAsync(ChromiumWebBrowser browser)
        {
            if (TargetElement == null)
                throw new InvalidOperationException("没有设置目标元素");
                
            if (string.IsNullOrEmpty(Text))
                Text = "";
                
            string script = "";
            
            // 根据不同选择器类型尝试查找元素并设置值
            if (!string.IsNullOrEmpty(TargetElement.Id))
            {
                script = $@"
                    (function() {{
                        const element = document.getElementById('{TargetElement.Id}');
                        if (element) {{
                            element.value = '{Text.Replace("'", "\\'")}';
                            // 触发输入事件
                            const event = new Event('input', {{ bubbles: true }});
                            element.dispatchEvent(event);
                            return true;
                        }}
                        return false;
                    }})();
                ";
            }
            else if (!string.IsNullOrEmpty(TargetElement.CssSelector))
            {
                script = $@"
                    (function() {{
                        const element = document.querySelector('{TargetElement.CssSelector}');
                        if (element) {{
                            element.value = '{Text.Replace("'", "\\'")}';
                            // 触发输入事件
                            const event = new Event('input', {{ bubbles: true }});
                            element.dispatchEvent(event);
                            return true;
                        }}
                        return false;
                    }})();
                ";
            }
            else if (!string.IsNullOrEmpty(TargetElement.XPath))
            {
                script = $@"
                    (function() {{
                        const element = document.evaluate(
                            ""{TargetElement.XPath}"", 
                            document, 
                            null, 
                            XPathResult.FIRST_ORDERED_NODE_TYPE, 
                            null
                        ).singleNodeValue;
                        
                        if (element) {{
                            element.value = '{Text.Replace("'", "\\'")}';
                            // 触发输入事件
                            const event = new Event('input', {{ bubbles: true }});
                            element.dispatchEvent(event);
                            return true;
                        }}
                        return false;
                    }})();
                ";
            }
            
            var response = await browser.EvaluateScriptAsync(script);
        }
    }
    
    // 获取文本操作
    public class GetTextAction : BrowserAction
    {
        public ElementInfo TargetElement { get; set; }
        public string ExtractedText { get; private set; }
        
        public GetTextAction()
        {
            ActionType = ActionType.GetText;
            Name = "获取文本";
        }
        
        public override async Task ExecuteAsync(ChromiumWebBrowser browser)
        {
            if (TargetElement == null)
                throw new InvalidOperationException("没有设置目标元素");
                
            string script = "";
            
            // 根据不同选择器类型尝试查找元素并获取文本
            if (!string.IsNullOrEmpty(TargetElement.Id))
            {
                script = $@"
                    (function() {{
                        const element = document.getElementById('{TargetElement.Id}');
                        if (element) {{
                            // 针对不同类型元素获取文本
                            if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {{
                                return element.value;
                            }} else {{
                                return element.textContent;
                            }}
                        }}
                        return null;
                    }})();
                ";
            }
            else if (!string.IsNullOrEmpty(TargetElement.CssSelector))
            {
                script = $@"
                    (function() {{
                        const element = document.querySelector('{TargetElement.CssSelector}');
                        if (element) {{
                            // 针对不同类型元素获取文本
                            if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {{
                                return element.value;
                            }} else {{
                                return element.textContent;
                            }}
                        }}
                        return null;
                    }})();
                ";
            }
            else if (!string.IsNullOrEmpty(TargetElement.XPath))
            {
                script = $@"
                    (function() {{
                        const element = document.evaluate(
                            ""{TargetElement.XPath}"", 
                            document, 
                            null, 
                            XPathResult.FIRST_ORDERED_NODE_TYPE, 
                            null
                        ).singleNodeValue;
                        
                        if (element) {{
                            // 针对不同类型元素获取文本
                            if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {{
                                return element.value;
                            }} else {{
                                return element.textContent;
                            }}
                        }}
                        return null;
                    }})();
                ";
            }
            
            var response = await browser.EvaluateScriptAsync(script);
            
            if (response.Success && response.Result != null)
            {
                ExtractedText = response.Result.ToString();
            }
            else
            {
                ExtractedText = null;
            }
        }
    }
    
    // 导航操作
    public class NavigateAction : BrowserAction
    {
        public string Url { get; set; }
        
        public NavigateAction()
        {
            ActionType = ActionType.Navigate;
            Name = "导航到网址";
        }
        
        public override async Task ExecuteAsync(ChromiumWebBrowser browser)
        {
            if (string.IsNullOrEmpty(Url))
                throw new InvalidOperationException("没有设置目标URL");
                
            // 确保URL格式正确
            if (!Url.StartsWith("http://") && !Url.StartsWith("https://"))
            {
                Url = "https://" + Url;
            }
            
            browser.Address = Url;
            
            // 等待导航完成
            var tcs = new TaskCompletionSource<bool>();
            
            EventHandler<FrameLoadEndEventArgs> handler = null;
            handler = (s, e) => {
                if (e.Frame.IsMain)
                {
                    browser.FrameLoadEnd -= handler;
                    tcs.SetResult(true);
                }
            };
            
            browser.FrameLoadEnd += handler;
            
            // 设置超时
            var timeoutTask = Task.Delay(30000);
            
            if (await Task.WhenAny(tcs.Task, timeoutTask) == timeoutTask)
            {
                browser.FrameLoadEnd -= handler;
                throw new TimeoutException("导航操作超时");
            }
        }
    }
    
    // 等待操作
    public class WaitAction : BrowserAction
    {
        public int WaitTimeMilliseconds { get; set; }
        
        public WaitAction()
        {
            ActionType = ActionType.Wait;
            Name = "等待";
            WaitTimeMilliseconds = 1000; // 默认1秒
        }
        
        public override async Task ExecuteAsync(ChromiumWebBrowser browser)
        {
            await Task.Delay(WaitTimeMilliseconds);
        }
    }
    
    // 提交表单操作
    public class SubmitFormAction : BrowserAction
    {
        public ElementInfo FormElement { get; set; }
        
        public SubmitFormAction()
        {
            ActionType = ActionType.SubmitForm;
            Name = "提交表单";
        }
        
        public override async Task ExecuteAsync(ChromiumWebBrowser browser)
        {
            string script;
            
            if (FormElement != null && !string.IsNullOrEmpty(FormElement.Id))
            {
                script = $"document.getElementById('{FormElement.Id}').submit();";
            }
            else if (FormElement != null && !string.IsNullOrEmpty(FormElement.CssSelector))
            {
                script = $"document.querySelector('{FormElement.CssSelector}').submit();";
            }
            else if (FormElement != null && !string.IsNullOrEmpty(FormElement.XPath))
            {
                script = @"
                    (function() {
                        const form = document.evaluate(
                            """+FormElement.XPath+@""", 
                            document, 
                            null, 
                            XPathResult.FIRST_ORDERED_NODE_TYPE, 
                            null
                        ).singleNodeValue;
                        
                        if (form) {
                            form.submit();
                            return true;
                        }
                        return false;
                    })();
                ";
            }
            else
            {
                // 如果没有指定表单，尝试提交当前表单
                script = @"
                    (function() {
                        // 获取活动元素
                        const activeElement = document.activeElement;
                        
                        // 如果活动元素是表单的一部分，找到并提交它的表单
                        if (activeElement && activeElement.form) {
                            activeElement.form.submit();
                            return true;
                        }
                        
                        // 否则尝试查找页面上的第一个表单并提交
                        const form = document.querySelector('form');
                        if (form) {
                            form.submit();
                            return true;
                        }
                        
                        return false;
                    })();
                ";
            }
            
            await browser.EvaluateScriptAsync(script);
        }
    }

    // 自定义JavaScript脚本执行操作
    public class ExecuteJavaScriptAction : BrowserAction
    {
        public ElementInfo TargetElement { get; set; }
        public string ScriptTemplate { get; set; } = @"function (element, input) {
    // 在此处编写您的Javascript代码
    // element表示选择的操作目标(HTML元素)
    // input表示输入的参数(字符串)
    
    // 示例1: 获取元素的所有属性
    if (input === 'getAllProperties') {
        var result = {};
        for (var key in element) {
            try {
                if (typeof element[key] !== 'function' && typeof element[key] !== 'object') {
                    result[key] = element[key];
                }
            } catch (e) { }
        }
        return JSON.stringify(result, null, 2);
    }
    
    // 示例2: 获取元素计算后的样式
    if (input === 'getComputedStyle') {
        var styles = window.getComputedStyle(element);
        var result = {};
        for (var i = 0; i < styles.length; i++) {
            var prop = styles[i];
            result[prop] = styles.getPropertyValue(prop);
        }
        return JSON.stringify(result, null, 2);
    }
    
    // 示例3: 获取元素在页面中的位置
    if (input === 'getPosition') {
        var rect = element.getBoundingClientRect();
        return JSON.stringify({
            top: rect.top + window.scrollY,
            left: rect.left + window.scrollX,
            width: rect.width,
            height: rect.height
        }, null, 2);
    }
    
    // 示例4: 获取元素的HTML内容
    if (input === 'getHTML') {
        return element.outerHTML;
    }
    
    // 示例5: 模拟点击事件(带有视觉反馈)
    if (input === 'simulateClick') {
        // 保存原始样式
        var originalBackground = element.style.backgroundColor;
        var originalTransition = element.style.transition;
        
        // 设置高亮效果
        element.style.transition = 'background-color 0.3s';
        element.style.backgroundColor = '#ffcc00';
        
        // 延迟后执行点击并恢复样式
        setTimeout(function() {
            element.click();
            element.style.backgroundColor = originalBackground;
            element.style.transition = originalTransition;
        }, 300);
        
        return '已模拟点击事件';
    }
    
    // 示例6: 查找所有子元素
    if (input === 'findChildren') {
        var children = [];
        for (var i = 0; i < element.children.length; i++) {
            var child = element.children[i];
            children.push({
                tagName: child.tagName,
                id: child.id,
                className: child.className,
                text: child.textContent.trim().substring(0, 50) + (child.textContent.length > 50 ? '...' : '')
            });
        }
        return JSON.stringify(children, null, 2);
    }
    
    // 示例7: 表单元素值设置和获取
    if (input && input.startsWith('setValue:')) {
        var newValue = input.substring(9);
        if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA' || element.tagName === 'SELECT') {
            var oldValue = element.value;
            element.value = newValue;
            // 触发change事件
            var event = new Event('change', { bubbles: true });
            element.dispatchEvent(event);
            return JSON.stringify({
                success: true, 
                oldValue: oldValue, 
                newValue: newValue
            }, null, 2);
        } else {
            return JSON.stringify({
                success: false,
                error: '目标元素不是表单元素'
            }, null, 2);
        }
    }
    
    // 默认行为: 返回元素的基本信息
    return JSON.stringify({
        tagName: element.tagName,
        id: element.id,
        className: element.className,
        name: element.getAttribute('name'),
        value: element.value,
        textContent: element.textContent.trim().substring(0, 100) + (element.textContent.length > 100 ? '...' : ''),
        isVisible: element.offsetParent !== null,
        attributes: (function() {
            var attrs = {};
            for (var i = 0; i < element.attributes.length; i++) {
                var attr = element.attributes[i];
                attrs[attr.name] = attr.value;
            }
            return attrs;
        })()
    }, null, 2);
}";
        public string InputParameter { get; set; }
        public string Result { get; private set; }
        
        public ExecuteJavaScriptAction()
        {
            ActionType = ActionType.JavaScript;
            Name = "执行JavaScript";
            Description = "执行自定义JavaScript脚本";
        }
        
        public override async Task ExecuteAsync(ChromiumWebBrowser browser)
        {
            if (TargetElement == null)
                throw new InvalidOperationException("没有设置目标元素");
                
            string getElementScript = "";
            
            // 根据不同选择器类型查找元素
            if (!string.IsNullOrEmpty(TargetElement.Id))
            {
                getElementScript = $"document.getElementById('{TargetElement.Id}')";
            }
            else if (!string.IsNullOrEmpty(TargetElement.CssSelector))
            {
                getElementScript = $"document.querySelector('{TargetElement.CssSelector}')";
            }
            else if (!string.IsNullOrEmpty(TargetElement.XPath))
            {
                getElementScript = $"document.evaluate(\"{TargetElement.XPath}\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue";
            }
            else
            {
                throw new InvalidOperationException("无法确定元素选择器");
            }
            
            // 构建完整的执行脚本
            string script = $@"
            (function() {{
                try {{
                    const element = {getElementScript};
                    if (!element) {{
                        return '找不到目标元素';
                    }}
                    
                    const input = {(string.IsNullOrEmpty(InputParameter) ? "null" : $"'{InputParameter.Replace("'", "\\'")}'")};
                    const userFunction = {ScriptTemplate};
                    
                    return userFunction(element, input);
                }} catch (error) {{
                    return '执行脚本时出错: ' + error.message;
                }}
            }})();
            ";
            
            var response = await browser.EvaluateScriptAsync(script);
            
            if (response.Success && response.Result != null)
            {
                Result = response.Result.ToString();
                
                // 添加弹框显示结果（使用自定义模态弹窗代替alert）
                string modalScript = $@"
                (function() {{
                    try {{
                        const resultContent = {(string.IsNullOrEmpty(Result) ? "'无结果'" : $"'{Result.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r")}'")};
                        
                        // 创建模态弹窗
                        const modal = document.createElement('div');
                        modal.style.position = 'fixed';
                        modal.style.left = '0';
                        modal.style.top = '0';
                        modal.style.width = '100%';
                        modal.style.height = '100%';
                        modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
                        modal.style.zIndex = '10000';
                        modal.style.display = 'flex';
                        modal.style.justifyContent = 'center';
                        modal.style.alignItems = 'center';
                        
                        // 创建内容容器
                        const content = document.createElement('div');
                        content.style.backgroundColor = 'white';
                        content.style.borderRadius = '5px';
                        content.style.padding = '20px';
                        content.style.width = '80%';
                        content.style.maxWidth = '800px';
                        content.style.maxHeight = '80%';
                        content.style.overflowY = 'auto';
                        content.style.boxShadow = '0 4px 8px rgba(0,0,0,0.2)';
                        content.style.position = 'relative';
                        
                        // 添加标题
                        const title = document.createElement('h3');
                        title.textContent = '脚本执行结果';
                        title.style.margin = '0 0 10px 0';
                        title.style.borderBottom = '1px solid #eee';
                        title.style.paddingBottom = '10px';
                        content.appendChild(title);
                        
                        // 添加结果内容
                        const pre = document.createElement('pre');
                        pre.style.margin = '10px 0';
                        pre.style.padding = '10px';
                        pre.style.backgroundColor = '#f5f5f5';
                        pre.style.border = '1px solid #ddd';
                        pre.style.borderRadius = '3px';
                        pre.style.maxHeight = '400px';
                        pre.style.overflowY = 'auto';
                        pre.style.whiteSpace = 'pre-wrap';
                        pre.style.wordBreak = 'break-word';
                        pre.textContent = resultContent;
                        content.appendChild(pre);
                        
                        // 添加关闭按钮
                        const closeBtn = document.createElement('button');
                        closeBtn.textContent = '关闭';
                        closeBtn.style.padding = '8px 16px';
                        closeBtn.style.backgroundColor = '#4CAF50';
                        closeBtn.style.color = 'white';
                        closeBtn.style.border = 'none';
                        closeBtn.style.borderRadius = '4px';
                        closeBtn.style.cursor = 'pointer';
                        closeBtn.style.marginTop = '10px';
                        closeBtn.onclick = function() {{
                            document.body.removeChild(modal);
                        }};
                        content.appendChild(closeBtn);
                        
                        // 添加到页面
                        modal.appendChild(content);
                        document.body.appendChild(modal);
                        
                        // 点击外部关闭
                        modal.onclick = function(event) {{
                            if (event.target === modal) {{
                                document.body.removeChild(modal);
                            }}
                        }};
                        
                        return true;
                    }} catch (error) {{
                        console.error('显示结果弹框时出错:', error);
                        alert('显示结果弹框时出错: ' + error.message);
                        return false;
                    }}
                }})();
                ";
                
                await browser.EvaluateScriptAsync(modalScript);
            }
            else
            {
                Result = $"执行失败: {response.Message}";
                
                // 添加错误弹框
                string errorScript = $@"
                (function() {{
                    try {{
                        // 创建错误模态弹窗
                        const modal = document.createElement('div');
                        modal.style.position = 'fixed';
                        modal.style.left = '0';
                        modal.style.top = '0';
                        modal.style.width = '100%';
                        modal.style.height = '100%';
                        modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
                        modal.style.zIndex = '10000';
                        modal.style.display = 'flex';
                        modal.style.justifyContent = 'center';
                        modal.style.alignItems = 'center';
                        
                        // 创建内容容器
                        const content = document.createElement('div');
                        content.style.backgroundColor = 'white';
                        content.style.borderRadius = '5px';
                        content.style.padding = '20px';
                        content.style.width = '80%';
                        content.style.maxWidth = '600px';
                        content.style.boxShadow = '0 4px 8px rgba(0,0,0,0.2)';
                        content.style.position = 'relative';
                        
                        // 添加标题
                        const title = document.createElement('h3');
                        title.textContent = '脚本执行失败';
                        title.style.margin = '0 0 10px 0';
                        title.style.color = '#d9534f';
                        title.style.borderBottom = '1px solid #eee';
                        title.style.paddingBottom = '10px';
                        content.appendChild(title);
                        
                        // 添加错误信息
                        const pre = document.createElement('pre');
                        pre.style.margin = '10px 0';
                        pre.style.padding = '10px';
                        pre.style.backgroundColor = '#f5f5f5';
                        pre.style.border = '1px solid #ddd';
                        pre.style.borderRadius = '3px';
                        pre.style.color = '#d9534f';
                        pre.style.whiteSpace = 'pre-wrap';
                        pre.style.wordBreak = 'break-word';
                        pre.textContent = '{response.Message.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r")}';
                        content.appendChild(pre);
                        
                        // 添加关闭按钮
                        const closeBtn = document.createElement('button');
                        closeBtn.textContent = '关闭';
                        closeBtn.style.padding = '8px 16px';
                        closeBtn.style.backgroundColor = '#d9534f';
                        closeBtn.style.color = 'white';
                        closeBtn.style.border = 'none';
                        closeBtn.style.borderRadius = '4px';
                        closeBtn.style.cursor = 'pointer';
                        closeBtn.style.marginTop = '10px';
                        closeBtn.onclick = function() {{
                            document.body.removeChild(modal);
                        }};
                        content.appendChild(closeBtn);
                        
                        // 添加到页面
                        modal.appendChild(content);
                        document.body.appendChild(modal);
                        
                        // 点击外部关闭
                        modal.onclick = function(event) {{
                            if (event.target === modal) {{
                                document.body.removeChild(modal);
                            }}
                        }};
                        
                        return true;
                    }} catch (error) {{
                        console.error('显示错误弹框时出错:', error);
                        alert('脚本执行失败:\\n{response.Message.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r")}');
                        return false;
                    }}
                }})();
                ";
                
                await browser.EvaluateScriptAsync(errorScript);
            }
        }
    }
}