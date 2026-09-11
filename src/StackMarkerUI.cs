using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SpecializedStorage
{
    internal static class StackMarkerUI
    {
        private const string MarkerObjectName = "__SpecializedStorageStackMarker";
        private const string MarkerResourceName = "SpecializedStorage.Resources.StackMarker.pixels";
        // The marker is 7x5 and uses a bottom-left pivot. These insets put its
        // center 10.5 pixels from both the left and bottom edges of a 50x50 slot.
        private const float MarkerHorizontalInset = 7f;
        private const float MarkerVerticalInset = 8f;

        private static readonly HashSet<string> LoggedErrors = new HashSet<string>(StringComparer.Ordinal);

        private static ChestGUI _activeGui;
        private static WorldGameObject _activeChest;
        private static StorageKind _activeKind;
        private static Texture2D _texture;
        private static Sprite _sprite;
        private static int _spriteWidth;
        private static int _spriteHeight;

        internal static void OnChestOpening(ChestGUI gui, WorldGameObject chest)
        {
            _activeGui = gui;
            _activeChest = chest;
            if (!StorageRules.TryGetKind(chest, out _activeKind))
            {
                _activeKind = StorageKind.None;
                return;
            }
        }

        internal static void OnChestClosed(ChestGUI gui)
        {
            if (!ReferenceEquals(_activeGui, gui)) return;
            _activeGui = null;
            _activeChest = null;
            _activeKind = StorageKind.None;
        }

        internal static void Refresh(BaseItemCellGUI cell, Item item)
        {
            if (ReferenceEquals(cell, null)) return;

            try
            {
                bool visible = ShouldShow(cell, item);
                Transform markerTransform = cell.transform.Find(MarkerObjectName);
                if (!visible)
                {
                    if (markerTransform != null) markerTransform.gameObject.SetActive(false);
                    return;
                }

                UI2DSprite marker = markerTransform == null
                    ? CreateMarker(cell)
                    : markerTransform.GetComponent<UI2DSprite>();

                if (marker == null)
                {
                    LogErrorOnce("missing-component", "Stack marker child exists without UI2DSprite; recreating it.");
                    if (markerTransform != null) UnityEngine.Object.Destroy(markerTransform.gameObject);
                    marker = CreateMarker(cell);
                }

                LayoutMarker(cell, marker);
                marker.gameObject.SetActive(true);
            }
            catch (Exception exception)
            {
                LogErrorOnce(exception.GetType().FullName + ":" + exception.Message,
                    "Failed to refresh stack marker: " + exception);
            }
        }

        internal static void Dispose()
        {
            _activeGui = null;
            _activeChest = null;
            _activeKind = StorageKind.None;

            if (_sprite != null) UnityEngine.Object.Destroy(_sprite);
            if (_texture != null) UnityEngine.Object.Destroy(_texture);
            _sprite = null;
            _texture = null;
        }

        private static bool ShouldShow(BaseItemCellGUI cell, Item item)
        {
            if (ReferenceEquals(_activeGui, null) || ReferenceEquals(_activeChest, null) ||
                _activeKind == StorageKind.None || ReferenceEquals(item, null) ||
                ReferenceEquals(item.definition, null) || string.IsNullOrEmpty(item.id) ||
                item.id == "empty" || item.value <= 0)
            {
                return false;
            }

            InventoryPanelGUI panel = NGUITools.FindInParents<InventoryPanelGUI>(cell.gameObject);
            if (!ReferenceEquals(panel, _activeGui.chest_panel)) return false;

            string ignored;
            if (!StorageRules.IsSuitable(_activeKind, item.definition, item.id, out ignored)) return false;

            int vanillaMaximum = StackLimitOverride.GetVanillaStackCount(item.definition);
            int moddedMaximum = StorageRules.EffectiveMax(vanillaMaximum);
            return moddedMaximum > vanillaMaximum;
        }

        private static UI2DSprite CreateMarker(BaseItemCellGUI cell)
        {
            EnsureSprite();

            GameObject markerObject = new GameObject(MarkerObjectName);
            markerObject.layer = cell.gameObject.layer;
            markerObject.hideFlags = HideFlags.DontSave;
            markerObject.transform.SetParent(cell.transform, false);

            UI2DSprite marker = markerObject.AddComponent<UI2DSprite>();
            marker.sprite2D = _sprite;
            marker.pivot = UIWidget.Pivot.BottomLeft;
            marker.color = Color.white;
            marker.width = _spriteWidth;
            marker.height = _spriteHeight;
            marker.autoResizeBoxCollider = false;
            return marker;
        }

        private static void LayoutMarker(BaseItemCellGUI cell, UI2DSprite marker)
        {
            BaseItemCellElements elements = cell.container;
            UI2DSprite back = elements == null ? null : elements.back;
            UI2DSprite icon = elements == null ? null : elements.icon;
            UI2DSprite selection = elements == null ? null : elements.selection;

            Vector3 localPosition = new Vector3(
                -25f + MarkerHorizontalInset,
                -25f + MarkerVerticalInset,
                0f);
            if (back != null)
            {
                Vector3[] corners = back.localCorners;
                Vector3 worldCorner = back.transform.TransformPoint(corners[0]);
                Vector3 cellCorner = cell.transform.InverseTransformPoint(worldCorner);
                localPosition.x = Mathf.Round(cellCorner.x + MarkerHorizontalInset);
                localPosition.y = Mathf.Round(cellCorner.y + MarkerVerticalInset);
            }

            marker.transform.localPosition = localPosition;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one;
            marker.width = _spriteWidth;
            marker.height = _spriteHeight;

            int visualDepth = back == null ? 0 : back.depth;
            if (icon != null) visualDepth = Math.Max(visualDepth, icon.depth);
            int depth = visualDepth + 1;
            if (selection != null && selection.depth > visualDepth)
                depth = Math.Min(depth, selection.depth - 1);
            marker.depth = depth;
        }

        private static void EnsureSprite()
        {
            if (_sprite != null) return;

            Assembly assembly = typeof(StackMarkerUI).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(MarkerResourceName))
            {
                if (stream == null) throw new InvalidOperationException("Embedded stack marker resource was not found.");
                using (StreamReader reader = new StreamReader(stream))
                {
                    string header = reader.ReadLine();
                    if (string.IsNullOrEmpty(header)) throw new InvalidDataException("Stack marker resource has no header.");
                    string[] dimensions = header.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (dimensions.Length != 2 ||
                        !int.TryParse(dimensions[0], out _spriteWidth) ||
                        !int.TryParse(dimensions[1], out _spriteHeight) ||
                        _spriteWidth <= 0 || _spriteHeight <= 0)
                    {
                        throw new InvalidDataException("Stack marker resource has invalid dimensions.");
                    }

                    string[] rows = new string[_spriteHeight];
                    for (int y = 0; y < _spriteHeight; y++)
                    {
                        rows[y] = reader.ReadLine();
                        if (rows[y] == null || rows[y].Length != _spriteWidth)
                            throw new InvalidDataException("Stack marker resource row " + y + " has invalid width.");
                    }

                    Color32[] pixels = new Color32[_spriteWidth * _spriteHeight];
                    for (int sourceY = 0; sourceY < _spriteHeight; sourceY++)
                    {
                        int targetY = _spriteHeight - sourceY - 1;
                        for (int x = 0; x < _spriteWidth; x++)
                            pixels[targetY * _spriteWidth + x] = PixelColor(rows[sourceY][x]);
                    }

                    _texture = new Texture2D(_spriteWidth, _spriteHeight, TextureFormat.RGBA32, false);
                    _texture.name = "Specialized Storage Stack Marker";
                    _texture.filterMode = FilterMode.Point;
                    _texture.wrapMode = TextureWrapMode.Clamp;
                    _texture.anisoLevel = 0;
                    _texture.hideFlags = HideFlags.HideAndDontSave;
                    _texture.SetPixels32(pixels);
                    _texture.Apply(false, true);

                    _sprite = Sprite.Create(
                        _texture,
                        new Rect(0f, 0f, _spriteWidth, _spriteHeight),
                        Vector2.zero,
                        1f,
                        0u,
                        SpriteMeshType.FullRect);
                    _sprite.name = "Specialized Storage Stack Marker";
                    _sprite.hideFlags = HideFlags.HideAndDontSave;
                }
            }
        }

        private static Color32 PixelColor(char value)
        {
            switch (value)
            {
                case 'D': return new Color32(69, 49, 31, 255);
                case 'F': return new Color32(137, 98, 50, 255);
                case 'H': return new Color32(190, 146, 72, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }

        private static void LogErrorOnce(string key, string message)
        {
            if (LoggedErrors.Add(key)) SpecializedStoragePlugin.ModLog.LogError(message);
        }
    }
}
