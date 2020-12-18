﻿namespace GenericUnityObjects.EditorTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Editor.MonoBehaviour;
    using NUnit.Framework;

    internal class AssembliesCheckerTests
    {
        public class GetTypesForAssemblyChange
        {
            private BehaviourInfo _firstInfo;
            private BehaviourInfo _secondInfo;
            private BehaviourInfo _thirdInfo;
            private BehaviourInfo _fourthInfo;

            [OneTimeSetUp]
            public void BeforeAllTests()
            {
                _firstInfo = new BehaviourInfo("firstFullName", "firstGUID");
                _secondInfo = new BehaviourInfo("secondFullName", "secondGUID");
                _thirdInfo = new BehaviourInfo("thirdFullName", "thirdGUID");
                _fourthInfo = new BehaviourInfo("fourthFullName", "fourthGUID");
            }

            [Test]
            public void When_identical_collections_are_passed_returns_empty_collections()
            {
                var oldTypes = new[] { _firstInfo, _secondInfo };
                var newTypes = new[] { _firstInfo, _secondInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                Assert.IsEmpty(typesToRemove);
                Assert.IsEmpty(typesToAdd);
                Assert.IsEmpty(typesToUpdate);
            }

            [Test]
            public void When_newTypes_is_bigger_returns_needAssemblyAdd()
            {
                var oldTypes = new[] { _firstInfo, _secondInfo };
                var newTypes = new[] { _firstInfo, _secondInfo, _thirdInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                var expectedTypesToAdd = new List<BehaviourInfo> { _thirdInfo };

                Assert.IsEmpty(typesToRemove);
                Assert.IsEmpty(typesToUpdate);
                Assert.IsTrue(typesToAdd.SequenceEqual(expectedTypesToAdd));
            }

            [Test]
            public void When_oldTypes_is_bigger_returns_needAssemblyRemove()
            {
                var oldTypes = new[] { _firstInfo, _secondInfo, _thirdInfo };
                var newTypes = new[] { _firstInfo, _secondInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                var expectedTypesToRemove = new List<BehaviourInfo> { _thirdInfo };

                Assert.IsEmpty(typesToAdd);
                Assert.IsEmpty(typesToUpdate);
                Assert.IsTrue(typesToRemove.SequenceEqual(expectedTypesToRemove));
            }

            [Test]
            public void When_GUID_differs_returns_empty_collections()
            {
                var firstTestInfo = new BehaviourInfo("testFullName", "testFirstGuid");
                var secondTestInfo = new BehaviourInfo("testFullName", "testSecondGuid");

                var oldTypes = new[] { _firstInfo, _secondInfo, firstTestInfo };
                var newTypes = new[] { _firstInfo, _secondInfo, secondTestInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                Assert.IsEmpty(typesToRemove);
                Assert.IsEmpty(typesToAdd);
                Assert.IsEmpty(typesToUpdate);
            }

            [Test]
            public void When_TypeFullName_differs_but_GUID_is_same_adds_types_to_needAssemblyUpdate()
            {
                var firstTestInfo = new BehaviourInfo("firstTestType", "testGuid");
                var secondTestInfo = new BehaviourInfo("secondTestType", "testGuid");

                var oldTypes = new[] { _firstInfo, _secondInfo, firstTestInfo };
                var newTypes = new[] { _firstInfo, _secondInfo, secondTestInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                var expectedTypesToUpdate = new List<BehaviourInfoPair>
                {
                    new BehaviourInfoPair(firstTestInfo, secondTestInfo)
                };

                Assert.IsEmpty(typesToRemove);
                Assert.IsEmpty(typesToAdd);
                Assert.IsTrue(typesToUpdate.SequenceEqual(expectedTypesToUpdate));
            }

            [Test]
            public void When_everything_differs_adds_types_to_needAssemblyRemove_and_needAssemblyAdd()
            {
                var oldTypes = new[] { _firstInfo, _secondInfo, _thirdInfo };
                var newTypes = new[] { _firstInfo, _secondInfo, _fourthInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                var expectedTypesToRemove = new List<BehaviourInfo> { _thirdInfo };
                var expectedTypesToAdd = new List<BehaviourInfo> { _fourthInfo };

                Assert.IsEmpty(typesToUpdate);
                Assert.IsTrue(typesToRemove.SequenceEqual(expectedTypesToRemove));
                Assert.IsTrue(typesToAdd.SequenceEqual(expectedTypesToAdd));
            }

            [Test]
            public void When_two_collections_are_empty_returns_empty_collections()
            {
                BehaviourInfo[] oldTypes = { };
                BehaviourInfo[] newTypes = { };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                Assert.IsEmpty(typesToRemove);
                Assert.IsEmpty(typesToAdd);
                Assert.IsEmpty(typesToUpdate);
            }

            [Test]
            public void When_old_collection_is_empty_returns_full_needAssemblyAdd()
            {
                BehaviourInfo[] oldTypes = { };
                BehaviourInfo[] newTypes = { _firstInfo, _secondInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                Assert.IsEmpty(typesToRemove);
                Assert.IsEmpty(typesToUpdate);
                Assert.IsTrue(typesToAdd.SequenceEqual(newTypes));
            }

            [Test]
            public void When_new_collection_is_empty_returns_full_needAssemblyRemove()
            {
                BehaviourInfo[] oldTypes = { _firstInfo, _secondInfo };
                BehaviourInfo[] newTypes = { };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                Assert.IsEmpty(typesToAdd);
                Assert.IsEmpty(typesToUpdate);
                Assert.IsTrue(typesToRemove.SequenceEqual(oldTypes));
            }

            [Test]
            public void When_TypeFullNames_match_and_new_GUID_is_empty_but_old_one_is_not_returns_empty_collections_and_updates_new_GUID()
            {
                var firstTestInfo = new BehaviourInfo("TestType", "testGUID");
                var secondTestInfo = new BehaviourInfo("TestType", string.Empty);

                var oldTypes = new[] { _firstInfo, _secondInfo, firstTestInfo };
                var newTypes = new[] { _firstInfo, _secondInfo, secondTestInfo };

                var (typesToRemove, typesToAdd, typesToUpdate) =
                    AssembliesChecker.GetTypesForAssemblyChange(oldTypes, newTypes);

                Assert.IsEmpty(typesToAdd);
                Assert.IsEmpty(typesToUpdate);
                Assert.IsEmpty(typesToRemove);
                Assert.IsTrue(newTypes.Last().GUID == "testGUID");
            }
        }
    }
}