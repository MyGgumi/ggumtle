using System;
using UnityEngine;
using UnityEngine.UIElements;
using MVVM.Core;

namespace Views.Core
{
    public abstract class BaseView<TViewModel> : MonoBehaviour where TViewModel : BaseViewModel
    {
        [Header("UI Document")]
        [SerializeField] protected UIDocument _uiDocument;

        [Header("ViewModel")]
        [SerializeField] protected TViewModel _viewModel;

        protected VisualElement _rootElement;
        protected bool _isInitialized = false;

        protected virtual void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }

        protected virtual void Start()
        {
            if (!_isInitialized && _viewModel != null)
            {
                Initialize(_uiDocument?.rootVisualElement, _viewModel);
            }
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        public virtual void Initialize(VisualElement rootElement, TViewModel viewModel)
        {
            if (_isInitialized) return;

            _rootElement = rootElement;
            _viewModel = viewModel;

            if (_rootElement != null && _viewModel != null)
            {
                InitializeUIElements();
                SubscribeToViewModel();
                _isInitialized = true;
            }
        }

        protected virtual void InitializeUIElements()
        {
            // Override in derived classes to initialize UI elements
        }

        protected virtual void SubscribeToViewModel()
        {
            // Override in derived classes to subscribe to ViewModel events
        }

        protected virtual void UnsubscribeFromViewModel()
        {
            // Override in derived classes to unsubscribe from ViewModel events
        }

        protected T GetUIElement<T>(string elementName) where T : VisualElement
        {
            return _rootElement?.Q<T>(elementName);
        }

        protected void SetElementVisibility(string elementName, bool visible)
        {
            var element = GetUIElement<VisualElement>(elementName);
            if (element != null)
            {
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        protected void SetElementText(string elementName, string text)
        {
            var element = GetUIElement<Label>(elementName);
            if (element != null)
            {
                element.text = text;
            }
        }

        public bool IsInitialized => _isInitialized;
        public TViewModel ViewModel => _viewModel;
        public VisualElement RootElement => _rootElement;
    }
}