using UnityEngine;
using System;

namespace Less3.TypeTree
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class TypeTreeMenuAttribute : System.Attribute
    {
        /// <summary>Every menu this type is offered in. One entry is the ordinary case.</summary>
        public Type[] keyTypes;
        public string path;

        /// <summary>
        /// Offers the type in as many menus as it names, all at the same path -- so something
        /// authorable into more than one differently-typed field is one attribute rather than one
        /// per menu, and the path is written once where it cannot drift between them.
        /// </summary>
        // Path first because a params list has to come last.
        public TypeTreeMenuAttribute(string path, params Type[] keyTypes)
        {
            this.keyTypes = keyTypes;
            this.path = path;
        }
    }
}
