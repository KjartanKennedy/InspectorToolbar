using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

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

        void ShowHistory()
        {
            VisualElement root = rootVisualElement;
            Object[] forwardStack = GetForwardStack();
            Object[] backStack = GetBackStack();

            if (historyStack != null)
            {
                root.Remove(historyStack);
            }
            

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(InspectorToolbarPackagePath + "/historyStack.uxml");
            historyStack = visualTree.CloneTree();

            for (int i = forwardStack.Length - 1; i >= 0f; i--)
            {
                Label forwardItem = new Label(forwardStack[i].name);
                historyStack.Add(forwardItem);
            }

            Label currentItem = new Label("[" + activeSelection.name + "]");
            historyStack.Add(currentItem);

            foreach (Object o in backStack)
            {
                Label backItem = new Label(o.name);
                historyStack.Add(backItem);
            }

            root.Add(historyStack);
        }

        void test()
        {

        }

        Object[] GetBackStack()
        {
            return previousSelections.GetStack();
        }

        Object[] GetForwardStack()
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
    }
}