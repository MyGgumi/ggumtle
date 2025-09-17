using System;
using System.Collections.Generic;
using MVVM.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Views
{
    public class ChatView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private ChatViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _chatIconArea;
        private VisualElement _chatPanel;
        private VisualElement _chatMessages;
        private TextField _chatInput;
        private Button _sendButton;

        [Header("Quick Chat")]
        private VisualElement _quickChatArea;
        private List<Button> _quickChatButtons = new List<Button>();

        // UI 이벤트 콜백 저장 변수들
        private EventCallback<ClickEvent> _chatIconAreaCallback;
        private EventCallback<KeyDownEvent> _chatInputKeyDownCallback;

        public void Initialize(VisualElement root, ChatViewModel viewModel)
        {
            _root = root;
            this.viewModel = viewModel;

            CacheUIElements();
            SubscribeToViewModel();
            SetupChatEvents();
            CreateChatPanel();
            SetChatVisibility(false);

            Debug.Log("[ChatView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _chatIconArea = _root.Q<VisualElement>("chatIconArea");

            Debug.Log(
                $"[ChatView] UI 요소 캐싱 완료: "
                    + $"채팅아이콘={(_chatIconArea != null ? "OK" : "NULL")}"
            );
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
                return;

            viewModel.ChatToggled += OnChatToggled;
            viewModel.MessageAdded += OnMessageAdded;
            viewModel.MessageReceived += OnMessageReceived;
            viewModel.ChatHistoryCleared += OnChatHistoryCleared;
            viewModel.QuickChatMessagesChanged += OnQuickChatMessagesChanged;

            Debug.Log("[ChatView] ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (viewModel == null)
                return;

            viewModel.ChatToggled -= OnChatToggled;
            viewModel.MessageAdded -= OnMessageAdded;
            viewModel.MessageReceived -= OnMessageReceived;
            viewModel.ChatHistoryCleared -= OnChatHistoryCleared;
            viewModel.QuickChatMessagesChanged -= OnQuickChatMessagesChanged;

            Debug.Log("[ChatView] ViewModel 이벤트 구독 해제 완료");
        }

        private void SetupChatEvents()
        {
            if (_chatIconArea != null)
            {
                // 콜백 인스턴스 생성 및 저장
                _chatIconAreaCallback = evt => OnChatIconClicked();

                // 저장된 콜백으로 등록
                _chatIconArea.RegisterCallback(_chatIconAreaCallback);
            }
        }

        #region ViewModel Event Handlers

        private void OnChatToggled(bool isOpen)
        {
            SetChatVisibility(isOpen);
        }

        private void OnMessageAdded(ChatMessage message)
        {
            CreateMessageElement(message);
            ScrollToBottom();
        }

        private void OnMessageReceived(ChatMessage message)
        {
            // 외부에서 받은 메시지에 대한 추가 처리가 필요하면 여기서
        }

        private void OnChatHistoryCleared()
        {
            ClearMessageElements();
        }

        private void OnQuickChatMessagesChanged(string[] messages)
        {
            UpdateQuickChatButtons(messages);
        }

        #endregion

        #region UI Creation

        private void CreateChatPanel()
        {
            // 채팅 패널을 동적으로 생성
            _chatPanel = new VisualElement();
            _chatPanel.name = "chatPanel";
            _chatPanel.AddToClassList("chat-panel");

            // 채팅 패널 스타일 설정
            _chatPanel.style.position = Position.Absolute;
            _chatPanel.style.right = 30;
            _chatPanel.style.top = 300;
            _chatPanel.style.width = 400;
            _chatPanel.style.height = 300;
            _chatPanel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            _chatPanel.style.borderTopWidth = 2;
            _chatPanel.style.borderBottomWidth = 2;
            _chatPanel.style.borderLeftWidth = 2;
            _chatPanel.style.borderRightWidth = 2;
            _chatPanel.style.borderTopColor = Color.white;
            _chatPanel.style.borderBottomColor = Color.white;
            _chatPanel.style.borderLeftColor = Color.white;
            _chatPanel.style.borderRightColor = Color.white;
            _chatPanel.style.borderTopLeftRadius = 10;
            _chatPanel.style.borderTopRightRadius = 10;
            _chatPanel.style.borderBottomLeftRadius = 10;
            _chatPanel.style.borderBottomRightRadius = 10;
            _chatPanel.style.paddingTop = 10;
            _chatPanel.style.paddingBottom = 10;
            _chatPanel.style.paddingLeft = 10;
            _chatPanel.style.paddingRight = 10;

            // 채팅 메시지 영역
            _chatMessages = new ScrollView();
            _chatMessages.name = "chatMessages";
            _chatMessages.style.flexGrow = 1;
            _chatMessages.style.marginBottom = 10;

            // 퀵 채팅 영역
            if (viewModel.EnableQuickChat)
            {
                CreateQuickChatArea();
            }

            // 입력 영역
            CreateInputArea();

            _chatPanel.Add(_chatMessages);
            if (_quickChatArea != null)
            {
                _chatPanel.Add(_quickChatArea);
            }

            _root.Add(_chatPanel);

            Debug.Log("[ChatView] 채팅 패널 생성 완료");
        }

        private void CreateQuickChatArea()
        {
            _quickChatArea = new VisualElement();
            _quickChatArea.name = "quickChatArea";
            _quickChatArea.style.flexDirection = FlexDirection.Row;
            _quickChatArea.style.flexWrap = Wrap.Wrap;
            _quickChatArea.style.marginTop = 5;
            _quickChatArea.style.marginBottom = 5;

            UpdateQuickChatButtons(viewModel.QuickChatMessages);
        }

        private void CreateInputArea()
        {
            var inputArea = new VisualElement();
            inputArea.name = "inputArea";
            inputArea.style.flexDirection = FlexDirection.Row;
            inputArea.style.alignItems = Align.Center;

            _chatInput = new TextField();
            _chatInput.style.flexGrow = 1;
            _chatInput.style.marginRight = 5;

            // 입력 필드 이벤트
            _chatInputKeyDownCallback = OnChatInputKeyDown;
            _chatInput.RegisterCallback(_chatInputKeyDownCallback);

            _sendButton = new Button(() => OnSendButtonClicked());
            _sendButton.text = "전송";
            _sendButton.style.width = 60;

            inputArea.Add(_chatInput);
            inputArea.Add(_sendButton);

            _chatPanel.Add(inputArea);
        }

        private void UpdateQuickChatButtons(string[] messages)
        {
            if (_quickChatArea == null)
                return;

            // 기존 버튼들 제거
            _quickChatArea.Clear();
            _quickChatButtons.Clear();

            // 새 버튼들 생성
            foreach (string message in messages)
            {
                var quickButton = new Button(() => OnQuickChatButtonClicked(message));
                quickButton.text = message;
                quickButton.AddToClassList("quick-chat-button");
                quickButton.style.fontSize = 12;
                quickButton.style.marginTop = 2;
                quickButton.style.marginBottom = 2;
                quickButton.style.marginLeft = 2;
                quickButton.style.marginRight = 2;
                quickButton.style.paddingTop = 2;
                quickButton.style.paddingBottom = 2;
                quickButton.style.paddingLeft = 5;
                quickButton.style.paddingRight = 5;

                _quickChatArea.Add(quickButton);
                _quickChatButtons.Add(quickButton);
            }

            Debug.Log($"[ChatView] 퀵 채팅 버튼 업데이트: {messages.Length}개");
        }

        #endregion

        #region Message Display

        private void CreateMessageElement(ChatMessage message)
        {
            if (_chatMessages == null)
                return;

            var messageElement = new VisualElement();
            messageElement.AddToClassList("chat-message");
            messageElement.style.marginBottom = 2;
            messageElement.style.paddingTop = 2;
            messageElement.style.paddingBottom = 2;

            // 메시지 타입에 따른 스타일
            Color messageColor = viewModel.GetMessageColor(message.type);

            // 플레이어 이름과 시간
            var headerLabel = new Label(
                $"[{message.timestamp.ToString("HH:mm")}] {message.playerName}:"
            );
            headerLabel.style.color = messageColor;
            headerLabel.style.fontSize = 12;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            // 메시지 내용
            var contentLabel = new Label(message.message);
            contentLabel.style.color = Color.white;
            contentLabel.style.fontSize = 12;
            contentLabel.style.whiteSpace = WhiteSpace.Normal;
            contentLabel.style.marginLeft = 10;

            messageElement.Add(headerLabel);
            messageElement.Add(contentLabel);

            _chatMessages.Add(messageElement);
        }

        private void ClearMessageElements()
        {
            if (_chatMessages != null)
            {
                _chatMessages.Clear();
            }
        }

        private void ScrollToBottom()
        {
            if (_chatMessages != null && _chatMessages is ScrollView scrollView)
            {
                // UI Toolkit에서 스크롤을 맨 아래로 이동
                _chatMessages
                    .schedule.Execute(() =>
                    {
                        scrollView.verticalScroller.value = scrollView.verticalScroller.highValue;
                    })
                    .ExecuteLater(10); // 다음 프레임에 실행
            }
        }

        #endregion

        #region Event Handlers

        private void OnChatIconClicked()
        {
            if (viewModel != null)
            {
                viewModel.ToggleChat();
            }

            Debug.Log("[ChatView] 채팅 아이콘 클릭");
        }

        private void OnChatInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                OnSendButtonClicked();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                if (viewModel != null)
                {
                    viewModel.IsChatOpen = false;
                }
                evt.StopPropagation();
            }
        }

        private void OnSendButtonClicked()
        {
            string message = _chatInput.text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            if (viewModel != null)
            {
                viewModel.SendMessage(message);
            }

            _chatInput.value = "";
        }

        private void OnQuickChatButtonClicked(string message)
        {
            if (viewModel != null)
            {
                viewModel.SendQuickChatMessage(message);
            }
        }

        #endregion

        #region Public API

        public void SetChatVisibility(bool visible)
        {
            if (_chatPanel != null)
            {
                _chatPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (visible && _chatInput != null)
            {
                // 채팅이 열릴 때 입력 필드에 포커스
                _chatInput.Focus();
            }

            Debug.Log($"[ChatView] 채팅 가시성 설정: {visible}");
        }

        public void SetChatIconVisibility(bool visible)
        {
            if (_chatIconArea != null)
            {
                _chatIconArea.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void RefreshChatHistory()
        {
            if (viewModel == null)
                return;

            ClearMessageElements();

            foreach (var message in viewModel.ChatHistory)
            {
                CreateMessageElement(message);
            }

            ScrollToBottom();
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewModel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
            UnregisterUIEvents();
        }

        private void UnregisterUIEvents()
        {
            // 채팅 아이콘 이벤트 해제
            if (_chatIconArea != null && _chatIconAreaCallback != null)
            {
                _chatIconArea.UnregisterCallback(_chatIconAreaCallback);
            }

            // 입력 필드 이벤트 해제
            if (_chatInput != null && _chatInputKeyDownCallback != null)
            {
                _chatInput.UnregisterCallback(_chatInputKeyDownCallback);
            }

            Debug.Log("[ChatView] UI 이벤트 구독 해제 완료");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log(
                $"[ChatView] UI State:\n"
                    + $"  ChatPanel Visible: {(_chatPanel?.style.display.value == DisplayStyle.Flex)}\n"
                    + $"  ChatIcon Visible: {(_chatIconArea?.style.display.value == DisplayStyle.Flex)}\n"
                    + $"  MessageCount: {_chatMessages?.childCount ?? 0}\n"
                    + $"  QuickChatButtons: {_quickChatButtons.Count}\n"
                    + $"  CurrentInput: '{_chatInput?.value ?? "NULL"}'\n"
                    + $"  ViewModel: {(viewModel != null ? "Connected" : "NULL")}"
            );
        }

        #endregion
    }
}
