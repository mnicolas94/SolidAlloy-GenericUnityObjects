﻿namespace GenericUnityObjects.Editor.GeneratedTypesDatabase
{
    using System;
    using GenericUnityObjects.Util;
    using SolidUtilities.Editor.Helpers;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    internal abstract class TypeInfo : IEquatable<TypeInfo>
    {
        [SerializeField] private string _typeNameAndAssembly;
        [SerializeField] private string _guid;

        protected TypeInfo(string typeNameAndAssembly, string guid)
        {
            _typeNameAndAssembly = typeNameAndAssembly;
            _guid = guid;
        }

        protected TypeInfo(string typeFullName, string assemblyName, string guid)
        {
            _typeNameAndAssembly = TypeUtility.GetTypeNameAndAssembly(typeFullName, assemblyName);
            _guid = guid;
        }

        protected TypeInfo(Type type, string typeGUID = null)
        {
            Type = type;
            _typeNameAndAssembly = TypeUtility.GetTypeNameAndAssembly(type);
            _guid = typeGUID ?? AssetSearcher.GetClassGUID(type);
        }

        public string TypeNameAndAssembly => _typeNameAndAssembly;

        public string GUID => _guid;

        public Type Type { get; private set; }

        public string TypeFullName
        {
            get
            {
                int comaIndex = _typeNameAndAssembly.IndexOf(',');
                return _typeNameAndAssembly.Substring(0, comaIndex);
            }
        }

        public bool RetrieveType<TObject>(out Type type, out bool retrievedFromGUID)
            where TObject : Object
        {
            retrievedFromGUID = false;

            if (Type != null)
            {
                type = Type;
                return true;
            }

            Type = Type.GetType(TypeNameAndAssembly);

            if (Type != null)
            {
                type = Type;
                UpdateGUIDIfNeeded<TObject>();
                return true;
            }

            if (string.IsNullOrEmpty(GUID))
            {
                type = null;
                return false;
            }

            Type = AssetDatabaseHelper.GetTypeFromGUID(GUID);
            type = Type;
            retrievedFromGUID = true;

            return Type != null;
        }

        public Type RetrieveType<TObject>()
            where TObject : Object
        {
            RetrieveType<TObject>(out Type type, out bool _);
            return type;
        }

        private void UpdateGUIDIfNeeded<TObject>()
            where TObject : Object
        {
            string currentGUID = AssetSearcher.GetClassGUID(Type);

            if (GUID == currentGUID || string.IsNullOrEmpty(currentGUID))
                return;

            if (this is ArgumentInfo argument)
            {
                GenerationDatabase<TObject>.UpdateArgumentGUID(argument, currentGUID);
            }
            else if (this is GenericTypeInfo genericTypeInfo)
            {
                GenerationDatabase<TObject>.UpdateGenericTypeGUID(genericTypeInfo, currentGUID);
            }
            else
            {
                throw new TypeLoadException($"{nameof(UpdateGUIDIfNeeded)} method doesn't know of this inheritor of {nameof(TypeInfo)} yet: {GetType()}.");
            }
        }

        public void UpdateGUID(string newGUID) => _guid = newGUID;

        public void UpdateNameAndAssembly(string newFullName, string newAssemblyName) =>
            _typeNameAndAssembly = TypeUtility.GetTypeNameAndAssembly(newFullName, newAssemblyName);

        public void UpdateNameAndAssembly(Type newType) =>
            _typeNameAndAssembly = TypeUtility.GetTypeNameAndAssembly(newType);

        public bool Equals(TypeInfo p)
        {
            // If parameter is null, return false.
            if (ReferenceEquals(p, null))
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, p))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != p.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return TypeNameAndAssembly == p.TypeNameAndAssembly && _guid == p._guid;
        }

        public static bool operator ==(TypeInfo lhs, TypeInfo rhs)
        {
            return lhs?.Equals(rhs) ?? ReferenceEquals(rhs, null);
        }

        public static bool operator !=(TypeInfo lhs, TypeInfo rhs)
        {
            return ! (lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TypeInfo);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _typeNameAndAssembly.GetHashCode();
                hash = hash * 23 + _guid.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => _typeNameAndAssembly;
    }
}