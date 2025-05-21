using System;
using System.Windows;
using System.Text;
using Newtonsoft.Json;

namespace MyRpa
{
    /// <summary>
    /// 桌面UI元素信息类，存储UI自动化元素的属性
    /// </summary>
    [Serializable]
    public class DesktopElementInfo : ElementInfo
    {
        // 桌面元素特有属性
        public string AutomationId { get; set; }
        public string ControlType { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; }
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public Rect BoundingRectangle { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsOffscreen { get; set; }
        
        // 用于后续查找元素的条件组合
        public string[] SearchConditions { get; set; }
        
        // 覆盖TagName以返回控件类型
        public override string TagName => ControlType?.Replace("ControlType.", "");
        
        // 覆盖XPath以提供类似于XPath的定位路径
        public override string XPath => BuildLocatorPath();
        
        // 覆盖Id以返回AutomationId
        public override string Id => AutomationId;
        
        /// <summary>
        /// 构建元素定位路径（类似XPath）
        /// </summary>
        private string BuildLocatorPath()
        {
            var sb = new StringBuilder();
            
            // 添加进程和窗口信息
            sb.Append($"[{ProcessName}:{ProcessId}]");
            
            // 添加窗口标题
            if (!string.IsNullOrEmpty(WindowTitle))
                sb.Append($" \"{WindowTitle}\"");
                
            // 添加控件类型和名称或ID
            sb.Append($" > {TagName}");
            
            if (!string.IsNullOrEmpty(AutomationId))
                sb.Append($"[AutomationId='{AutomationId}']");
            else if (!string.IsNullOrEmpty(Name))
                sb.Append($"[Name='{Name}']");
                
            return sb.ToString();
        }
        
        /// <summary>
        /// 描述元素信息
        /// </summary>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
                return $"{TagName}: {Name}";
            else if (!string.IsNullOrEmpty(AutomationId))
                return $"{TagName}: #{AutomationId}";
            else
                return $"{TagName} [{ProcessName}]";
        }
    }
} 