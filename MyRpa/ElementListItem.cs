using System;
using System.Windows;

namespace MyRpa
{
    /// <summary>
    /// 元素列表项类，用于在列表视图中显示元素信息
    /// </summary>
    public class ElementListItem
    {
        /// <summary>
        /// 获取或设置原始元素信息
        /// </summary>
        public ElementInfo Element { get; set; }
        
        /// <summary>
        /// 获取或设置元素标签名称
        /// </summary>
        public string TagName { get; set; }
        
        /// <summary>
        /// 获取或设置元素显示文本
        /// </summary>
        public string DisplayText { get; set; }
        
        /// <summary>
        /// 返回元素的字符串表示
        /// </summary>
        public override string ToString()
        {
            return DisplayText ?? Element?.ToString() ?? "未知元素";
        }
    }
} 