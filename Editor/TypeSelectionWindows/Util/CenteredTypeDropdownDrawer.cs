﻿namespace GenericScriptableObjects.Editor.TypeSelectionWindows.Util
{
    using System;
    using TypeReferences;
    using TypeReferences.Editor.Drawers;
    using TypeReferences.Editor.TypeDropdown;
    using UnityEditor;
    using UnityEngine;

    internal class CenteredTypeDropdownDrawer : TypeDropdownDrawer
    {
        private readonly Type _selectedType;
        private readonly TypeOptionsAttribute _attribute;

        public CenteredTypeDropdownDrawer(Type selectedType, TypeOptionsAttribute attribute, Type declaringType)
            : base(selectedType, attribute, declaringType)
        {
            _selectedType = selectedType;
            _attribute = attribute;
        }

        public new DropdownWindow Draw(Action<Type> onTypeSelected)
        {
            var dropdownItems = GetDropdownItems();
            var selectionTree = new SelectionTree(dropdownItems, _selectedType, onTypeSelected,
                _attribute.SearchbarMinItemsCount, _attribute.ExcludeNone);

            if (_attribute.ExpandAllFolders)
                selectionTree.ExpandAllFolders();

            Vector2 dropdownPosition = EditorGUIUtility.GetMainWindowPosition().center;
            dropdownPosition.x -= DropdownWindow.CalculateOptimalWidth(selectionTree.SelectionPaths) / 2f;

            return DropdownWindow.Create(selectionTree, _attribute.DropdownHeight, dropdownPosition);
        }
    }
}