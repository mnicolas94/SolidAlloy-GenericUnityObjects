﻿namespace GenericUnityObjects.Editor.MonoBehaviour
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using SolidUtilities.Extensions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    internal static class AssembliesChecker
    {
        private const string PathToAssemblies = "Assets";

        private static void OnAssembliesReload()
        {
            var oldTypes = PersistentStorage.BehaviourInfos;
            var newTypes = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .Where(type => type.IsGenericType)
                .Select(type => new BehaviourInfo(type))
                .ToArray();

            var (needAssemblyRemove, needAssemblyAdd, needAssemblyUpdate) = GetTypesForAssemblyChange(oldTypes, newTypes);

            // Do the assembly stuff
            RemoveAssemblies(needAssemblyRemove);
            AddAssemblies(needAssemblyAdd);


            PersistentStorage.BehaviourInfos = newTypes;

            if (needAssemblyRemove.Count != 0 || needAssemblyAdd.Count != 0 || needAssemblyUpdate.Count != 0)
                AssetDatabase.Refresh();
        }

        private static void RemoveAssemblies(List<BehaviourInfo> typesToRemove)
        {
            foreach (BehaviourInfo typeInfo in typesToRemove)
            {
                Assert.IsFalse(string.IsNullOrEmpty(typeInfo.AssemblyGUID));
                var dirInfo = new DirectoryInfo(PathToAssemblies);

                foreach (var fileInfo in dirInfo.GetFiles($"{typeInfo.AssemblyGUID}*.dll"))
                {
                    FileUtil.DeleteFileOrDirectory(fileInfo.FullName);
                }
            }
        }

        private static void AddAssemblies(List<BehaviourInfo> typesToAdd)
        {
            foreach (BehaviourInfo typeInfo in typesToAdd)
            {
                // Find a name for assembly
                // Find all the other needed info
                // Generate and save an assembly
                // Assign assembly name to typeInfo
            }
        }

        private static void UpdateAssemblies(List<BehaviourInfoPair> typesToUpdate)
        {
            foreach (var typesPair in typesToUpdate)
            {
                // collect generated concrete classes
                // remove the assembly
                // add a new assembly with the new concrete classes
            }
        }

        public static (
            List<BehaviourInfo> typesToRemove,
            List<BehaviourInfo> typesToAdd,
            List<BehaviourInfoPair> typesToUpdate
            ) GetTypesForAssemblyChange(BehaviourInfo[] oldTypes, BehaviourInfo[] newTypes)
        {
            int oldTypesLength = oldTypes.Length;
            int newTypesLength = newTypes.Length;

            // Optimizations for some cases.
            if (oldTypesLength == 0 && newTypesLength == 0)
            {
                return (new List<BehaviourInfo>(),
                    new List<BehaviourInfo>(),
                    new List<BehaviourInfoPair>());
            }

            if (oldTypesLength == 0)
            {
                return (new List<BehaviourInfo>(),
                    new List<BehaviourInfo>(newTypes),
                    new List<BehaviourInfoPair>());
            }

            if (newTypesLength == 0)
            {
                return (new List<BehaviourInfo>(oldTypes),
                    new List<BehaviourInfo>(),
                    new List<BehaviourInfoPair>());
            }

            var oldTypesSet = new HashSet<BehaviourInfo>(oldTypes);
            var newTypesSet = new HashSet<BehaviourInfo>(newTypes);

            var oldTypesOnly = oldTypesSet.ExceptWithAndCreateNew(newTypesSet).ToList();
            var newTypesOnly = newTypesSet.ExceptWithAndCreateNew(oldTypesSet).ToArray();
            var needAssemblyUpdate = new List<BehaviourInfoPair>(1);
            var needAssemblyAdd = new List<BehaviourInfo>(newTypesOnly.Length);

            foreach (BehaviourInfo newTypeInfo in newTypesOnly)
            {
                bool foundMatching = false;

                foreach (BehaviourInfo oldTypeInfo in oldTypesOnly)
                {
                    // Full names are equal but GUIDs are not
                    if (newTypeInfo.TypeFullName == oldTypeInfo.TypeFullName)
                    {
                        // If the new GUID was not found, save the old GUID.
                        if (string.IsNullOrEmpty(newTypeInfo.GUID) && ! string.IsNullOrEmpty(oldTypeInfo.GUID))
                            newTypeInfo.UpdateGUID(oldTypeInfo.GUID); // TODO: check carefully that it doesn't create issues

                        oldTypesOnly.Remove(oldTypeInfo);
                        foundMatching = true;
                        break;
                    }

                    // GUIDS are equal but full names are not
                    if (newTypeInfo.GUID == oldTypeInfo.GUID)
                    {
                        oldTypesOnly.Remove(oldTypeInfo); // Remove old type from collection to optimize the next searches.
                        needAssemblyUpdate.Add(new BehaviourInfoPair(oldTypeInfo, newTypeInfo)); // Selector and all concrete classes need to be regenerated for this assembly.
                        foundMatching = true;
                        break; // Go to new newTypeInfo
                    }
                }

                // There is no matching old type info, so a completely new assembly must be added for this type
                if (! foundMatching)
                    needAssemblyAdd.Add(newTypeInfo);
            }

            // All the matching types were removed from oldTypesOnly, so all the left must be removed.
            return (oldTypesOnly, needAssemblyAdd, needAssemblyUpdate);
        }
    }
}