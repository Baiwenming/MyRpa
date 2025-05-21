using System;
using System.Windows;
using System.Text;
using Newtonsoft.Json;

namespace MyRpa
{
    /// <summary>
    /// ����UIԪ����Ϣ�࣬�洢UI�Զ���Ԫ�ص�����
    /// </summary>
    [Serializable]
    public class DesktopElementInfo : ElementInfo
    {
        // ����Ԫ����������
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

        // ���ں�������Ԫ�ص��������
        public string[] SearchConditions { get; set; }

        // ����TagName�Է��ؿؼ�����
        public  string TagName => ControlType?.Replace("ControlType.", "");

        // ����XPath���ṩ������XPath�Ķ�λ·��
        public  string XPath => BuildLocatorPath();

        // ����Id�Է���AutomationId
        public  string Id => AutomationId;

        /// <summary>
        /// ����Ԫ�ض�λ·��������XPath��
        /// </summary>
        private string BuildLocatorPath()
        {
            var sb = new StringBuilder();

            // ��ӽ��̺ʹ�����Ϣ
            sb.Append($"[{ProcessName}:{ProcessId}]");

            // ��Ӵ��ڱ���
            if (!string.IsNullOrEmpty(WindowTitle))
                sb.Append($" \"{WindowTitle}\"");

            // ��ӿؼ����ͺ����ƻ�ID
            sb.Append($" > {TagName}");

            if (!string.IsNullOrEmpty(AutomationId))
                sb.Append($"[AutomationId='{AutomationId}']");
            else if (!string.IsNullOrEmpty(Name))
                sb.Append($"[Name='{Name}']");

            return sb.ToString();
        }

        /// <summary>
        /// ����Ԫ����Ϣ
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