using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using PopupWindow = UnityEditor.PopupWindow;

namespace CausewayStudios.Tools.InspectorToolbar
{
    public class InspectorToolbar : EditorWindow
    {
        private const int DEFAULT_CAPACITY = 10;
        public static readonly string InspectorToolbarPackageName = "com.causewaystudios.inspectortoolbar";
        public static readonly string InspectorToolbarPackagePath = "Packages/" + InspectorToolbarPackageName + "/Editor/InspectorToolbar";
		

		static Object activeSelection;
        static bool ignoreNextSelectionChangedEvent;
        static DropOutStack<Object> nextSelections;
        static DropOutStack<Object> previousSelections;

        static Button backButton;
        static Button forwardButton;

        TemplateContainer historyStack;

        [MenuItem("Window/CausewayTools/InspectorToolbar")]
        public static void Init()
        {
            InspectorToolbar wnd = GetWindow<InspectorToolbar>();
            wnd.titleContent = new GUIContent("InspectorToolbar");
            wnd.maxSize = new Vector2(10000f, 24f);
            wnd.minSize = new Vector2(100f, 24f);

            InitializeHistoryTracking();
        }

        static void InitializeHistoryTracking()
        {
            int capacity = DEFAULT_CAPACITY;

            nextSelections = new DropOutStack<Object>(capacity);
            previousSelections = new DropOutStack<Object>(capacity);

            //Selection.selectionChanged += HandleSelectionChange;
        }

        public void OnEnable()
        {
            if (previousSelections == null)
            {
                InitializeHistoryTracking();
            }

            activeSelection = Selection.activeObject;

            VisualElement root = rootVisualElement;

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(InspectorToolbarPackagePath + "/InspectorToolbar.uss");

            root.styleSheets.Add(styleSheet);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(InspectorToolbarPackagePath + "/InspectorToolbar.uxml");
            VisualElement toolbar = visualTree.CloneTree();
            root.Add(toolbar);

            backButton = toolbar.Q<Button>("Back");
            backButton.clickable.clicked += Back;
            backButton.tooltip = "Back (cmd+[)";
            backButton.SetEnabled(ValidateBack());


            forwardButton = toolbar.Q<Button>("Forward");
            forwardButton.clickable.clicked += Forward;
            forwardButton.tooltip = "Forward (cmd+])";
            forwardButton.SetEnabled(ValidateForward());

            var historyButton = toolbar.Q<Button>("History");
            historyButton.tooltip = "History";
            historyButton.clickable.clicked += ShowHistory;
        }

        void OnSelectionChange()
        {
            backButton.SetEnabled(ValidateBack());
            forwardButton.SetEnabled(ValidateForward());

            if (ignoreNextSelectionChangedEvent)
            {
                ignoreNextSelectionChangedEvent = false;
                //Repaint();
                return;
            }

            if (activeSelection != null)
            {
                previousSelections.Push(activeSelection);
            }

            activeSelection = Selection.activeObject;

            nextSelections.Clear();

            backButton.SetEnabled(ValidateBack());
            forwardButton.SetEnabled(ValidateForward());
        }


        Rect histRect;
        static HistoryWindow historyWindow;

        void ShowHistory()
        {
            historyWindow = new HistoryWindow(this);
            

            

            //PopupWindow.Show(historyStack);
            UnityEditor.PopupWindow.Show(histRect, historyWindow);
            
            Repaint();
            if (Event.current.type == EventType.Repaint) histRect = GUILayoutUtility.GetLastRect();

            //root.Add(historyStack);
        }

        

        public Object[] GetBackStack()
        {
            return previousSelections.GetStack();
        }

        public Object[] GetForwardStack()
        {
            return nextSelections.GetStack();
        }

        #region Menu Items

        const string backMenuLabel = "Edit/Selection/Back %[";
        const string forwardMenuLabel = "Edit/Selection/Forward %]";

        [MenuItem(backMenuLabel)]
        static void Back()
        {
            if (activeSelection != null)
            {
                nextSelections.Push(activeSelection);
            }

            ignoreNextSelectionChangedEvent = true;

            Selection.activeObject = previousSelections.Pop();
            activeSelection = Selection.activeObject;

        }

        [MenuItem(forwardMenuLabel)]
        static void Forward()
        {
            if (activeSelection != null)
            {
                previousSelections.Push(activeSelection);
            }

            ignoreNextSelectionChangedEvent = true;

            Selection.activeObject = nextSelections.Pop();
            activeSelection = Selection.activeObject;

        }

        public static void GoToHistoryItem(Object obj)
        {
            if (previousSelections.Contains(obj))
            {
                nextSelections.Push(activeSelection);
                var itemToMove = previousSelections.Pop();

                while (itemToMove != obj)
                {
                    nextSelections.Push(itemToMove);
                    itemToMove = previousSelections.Pop();
                }
            }
            else if (nextSelections.Contains(obj))
            {
                previousSelections.Push(activeSelection);
                var itemToMove = nextSelections.Pop();

                while (itemToMove != obj)
                {
                    previousSelections.Push(itemToMove);
                    itemToMove = nextSelections.Pop();
                }
            }

            ignoreNextSelectionChangedEvent = true;

            Selection.activeObject = obj;
            activeSelection = Selection.activeObject;

            historyWindow.Repaint();
        }

        [MenuItem(backMenuLabel, true)]
        static bool ValidateBack()
        {
            return !previousSelections.IsEmpty();
        }

        [MenuItem(forwardMenuLabel, true)]
        static bool ValidateForward()
        {
            return !nextSelections.IsEmpty();
        }
        #endregion

        public class HistoryWindow : PopupWindowContent
        {
            // TODO: Change rendering method, have clickable mouse pointer on hover of item, change size and position

            private InspectorToolbar m_Window;
            TemplateContainer historyStack;

            private bool rendered = false;

            public HistoryWindow(InspectorToolbar m_Window)
            {
                this.m_Window = m_Window;

                //historyItemStyle = new GUIStyle();
                //historyItemStyle.contentOffset = new Vector2(4, 0);
                //historyItemStyle.fixedHeight = 20.0f;
                //historyItemStyle.stretchWidth = true;
            }

            public void Repaint()
            {
                rendered = false;
            }

            public override void OnGUI(Rect rect)
            {
                if (rendered) return;

                if (editorWindow != null)
                {
                    editorWindow.minSize = new Vector2(300.0f, 450.0f);
                }

                VisualElement root = this.editorWindow.rootVisualElement;
                Object[] forwardStack = m_Window.GetForwardStack();
                Object[] backStack = m_Window.GetBackStack();

                if (historyStack != null)
                {
                    root.Remove(historyStack);
                }



                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(InspectorToolbarPackagePath + "/historyStack.uxml");
                historyStack = visualTree.CloneTree();

                var itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(InspectorToolbarPackagePath + "/HistoryItem.uxml");
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(InspectorToolbarPackagePath + "/InspectorToolbar.uss");

                root.styleSheets.Add(styleSheet);

                for (int i = forwardStack.Length - 1; i >= 0f; i--)
                {
                    CreateHistoryItem(itemTemplate, forwardStack[i], historyStack);
                }

                Label currentItem = new Label("[" + activeSelection.name + "]");
                historyStack.Add(currentItem);

                foreach (Object o in backStack)
                {
                    CreateHistoryItem(itemTemplate, o, historyStack);
                }

                root.Add(historyStack);

                rendered = true;
            }

            public void CreateHistoryItem(VisualTreeAsset template, Object item, VisualElement historyStack)
            {
                var itemElement = template.CloneTree();
                //Label forwardItem = new Label(forwardStack[i].name);

                var itemElementIcon = itemElement.Q<Image>("Icon");
                var icon = EditorGUIUtility.ObjectContent(item, item.GetType()).image;
                //var icon = EditorGUIUtility.IconContent(item.GetType().ToString()).image;
                itemElementIcon.image = icon;

                var itemElementName = itemElement.Q<Label>("Name");
                itemElementName.text = item.name;

                var pingButton = itemElement.Q<Button>("Ping");
                pingButton.tooltip = "Ping";
                pingButton.clickable.clicked += () =>
                {
                    EditorGUIUtility.PingObject(item);
                };

                itemElement.RegisterCallback<MouseDownEvent, Object>(HistoryItemMouseDown, item);


                historyStack.Add(itemElement);
            }

            void HistoryItemMouseDown(MouseDownEvent evt, Object item)
            {
                var mouseStartDrag = false;
                var mouseClick = true;
                //mouseStartDrag = (evt.type == EventType.MouseDrag) && evt.button == 0;
                //mouseClick = (evt.type == EventType.MouseUp) && evt.button == 0 && evt.clickCount == 1;

                if (mouseClick)
                {
                    InspectorToolbar.GoToHistoryItem(item);
                }
            }
        }
    }

    
}