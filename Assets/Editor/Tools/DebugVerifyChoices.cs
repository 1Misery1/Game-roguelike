using Game.Narrative;
using UnityEditor;
using UnityEngine;

public static class DebugVerifyChoices
{
    public static void Execute()
    {
        var lake = AssetDatabase.LoadAssetAtPath<StoryInteractableData>(
            "Assets/Resources/Story/Floor2_FrozenLake.asset");
        var mirror = AssetDatabase.LoadAssetAtPath<StoryInteractableData>(
            "Assets/Resources/Story/Floor3_BlackMirror.asset");

        Inspect("Lake",   lake);
        Inspect("Mirror", mirror);
    }

    static void Inspect(string tag, StoryInteractableData d)
    {
        if (d == null) { Debug.LogWarning($"[ChoiceVerify] {tag} = null"); return; }
        Debug.Log($"[ChoiceVerify] {tag} choiceTitle='{d.choiceTitle}' choices.Count={d.choices.Count}");
        foreach (var c in d.choices)
        {
            string items = c.grantStoryItems != null && c.grantStoryItems.Count > 0
                ? string.Join(",", c.grantStoryItems) : "-";
            string flags = c.runStoryFlags != null && c.runStoryFlags.Count > 0
                ? string.Join(",", c.runStoryFlags) : "-";
            Debug.Log($"[ChoiceVerify]   · {c.label} | corrupt={c.addCorruption:+#;-#;0} | items={items} | flags={flags} | follow={c.followLines.Count}行");
        }
    }
}
