using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Less3.TypeTree.Editor
{
    public class L3TypeTreeWindow : EditorWindow
    {
        private const string ADD_NODE_UXML = "TypeTreeMenu";
        private static VisualTreeAsset _addNodeUXML;
        public VisualTreeAsset addNodeUXML
        {
            get
            {
                if (_addNodeUXML == null)
                    _addNodeUXML = Resources.Load<VisualTreeAsset>(ADD_NODE_UXML);
                return _addNodeUXML;
            }
        }

        private TreeView treeView;
        private TextField searchField;
        private string addNodeFilter = "";
        private List<TreeViewItemData<L3TypeTreeEntry>> menu;
        private Action<Type> nodeSelectedCallback;
        private Action nothingSelectedCallback;

        private bool selectedSomething = false;

        // filter hides the types it rejects from this one window, and a folder left empty by it goes
        // too. The caller decides from where it opened the picker; the window holds no rules of its own.
        public static void OpenForType(Type KeyType, Vector2 position, Action<Type> typeSelectedCallback, Action nothingSelectedCallback = null, Func<Type, bool> filter = null)
        {
            L3TypeTreeWindow window = ScriptableObject.CreateInstance<L3TypeTreeWindow>();
            //get mouse position in screen space

            Vector2 newPos = GUIUtility.GUIToScreenPoint(position);

            window.position = new Rect(newPos.x, newPos.y + 24, 256, 256);
            window.ShowPopup();
            window.Focus();
            window.Setup(KeyType, typeSelectedCallback, nothingSelectedCallback, filter);
        }

        private void OnLostFocus()
        {
            if (this != null)
            {
                this.Close();
            }
        }

        private void OnDestroy()
        {
            if (!selectedSomething)
            {
                nothingSelectedCallback?.Invoke();
            }
        }

        public void Setup(Type keyType, Action<Type> nodeSelectedCallback, Action nothingSelectedCallback, Func<Type, bool> filter = null)
        {
            this.menu = L3TypeTreeCache.GetMenuForType(keyType);
            if (filter != null)
                this.menu = L3TypeTreeCache.Filter(menu, entry => entry.type != null && filter(entry.type));
            this.nodeSelectedCallback = nodeSelectedCallback;
            this.nothingSelectedCallback = nothingSelectedCallback;
            this.selectedSomething = false;

            treeView = rootVisualElement.Q<TreeView>("TreeView");

            treeView.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("ListItem");
                return label;
            };

            treeView.bindItem = (element, i) =>
            {
                var item = treeView.GetItemDataForIndex<L3TypeTreeEntry>(i);
                var label = element as Label;
                if (!string.IsNullOrEmpty(addNodeFilter))
                {
                    label.text = item.path.Replace(addNodeFilter, $"<b><color=yellow>{addNodeFilter}</color></b>", comparisonType: System.StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    label.text = item.path;
                }
            };
            searchField = rootVisualElement.Q<TextField>("search");
            searchField.Focus();
            searchField.RegisterValueChangedCallback(evt =>
            {
                string filterText = evt.newValue.ToLower();
                addNodeFilter = filterText;
                if (string.IsNullOrEmpty(filterText))
                {
                    treeView.autoExpand = false;
                    treeView.SetRootItems(menu);
                }
                else
                {
                    treeView.autoExpand = true;
                    treeView.SetRootItems(L3TypeTreeCache.Filter(menu, entry => entry.path.ToLower().Contains(filterText)));
                    treeView.ExpandAll();
                }
                treeView.Rebuild();
            });

            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    this.Close();
                }

                // lets you focus the tree view with down arrow
                if (evt.keyCode == KeyCode.DownArrow && searchField.focusController.focusedElement == searchField)
                {
                    treeView.Focus();
                    treeView.selectedIndex = 0;
                }

                // return to search field if at top of tree view
                if (evt.keyCode == KeyCode.UpArrow && treeView.selectedIndex == 0)
                {
                    searchField.Focus();
                }
                //! trickledown lets this work even though the text field is focused etc.
            }, TrickleDown.TrickleDown);

            treeView.itemsChosen += objs =>
            {
                // get first selected item
                var item = objs.FirstOrDefault();
                if (item is L3TypeTreeEntry entry && entry.type != null)
                {
                    nodeSelectedCallback?.Invoke(entry.type);
                    selectedSomething = true;
                    this.Close();
                }
            };

            treeView.SetRootItems(menu);
            treeView.Rebuild();
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(addNodeUXML.Instantiate());
        }
    }
}
