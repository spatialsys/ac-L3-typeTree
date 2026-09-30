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
        private static readonly HashSet<Type> _warnedMissing = new HashSet<Type>();

        public static List<TreeViewItemData<L3TypeTreeEntry>> GetMenuForType(Type graphType)
        {
            if (typeMenuCache.ContainsKey(graphType))
            {
                return typeMenuCache[graphType];
            }

            // Exact key only, no inheritance fallback: a key that has drifted from the field being
            // picked for should fail loudly, not resolve to some neighbouring menu.
            WarnMissing(graphType);
            return new List<TreeViewItemData<L3TypeTreeEntry>>();
        }

        // Warned once per key: the window asks again every time it opens. Never null -- the
        // dictionary lookup above throws on a null key before it can get here.
        private static void WarnMissing(Type keyType)
        {
            if (!_warnedMissing.Add(keyType))
                return;

            Debug.LogWarning($"[L3TypeTree] Nothing is keyed to '{keyType.FullName}', so its picker is empty. " +
                             "Check that the [TypeTreeMenu] key type matches the declared type of the field being picked for.");
        }

        static L3TypeTreeCache()
        {
            // One bucket per key type. Nested loops because both multiply: an attribute names
            // several keys, and a type carries several attributes -- every pairing is registered.
            Dictionary<Type, List<(Type nodeType, TypeTreeMenuAttribute att)>> keyTypes = new Dictionary<Type, List<(Type, TypeTreeMenuAttribute)>>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    // inherit: false -- otherwise a derived class picks up its base's entry and
                    // every concrete type shows up twice.
                    var attrs = (TypeTreeMenuAttribute[])type.GetCustomAttributes(typeof(TypeTreeMenuAttribute), false);
                    foreach (var attr in attrs)
                    {
                        if (attr.keyTypes == null || attr.keyTypes.Length == 0)
                        {
                            Debug.LogError($"[L3TypeTree] '{type.FullName}' has a [TypeTreeMenu] naming no key type, so it will not appear in any picker.");
                            continue;
                        }

                        foreach (var key in attr.keyTypes)
                        {
                            if (key == null)
                            {
                                Debug.LogError($"[L3TypeTree] '{type.FullName}' has a [TypeTreeMenu] with a null key type in its list.");
                                continue;
                            }

                            if (!keyTypes.TryGetValue(key, out var entries))
                            {
                                entries = new List<(Type, TypeTreeMenuAttribute)>();
                                keyTypes[key] = entries;
                            }

                            entries.Add((type, attr));
                        }
                    }
                }
            }

            foreach (var pair in keyTypes)
            {
                BuildMenuForType(pair.Key, pair.Value);
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

            // A counter, because TreeView needs ids unique within the menu: hashing the last path
            // segment collided two leaves both called "Stop" under different folders.
            int nextId = 0;

            List<TreeViewItemData<L3TypeTreeEntry>> BuildTree(Dictionary<string, object> subtree)
            {
                List<TreeViewItemData<L3TypeTreeEntry>> items = new List<TreeViewItemData<L3TypeTreeEntry>>();

                foreach (var key in subtree.Keys.OrderBy(k => k))
                {
                    if (subtree[key] is Type nodeType)
                    {
                        // leaf
                        var entry = new L3TypeTreeEntry { path = key, type = nodeType };
                        items.Add(new TreeViewItemData<L3TypeTreeEntry>(nextId++, entry));
                    }
                    else if (subtree[key] is Dictionary<string, object> childSubtree)
                    {
                        // folder
                        var entry = new L3TypeTreeEntry { path = key, type = null };
                        items.Add(new TreeViewItemData<L3TypeTreeEntry>(nextId++, entry, BuildTree(childSubtree)));
                    }
                    else
                    {
                        Debug.LogError($"[L3TypeTree] Unexpected structure under '{key}' in the '{keyType.Name}' menu.");
                    }
                }

                return items;
            }

            typeMenuCache[keyType] = BuildTree(tree);
        }

        // A matching entry keeps its whole subtree. Folders have no type, so a type filter keeps a
        // folder only while one of its leaves survives.
        public static List<TreeViewItemData<L3TypeTreeEntry>> Filter(IEnumerable<TreeViewItemData<L3TypeTreeEntry>> items, Func<L3TypeTreeEntry, bool> match)
        {
            var result = new List<TreeViewItemData<L3TypeTreeEntry>>();
            foreach (var item in items)
            {
                if (match(item.data))
                {
                    result.Add(item);
                }
                else if (item.hasChildren)
                {
                    var children = Filter(item.children, match);
                    if (children.Count > 0)
                        result.Add(new TreeViewItemData<L3TypeTreeEntry>(item.id, item.data, children));
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
