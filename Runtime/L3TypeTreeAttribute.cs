using UnityEngine;
using System;

namespace Less3.TypeTree
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class TypeTreeMenuAttribute : System.Attribute
    {
        public Type keyType;
        public string path;

        public TypeTreeMenuAttribute(Type keyType, string path)
        {
            this.keyType = keyType;
            this.path = path;
        }
    }
}
