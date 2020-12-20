﻿namespace GenericUnityObjects.Editor.MonoBehaviour
{
    using System;

    [Serializable]
    internal class BehaviourInfo : TypeInfo
    {
        public string AssemblyGUID;

        public BehaviourInfo(string typeNameAndAssembly, string guid)
            : base(typeNameAndAssembly, guid) { }

        public BehaviourInfo(string typeFullName, string assemblyName, string guid)
            : base(typeFullName, assemblyName, guid) { }

        public BehaviourInfo(Type type)
            : base(type) { }
    }

    internal readonly struct BehaviourInfoPair
    {
        public readonly BehaviourInfo OldType;
        public readonly BehaviourInfo NewType;

        public BehaviourInfoPair(BehaviourInfo oldType, BehaviourInfo newType)
        {
            OldType = oldType;
            NewType = newType;
        }
    }
}