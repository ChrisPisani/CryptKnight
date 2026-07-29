#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using CryptKnight.Application;
using CryptKnight.Gameplay;
using CryptKnight.Loot;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.UI
{
    public sealed class EditorItemDebugConsole : MonoBehaviour
    {
        private const int WindowId = 0x43524950;
        private const int MaximumLogLines = 80;
        private static readonly Vector2 WindowSize = new Vector2(620f, 430f);

        private readonly List<string> output = new List<string>();
        private GameplaySceneController gameplayController;
        private Rect windowRect;
        private Vector2 outputScroll;
        private string command = string.Empty;
        private bool isVisible;
        private bool restorePausedState;
        private bool focusCommandField;

        public void Initialize(GameplaySceneController controller)
        {
            gameplayController = controller;
            AppendOutput("Type help for available commands.");
        }

        private void Update()
        {
            if (IsTogglePressed())
            {
                SetVisible(!isVisible);
            }
        }

        private void OnDestroy()
        {
            GameplayInputGate.SetBlocked(false);
            if (isVisible && GameManager.HasInstance)
            {
                GameManager.Instance.SetGameplayPaused(restorePausedState);
            }
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            windowRect = GUI.Window(WindowId, windowRect, DrawConsole, "Crypt Knight Item Debug Console");
        }

        private void DrawConsole(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("spawn <item id or name> [quantity] | random <number> | items [filter] | clear");

            outputScroll = GUILayout.BeginScrollView(outputScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < output.Count; i++)
            {
                GUILayout.Label(output[i]);
            }
            GUILayout.EndScrollView();

            Event currentEvent = Event.current;
            bool submitted = currentEvent.type == EventType.KeyDown
                && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter);

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("Item Debug Command");
            command = GUILayout.TextField(command, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Run", GUILayout.Width(72f)))
            {
                submitted = true;
            }
            GUILayout.EndHorizontal();

            if (focusCommandField)
            {
                GUI.FocusControl("Item Debug Command");
                focusCommandField = false;
            }

            if (submitted)
            {
                ExecuteCommand(command);
                command = string.Empty;
                focusCommandField = true;
                currentEvent.Use();
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            GameplayInputGate.SetBlocked(visible);
            if (visible)
            {
                restorePausedState = GameManager.HasInstance && GameManager.Instance.IsGameplayPaused;
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.SetGameplayPaused(true);
                }

                windowRect = new Rect(
                    Mathf.Max(12f, (Screen.width - WindowSize.x) * 0.5f),
                    Mathf.Max(12f, (Screen.height - WindowSize.y) * 0.5f),
                    WindowSize.x,
                    WindowSize.y);
                focusCommandField = true;
                return;
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameplayPaused(restorePausedState);
            }
        }

        private void ExecuteCommand(string rawCommand)
        {
            string trimmed = rawCommand?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return;
            }

            AppendOutput($"> {trimmed}");
            int separator = trimmed.IndexOf(' ');
            string commandName = separator < 0 ? trimmed : trimmed.Substring(0, separator);
            string argument = separator < 0 ? string.Empty : trimmed.Substring(separator + 1).Trim();

            switch (commandName.ToLowerInvariant())
            {
                case "help":
                    AppendOutput("spawn <item id or display name> [quantity]");
                    AppendOutput("random <number>");
                    AppendOutput("items [filter]");
                    AppendOutput("clear");
                    break;
                case "items":
                    ListItems(argument);
                    break;
                case "spawn":
                    SpawnItem(argument);
                    break;
                case "random":
                    SpawnRandomItems(argument);
                    break;
                case "clear":
                    output.Clear();
                    break;
                default:
                    AppendOutput($"Unknown command '{commandName}'.");
                    break;
            }
        }

        private void ListItems(string filter)
        {
            IReadOnlyList<LootItemDefinition> items = GetItems();
            int matches = 0;
            for (int i = 0; i < items.Count; i++)
            {
                LootItemDefinition item = items[i];
                if (!MatchesFilter(item, filter))
                {
                    continue;
                }

                AppendOutput($"{item.ItemId} | {item.DisplayName} | {item.Rarity}");
                matches++;
            }

            if (matches == 0)
            {
                AppendOutput("No matching items.");
            }
        }

        private void SpawnItem(string query)
        {
            if (!TryParseSpawnRequest(query, out string itemQuery, out int quantity))
            {
                AppendOutput("Usage: spawn <item id or display name> [positive quantity]");
                return;
            }

            LootItemDefinition item = ResolveItem(GetItems(), itemQuery);
            if (item == null)
            {
                AppendOutput($"No item matches '{itemQuery}'.");
                return;
            }

            if (gameplayController == null)
            {
                AppendOutput("Gameplay controller is unavailable.");
                return;
            }

            string resultMessage = string.Empty;
            for (int i = 0; i < quantity; i++)
            {
                if (!gameplayController.EditorSpawnItem(item, out resultMessage))
                {
                    AppendOutput(resultMessage);
                    return;
                }
            }

            AppendOutput(quantity == 1
                ? resultMessage
                : $"Spawned {item.DisplayName} x{quantity}.");
        }

        private void SpawnRandomItems(string countArgument)
        {
            if (!TryParseRandomCount(countArgument, out int count))
            {
                AppendOutput("Usage: random <positive number>");
                return;
            }

            IReadOnlyList<LootItemDefinition> items = GetItems();
            if (items == null || items.Count == 0)
            {
                AppendOutput("No configured items are available.");
                return;
            }

            if (gameplayController == null)
            {
                AppendOutput("Gameplay controller is unavailable.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                LootItemDefinition item = items[UnityEngine.Random.Range(0, items.Count)];
                if (!gameplayController.EditorSpawnItem(item, out string resultMessage))
                {
                    AppendOutput(resultMessage);
                    return;
                }
            }

            AppendOutput($"Spawned {count} random {(count == 1 ? "item" : "items")}.");
        }

        private IReadOnlyList<LootItemDefinition> GetItems()
        {
            return gameplayController != null
                ? gameplayController.EditorGetItems()
                : LootTableConfiguration.CreateDefault().Items;
        }

        public static LootItemDefinition ResolveItem(
            IReadOnlyList<LootItemDefinition> items,
            string query)
        {
            if (items == null || string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string normalizedQuery = NormalizeLookup(query);
            for (int i = 0; i < items.Count; i++)
            {
                LootItemDefinition item = items[i];
                if (NormalizeLookup(item.ItemId) == normalizedQuery
                    || NormalizeLookup(item.DisplayName) == normalizedQuery)
                {
                    return item;
                }
            }

            return null;
        }

        public static bool TryParseRandomCount(string argument, out int count)
        {
            return int.TryParse(argument?.Trim(), out count) && count > 0;
        }

        public static bool TryParseSpawnRequest(string argument, out string itemQuery, out int quantity)
        {
            itemQuery = argument?.Trim() ?? string.Empty;
            quantity = 1;
            if (itemQuery.Length == 0)
            {
                return false;
            }

            int separator = itemQuery.LastIndexOf(' ');
            if (separator < 0)
            {
                return true;
            }

            string quantityText = itemQuery.Substring(separator + 1);
            if (!int.TryParse(quantityText, out int parsedQuantity))
            {
                return true;
            }

            itemQuery = itemQuery.Substring(0, separator).Trim();
            quantity = parsedQuantity;
            return itemQuery.Length > 0 && quantity > 0;
        }

        private static string NormalizeLookup(string value)
        {
            StringBuilder normalized = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]))
                {
                    normalized.Append(char.ToLowerInvariant(value[i]));
                }
            }

            return normalized.ToString();
        }

        private static bool MatchesFilter(LootItemDefinition item, string filter)
        {
            return string.IsNullOrWhiteSpace(filter)
                || item.ItemId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || item.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AppendOutput(string line)
        {
            output.Add(line);
            if (output.Count > MaximumLogLines)
            {
                output.RemoveAt(0);
            }

            outputScroll.y = float.MaxValue;
        }

        private static bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.backquoteKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.BackQuote);
#endif
        }
    }
}
#endif
