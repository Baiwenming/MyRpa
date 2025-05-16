using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CefSharp.Wpf;

namespace MyRpa
{
    public class WorkflowManager
    {
        private ChromiumWebBrowser _browser;
        public ObservableCollection<BrowserAction> Actions { get; private set; }
        public event EventHandler<WorkflowEventArgs> WorkflowStarted;
        public event EventHandler<WorkflowEventArgs> WorkflowCompleted;
        public event EventHandler<WorkflowActionEventArgs> ActionStarted;
        public event EventHandler<WorkflowActionEventArgs> ActionCompleted;
        public event EventHandler<WorkflowErrorEventArgs> ActionFailed;
        
        public bool IsRunning { get; private set; }
        
        public WorkflowManager(ChromiumWebBrowser browser)
        {
            _browser = browser;
            Actions = new ObservableCollection<BrowserAction>();
        }
        
        // 添加操作
        public void AddAction(BrowserAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            Actions.Add(action);
        }
        
        // 移除操作
        public bool RemoveAction(BrowserAction action)
        {
            return Actions.Remove(action);
        }
        
        // 移动操作
        public void MoveAction(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Actions.Count)
                throw new ArgumentOutOfRangeException(nameof(oldIndex));
                
            if (newIndex < 0 || newIndex >= Actions.Count)
                throw new ArgumentOutOfRangeException(nameof(newIndex));
                
            var action = Actions[oldIndex];
            Actions.RemoveAt(oldIndex);
            Actions.Insert(newIndex, action);
        }
        
        // 清空所有操作
        public void ClearActions()
        {
            Actions.Clear();
        }
        
        // 执行单个操作
        public async Task ExecuteActionAsync(BrowserAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            ActionStarted?.Invoke(this, new WorkflowActionEventArgs(action));
            
            try
            {
                await action.ExecuteAsync(_browser);
                ActionCompleted?.Invoke(this, new WorkflowActionEventArgs(action));
            }
            catch (Exception ex)
            {
                ActionFailed?.Invoke(this, new WorkflowErrorEventArgs(action, ex));
                throw;
            }
        }
        
        // 执行所有操作
        public async Task ExecuteAllAsync()
        {
            if (IsRunning)
                throw new InvalidOperationException("工作流已经在运行中");
                
            if (Actions.Count == 0)
                return;
                
            IsRunning = true;
            WorkflowStarted?.Invoke(this, new WorkflowEventArgs(Actions.Count));
            
            try
            {
                for (int i = 0; i < Actions.Count; i++)
                {
                    var action = Actions[i];
                    
                    ActionStarted?.Invoke(this, new WorkflowActionEventArgs(action, i, Actions.Count));
                    
                    try
                    {
                        await action.ExecuteAsync(_browser);
                        ActionCompleted?.Invoke(this, new WorkflowActionEventArgs(action, i, Actions.Count));
                    }
                    catch (Exception ex)
                    {
                        ActionFailed?.Invoke(this, new WorkflowErrorEventArgs(action, ex, i));
                        throw;
                    }
                }
                
                WorkflowCompleted?.Invoke(this, new WorkflowEventArgs(Actions.Count));
            }
            finally
            {
                IsRunning = false;
            }
        }
        
        // 保存到文件
        public void SaveToFile(string filePath)
        {
            // 将工作流序列化为JSON并保存到文件
            var serializableActions = new List<object>();
            
            foreach (var action in Actions)
            {
                var actionData = new Dictionary<string, object>
                {
                    ["Type"] = action.GetType().Name,
                    ["Name"] = action.Name,
                    ["Description"] = action.Description
                };
                
                // 根据不同的操作类型添加特定属性
                switch (action)
                {
                    case ClickElementAction clickAction:
                        actionData["TargetElement"] = clickAction.TargetElement;
                        break;
                    case InputTextAction inputAction:
                        actionData["TargetElement"] = inputAction.TargetElement;
                        actionData["Text"] = inputAction.Text;
                        break;
                    case GetTextAction getTextAction:
                        actionData["TargetElement"] = getTextAction.TargetElement;
                        break;
                    case NavigateAction navigateAction:
                        actionData["Url"] = navigateAction.Url;
                        break;
                    case WaitAction waitAction:
                        actionData["WaitTimeMilliseconds"] = waitAction.WaitTimeMilliseconds;
                        break;
                    case SubmitFormAction submitAction:
                        actionData["FormElement"] = submitAction.FormElement;
                        break;
                }
                
                serializableActions.Add(actionData);
            }
            
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(serializableActions, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
        }
        
        // 从文件加载
        public void LoadFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException("找不到指定的工作流文件", filePath);
                
            string json = System.IO.File.ReadAllText(filePath);
            var actionDataList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            
            Actions.Clear();
            
            foreach (var actionData in actionDataList)
            {
                string typeName = actionData["Type"].ToString();
                BrowserAction action = null;
                
                // 根据类型创建相应的操作对象
                switch (typeName)
                {
                    case nameof(ClickElementAction):
                        action = new ClickElementAction
                        {
                            TargetElement = Newtonsoft.Json.JsonConvert.DeserializeObject<ElementInfo>(
                                actionData["TargetElement"].ToString())
                        };
                        break;
                    case nameof(InputTextAction):
                        action = new InputTextAction
                        {
                            TargetElement = Newtonsoft.Json.JsonConvert.DeserializeObject<ElementInfo>(
                                actionData["TargetElement"].ToString()),
                            Text = actionData["Text"].ToString()
                        };
                        break;
                    case nameof(GetTextAction):
                        action = new GetTextAction
                        {
                            TargetElement = Newtonsoft.Json.JsonConvert.DeserializeObject<ElementInfo>(
                                actionData["TargetElement"].ToString())
                        };
                        break;
                    case nameof(NavigateAction):
                        action = new NavigateAction
                        {
                            Url = actionData["Url"].ToString()
                        };
                        break;
                    case nameof(WaitAction):
                        action = new WaitAction
                        {
                            WaitTimeMilliseconds = Convert.ToInt32(actionData["WaitTimeMilliseconds"])
                        };
                        break;
                    case nameof(SubmitFormAction):
                        action = new SubmitFormAction
                        {
                            FormElement = Newtonsoft.Json.JsonConvert.DeserializeObject<ElementInfo>(
                                actionData["FormElement"].ToString())
                        };
                        break;
                    default:
                        throw new NotSupportedException($"不支持的操作类型：{typeName}");
                }
                
                if (action != null)
                {
                    action.Name = actionData["Name"].ToString();
                    action.Description = actionData["Description"]?.ToString();
                    Actions.Add(action);
                }
            }
        }
    }
    
    // 工作流事件参数
    public class WorkflowEventArgs : EventArgs
    {
        public int ActionCount { get; }
        
        public WorkflowEventArgs(int actionCount)
        {
            ActionCount = actionCount;
        }
    }
    
    // 工作流操作事件参数
    public class WorkflowActionEventArgs : EventArgs
    {
        public BrowserAction Action { get; }
        public int ActionIndex { get; }
        public int TotalActions { get; }
        
        public WorkflowActionEventArgs(BrowserAction action, int actionIndex = -1, int totalActions = -1)
        {
            Action = action;
            ActionIndex = actionIndex;
            TotalActions = totalActions;
        }
    }
    
    // 工作流错误事件参数
    public class WorkflowErrorEventArgs : WorkflowActionEventArgs
    {
        public Exception Error { get; }
        
        public WorkflowErrorEventArgs(BrowserAction action, Exception error, int actionIndex = -1)
            : base(action, actionIndex)
        {
            Error = error;
        }
    }
} 