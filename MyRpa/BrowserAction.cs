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
        SubmitForm
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
}