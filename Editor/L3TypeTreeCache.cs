using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Less3.TypeTree.Editor
{
    // Creates cached tree collections for building the create node menu.

    public static class L3TypeTreeCache
    {
        public static Dictionary<Type, List<TreeViewItemData<L3TypeTreeEntry>>> typeMenuCache = new Dictionary<Type, List<TreeViewItemData<L3TypeTreeEntry>>>();

        public static List<TreeViewItemData<L3TypeTreeEntry>> GetMenuForType(Type graphType)
        {
            if (typeMenuCache.ContainsKey(graphType))
            {
                return typeMenuCache[graphType];
            }

            // try all directly inherited types in order
            foreach (var iface in graphType.GetInterfaces())
            {
                if (iface.IsClass && typeMenuCache.ContainsKey(iface))
                {
                    return typeMenuCache[iface];
                }
            }

            return new List<TreeViewItemData<L3TypeTreeEntry>>();
        }

        public static List<TreeViewItemData<L3TypeTreeEntry>> GetFilteredMenuForType(Type graphType, string filterText)
        {
            if (typeMenuCache.ContainsKey(graphType))
            {
                if (string.IsNullOrEmpty(filterText))
                {
                    return typeMenuCache[graphType];
                }
                else
                {
                    filterText = filterText.ToLower();
                    return GetFilteredTree(typeMenuCache[graphType], filterText);
                }
            }
            return new List<TreeViewItemData<L3TypeTreeEntry>>();
        }

        static L3TypeTreeCache()
        {
            // get all TypeTreeMenuAttribute types
            List<(Type graphType, Type nodeType, TypeTreeMenuAttribute[] attrs)> types = new List<(Type, Type, TypeTreeMenuAttribute[])>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attrs = (TypeTreeMenuAttribute[])type.GetCustomAttributes(typeof(TypeTreeMenuAttribute), false);
                    if (attrs.Length > 0)
                    {
                        types.Add((attrs[0].keyType, type, attrs));
                    }
                }
            }

            // sort by graph types
            Dictionary<Type, List<(Type nodeType, TypeTreeMenuAttribute att)>> keyTypes = new Dictionary<Type, List<(Type, TypeTreeMenuAttribute)>>();
            foreach (var (graphType, nodeType, attrs) in types)
            {
                if (!keyTypes.ContainsKey(graphType))
                    keyTypes[graphType] = new List<(Type, TypeTreeMenuAttribute)>();

                foreach (var attr in attrs)
                {
                    if (attr.keyType == graphType)
                    {
                        keyTypes[graphType].Add((nodeType, attr));
                    }
                }
            }

            // build tree for each graph type
            foreach (var keyType in keyTypes.Keys)
            {
                BuildMenuForType(keyType, keyTypes[keyType]);
            }
        }

        private static void BuildMenuForType(Type keyType, List<(Type nodeType, TypeTreeMenuAttribute att)> nodeTypes)
        {
            if (typeMenuCache.ContainsKey(keyType))
                return;

            // where object is either a leaf or another dictionary woohoo
            var tree = new Dictionary<string, object>();

            foreach (var (nodeType, att) in nodeTypes)
            {
                string[] pathParts = att.path.Split('/');

                Dictionary<string, object> currentLevel = tree;
                for (int i = 0; i < pathParts.Length; i++)
                {
                    if (i == pathParts.Length - 1)
                    {
                        // leaf
                        currentLevel[pathParts[i]] = nodeType;
                    }
                    else
                    {
                        // folder
                        if (!currentLevel.ContainsKey(pathParts[i]))
                        {
                            currentLevel[pathParts[i]] = new Dictionary<string, object>();
                            currentLevel = (Dictionary<string, object>)currentLevel[pathParts[i]];
                        }
                        else
                        {
                            currentLevel = (Dictionary<string, object>)currentLevel[pathParts[i]];
                        }
                    }
                }
            }

            // recursively create tree view items
            List<TreeViewItemData<L3TypeTreeEntry>> BuildTree(Dictionary<string, object> subtree)
            {
                List<TreeViewItemData<L3TypeTreeEntry>> items = new List<TreeViewItemData<L3TypeTreeEntry>>();

                foreach (var key in subtree.Keys.OrderBy(k => k))
                {
                    if (subtree[key] is Type nodeType)
                    {
                        // leaf
                        var entry = new L3TypeTreeEntry { path = key, type = nodeType };
                        items.Add(new TreeViewItemData<L3TypeTreeEntry>(entry.path.GetHashCode(), entry));
                    }
                    else if (subtree[key] is Dictionary<string, object> childSubtree)
                    {
                        // folder
                        var children = BuildTree(childSubtree);
                        var entry = new L3TypeTreeEntry { path = key, type = null };
                        items.Add(new TreeViewItemData<L3TypeTreeEntry>(entry.path.GetHashCode(), entry, children));
                    }
                }

                return items;
            }

            List<TreeViewItemData<L3TypeTreeEntry>> items = new List<TreeViewItemData<L3TypeTreeEntry>>();
            foreach (var key in tree.Keys)
            {
                if (tree[key] is Dictionary<string, object> branch)
                {
                    items.Add(new TreeViewItemData<L3TypeTreeEntry>(key.GetHashCode(), new L3TypeTreeEntry { path = key, type = null }, BuildTree(branch)));
                }
                else if (tree[key] is Type nodeType)
                {
                    var entry = new L3TypeTreeEntry { path = key, type = nodeType };
                    items.Add(new TreeViewItemData<L3TypeTreeEntry>(entry.path.GetHashCode(), entry));
                }
                else
                {
                    Debug.LogError("Unexpected structure in node create menu tree.");
                }
            }
            typeMenuCache[keyType] = items;
        }

        // Recursive function to filter TreeView items
        private static List<TreeViewItemData<L3TypeTreeEntry>> GetFilteredTree(IEnumerable<TreeViewItemData<L3TypeTreeEntry>> items, string filterText)
        {
            List<TreeViewItemData<L3TypeTreeEntry>> result = new List<TreeViewItemData<L3TypeTreeEntry>>();
            foreach (var item in items)
            {
                bool matches = item.data.path.ToLower().Contains(filterText);
                IEnumerable<TreeViewItemData<L3TypeTreeEntry>> filteredChildren = null;

                if (item.children != null && item.children.Count() > 0)
                {
                    filteredChildren = GetFilteredTree(item.children, filterText);
                }

                // Include the item if it matches or if any of its children match
                if (matches || (filteredChildren != null && filteredChildren.Count() > 0))
                {
                    // If the item itself doesn't match but its children do, create a new item with only the matching children
                    if (!matches && filteredChildren != null)
                    {
                        var newItem = new TreeViewItemData<L3TypeTreeEntry>(item.id, item.data, filteredChildren.ToList());
                        result.Add(newItem);
                    }
                    else // If the item matches, or if it matches and has children (even if they don't match), add it as is
                    {
                        result.Add(item);
                    }
                }
            }
            return result;
        }

    }

    public struct L3TypeTreeEntry
    {
        public string path;
        public System.Type type;
    }
}
