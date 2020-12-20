﻿namespace GenericUnityObjects.Editor.MonoBehaviour
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using SolidUtilities.Extensions;
    using SolidUtilities.Helpers;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;
    using Util;

    internal static class GenericBehavioursAnalyzer
    {
        private static bool _needAssetDatabaseRefresh;

        [DidReloadScripts]
        private static void OnScriptsReload()
        {
            _needAssetDatabaseRefresh = false;
            Directory.CreateDirectory(Config.AssembliesDirPath);

            // It happens on editor launch.
            if (GenericBehavioursDatabase.CreatedOnlyInstance == null)
                return;

            CheckArguments();
            CheckBehaviours();

            if (_needAssetDatabaseRefresh)
                AssetDatabase.Refresh();
        }

        private static void CheckArguments()
        {
            foreach (ArgumentInfo argument in GenericBehavioursDatabase.Arguments)
            {
                if (argument.RetrieveType(out Type type, out bool retrievedFromGUID))
                {
                    if (retrievedFromGUID)
                    {
                        UpdateArgumentTypeName(argument, type);
                    }
                }
                else
                {
                    GenericBehavioursDatabase.RemoveArgument(argument, RemoveAssembly);
                }
            }
        }

        private static void CheckBehaviours()
        {
            var oldBehaviours = GenericBehavioursDatabase.Behaviours;
            var newBehaviours = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .Where(type => type.IsGenericType)
                .Select(type => new BehaviourInfo(type))
                .ToArray();

            int oldBehavioursLength = oldBehaviours.Length;
            int newBehavioursLength = newBehaviours.Length;

            // Optimizations for some cases.
            if (oldBehavioursLength == 0 && newBehavioursLength == 0)
            {
                return;
            }

            if (oldBehavioursLength == 0)
            {
                foreach (BehaviourInfo behaviour in newBehaviours)
                {
                    AddNewBehaviour(behaviour);
                }

                return;
            }

            if (newBehavioursLength == 0)
            {
                foreach (BehaviourInfo behaviour in oldBehaviours)
                {
                    GenericBehavioursDatabase.RemoveGenericBehaviour(behaviour, RemoveAssembly);
                }

                return;
            }

            var oldTypesSet = new HashSet<BehaviourInfo>(oldBehaviours);
            var newTypesSet = new HashSet<BehaviourInfo>(newBehaviours);

            var oldBehavioursOnly = oldTypesSet.ExceptWithAndCreateNew(newTypesSet).ToList();
            var newBehavioursOnly = newTypesSet.ExceptWithAndCreateNew(oldTypesSet).ToArray();

            foreach (BehaviourInfo newBehaviour in newBehavioursOnly)
            {
                bool foundMatching = false;

                for (int i = 0; i < oldBehavioursOnly.Count; i++)
                {
                    BehaviourInfo oldBehaviour = oldBehavioursOnly[i];

                    // Full names are equal but GUIDs are not
                    if (newBehaviour.TypeNameAndAssembly == oldBehaviour.TypeNameAndAssembly)
                    {
                        // new type GUID is empty -> leave the old GUID
                        if (!string.IsNullOrEmpty(newBehaviour.GUID))
                        {
                            // new type GUID is not empty -> update old GUID
                            GenericBehavioursDatabase.UpdateBehaviourGUID(ref oldBehaviour, newBehaviour.GUID);
                        }

                        foundMatching = true;
                        break;
                    }

                    // GUIDS are equal but full names are not
                    if (newBehaviour.GUID == oldBehaviour.GUID)
                    {
                        oldBehavioursOnly.Remove(oldBehaviour); // Remove old type from collection to optimize the next searches.
                        UpdateBehaviourTypeName(oldBehaviour, newBehaviour.Type);

                        foundMatching = true;
                        break; // Go to new newTypeInfo
                    }
                }

                // There is no matching old type info, so a completely new assembly must be added for this type
                if (!foundMatching)
                {
                    AddNewBehaviour(newBehaviour);
                }
            }

            foreach (BehaviourInfo behaviour in oldBehavioursOnly)
            {
                GenericBehavioursDatabase.RemoveGenericBehaviour(behaviour, RemoveAssembly);
            }
        }

        private static void UpdateArgumentTypeName(ArgumentInfo argument, Type newType)
        {
            // Retrieve the array of generic arguments where the old argument was listed
            bool behavioursSuccess = GenericBehavioursDatabase.TryGetReferencedBehaviours(argument, out BehaviourInfo[] referencedBehaviours);

            // update argument typename in database before updating assemblies and trying to find behaviour because behaviour might also need to be updated, and the argument should already be new
            GenericBehavioursDatabase.UpdateArgumentNameAndAssembly(ref argument, newType);

            Assert.IsTrue(behavioursSuccess);

            foreach (BehaviourInfo behaviour in referencedBehaviours)
            {
                // Retrieve type of the behaviour
                // If type cannot be retrieved, try finding by GUID
                // If GUID didn't help, remove the whole collection of assemblies
                if (behaviour.RetrieveType(out Type behaviourType, out bool retrievedFromGUID))
                {
                    if (retrievedFromGUID)
                    {
                        UpdateBehaviourTypeName(behaviour, behaviourType);
                    }
                }
                else
                {
                    GenericBehavioursDatabase.RemoveGenericBehaviour(behaviour, RemoveAssembly);
                    continue; // concrete classes are already removed, no need to iterate through them.
                }

                bool concreteClassesSuccess = GenericBehavioursDatabase.TryGetConcreteClassesByArgument(behaviour, argument, out ConcreteClass[] concreteClasses);

                Assert.IsTrue(concreteClassesSuccess);

                foreach (ConcreteClass concreteClass in concreteClasses)
                {
                    UpdateConcreteClassAssembly(behaviourType, concreteClass);
                }
            }
        }

        private static void UpdateBehaviourTypeName(BehaviourInfo behaviour, Type newType)
        {
            bool success = GenericBehavioursDatabase.TryGetConcreteClasses(behaviour, out ConcreteClass[] concreteClasses);

            Assert.IsTrue(success);

            // Update database before operating on assemblies
            GenericBehavioursDatabase.UpdateBehaviourNameAndAssembly(ref behaviour, newType);

            // update selector assembly
            UpdateSelectorAssembly(behaviour.AssemblyGUID, newType);

            // update concrete classes assemblies
            foreach (ConcreteClass concreteClass in concreteClasses)
            {
                UpdateConcreteClassAssembly(newType, concreteClass);
            }
        }

        private static string GetSelectorAssemblyName(Type genericTypeWithoutArgs) => genericTypeWithoutArgs.FullName.MakeClassFriendly();

        private static void UpdateSelectorAssembly(string selectorAssemblyGUID, Type newType)
        {
            string newAssemblyName = GetSelectorAssemblyName(newType);

            ReplaceAssembly(selectorAssemblyGUID, newAssemblyName, () =>
            {
                CreateSelectorAssembly(newType, newAssemblyName);
            });
        }

        private static void CreateSelectorAssembly(Type genericTypeWithoutArgs, string assemblyName)
        {
            Type[] genericArgs = genericTypeWithoutArgs.GetGenericArguments();
            string componentName = "Scripts/" + CreatorUtil.GetShortNameWithBrackets(genericTypeWithoutArgs, genericArgs);
            AssemblyCreator.CreateSelectorAssembly(assemblyName, genericTypeWithoutArgs, componentName);
        }

        private static void AddNewBehaviour(BehaviourInfo behaviour)
        {
            Type behaviourType = behaviour.Type;
            Assert.IsNotNull(behaviourType);

            string assemblyName = GetSelectorAssemblyName(behaviourType);
            CreateSelectorAssembly(behaviourType, assemblyName);

            string assemblyPath = $"{Config.AssembliesDirPath}/{assemblyName}.dll";

            AssetDatabase.ImportAsset(assemblyPath);
            AssetDatabase.ImportAsset(assemblyPath + ".mdb");
            _needAssetDatabaseRefresh = true;

            behaviour.AssemblyGUID = AssetDatabase.AssetPathToGUID(assemblyPath);

            GenericBehavioursDatabase.AddGenericBehaviour(behaviour);
        }

        private static void RemoveAssembly(string assemblyGUID)
        {
            string assemblyPath = AssetDatabase.GUIDToAssetPath(assemblyGUID);
            string mdbPath = $"{assemblyPath}.mdb";
            AssetDatabase.DeleteAsset(assemblyPath);
            AssetDatabase.DeleteAsset(mdbPath);
            _needAssetDatabaseRefresh = true;
        }

        private static void UpdateConcreteClassAssembly(Type behaviourType, ConcreteClass concreteClass)
        {
            if ( ! GetArgumentTypes(concreteClass, out Type[] argumentTypes))
                return;

            UpdateConcreteClassAssembly(behaviourType, argumentTypes, concreteClass);
        }

        private static bool GetArgumentTypes(ConcreteClass concreteClass, out Type[] argumentTypes)
        {
            var arguments = concreteClass.Arguments;
            int argumentsLength = arguments.Length;

            argumentTypes = new Type[argumentsLength];

            for (int i = 0; i < argumentsLength; i++)
            {
                ArgumentInfo argument = arguments[i];

                if (argument.RetrieveType(out Type type, out bool retrievedFromGUID))
                {
                    if (retrievedFromGUID)
                    {
                        UpdateArgumentTypeName(argument, type);
                    }

                    argumentTypes[i] = type;
                }
                else
                {
                    GenericBehavioursDatabase.RemoveArgument(argument, RemoveAssembly);

                    // Since one of the arguments was not found, the assembly associated with the concrete class
                    // already has been removed, and there is no need to try updating it.
                    return false;
                }
            }

            return true;
        }

        private static void UpdateConcreteClassAssembly(Type behaviourType, Type[] argumentTypes, ConcreteClass concreteClass)
        {
            string newAssemblyName = GetAssemblyName(behaviourType, concreteClass);

            Type genericTypeWithArgs = behaviourType.MakeGenericType(argumentTypes);
            string componentName = "Scripts/" + GetComponentName(behaviourType, argumentTypes);

            ReplaceAssembly(concreteClass.AssemblyGUID, newAssemblyName, () =>
            {
                AssemblyCreator.CreateConcreteClass(newAssemblyName, genericTypeWithArgs, componentName);
            });
        }

        private static string GetAssemblyName(Type behaviourType, ConcreteClass concreteClass)
        {
            var argumentsNames = concreteClass.Arguments.Select(argument => GetShortNameForNaming(argument.TypeFullName));
            string newAssemblyName = $"{GetShortNameForNaming(behaviourType.FullName)}_{string.Join("_", argumentsNames)}";
            var dirInfo = new DirectoryInfo(Config.AssembliesDirPath);
            int identicalFilesCount = dirInfo.GetFiles($"{newAssemblyName}*.dll").Length;

            if (identicalFilesCount != 0)
            {
                newAssemblyName += $"_{identicalFilesCount}";
            }

            return newAssemblyName;
        }

        private static void ReplaceAssembly(string assemblyGUID, string newAssemblyName, Action createAssembly)
        {
            string oldAssemblyPath = AssetDatabase.GUIDToAssetPath(assemblyGUID);
            string oldAssemblyPathWithoutExtension = RemoveDLLExtension(oldAssemblyPath);
            File.Delete(oldAssemblyPath);
            File.Delete($"{oldAssemblyPathWithoutExtension}.dll.mdb");

            createAssembly();

            string newAssemblyPathWithoutExtension = $"{Config.AssembliesDirPath}/{newAssemblyName}";
            File.Move($"{oldAssemblyPathWithoutExtension}.dll.meta", $"{newAssemblyPathWithoutExtension}.dll.meta");
            File.Move($"{oldAssemblyPathWithoutExtension}.dll.mdb.meta", $"{newAssemblyPathWithoutExtension}.dll.mdb.meta");
            _needAssetDatabaseRefresh = true; // TODO: perhaps reimport asset instead of refresh, if refresh doesn't work
        }

        private static string RemoveDLLExtension(string assemblyPath)
        {
            return assemblyPath.Substring(0, assemblyPath.Length - 4);
        }

        private static string GetShortNameForNaming(string fullName)
        {
            int lastDotIndex = fullName.LastIndexOf('.');

            if (lastDotIndex != -1)
            {
                fullName = fullName.Substring(lastDotIndex, fullName.Length - lastDotIndex - 1);
            }

            int backtickIndex = fullName.IndexOf('`');

            if (backtickIndex != -1)
            {
                fullName = fullName.Substring(0, backtickIndex);
            }

            return fullName;
        }

        private static string GetComponentName(Type genericTypeWithoutArgs, Type[] genericArgs)
        {
            Assert.IsTrue(genericTypeWithoutArgs.IsGenericTypeDefinition);

            string shortName = genericTypeWithoutArgs.Name;
            string typeNameWithoutSuffix = shortName.StripGenericSuffix();

            var argumentNames = genericArgs
                .Select(argument => argument.FullName)
                .Select(fullName => fullName.ReplaceWithBuiltInName())
                .Select(fullName => fullName.GetSubstringAfterLast('.'));

            return $"{typeNameWithoutSuffix}<{string.Join(",", argumentNames)}>";
        }
    }
}