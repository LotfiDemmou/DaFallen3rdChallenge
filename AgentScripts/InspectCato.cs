using System.Text;
using UnityEditor;
using UnityEngine;

public static class InspectCato
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var gos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in gos)
        {
            if (go.name != "Cato" && go.name != "Square") continue;
            var sr = go.GetComponent<SpriteRenderer>();
            sb.AppendLine("name=" + go.name);
            sb.AppendLine("  tag=" + go.tag);
            sb.AppendLine("  parent=" + (go.transform.parent == null ? "<root>" : go.transform.parent.name));
            sb.AppendLine("  active=" + go.activeInHierarchy);
            sb.AppendLine("  scale=" + go.transform.localScale);
            sb.AppendLine("  pos=" + go.transform.localPosition);
            sb.AppendLine("  hasCollider2D=" + (go.GetComponent<Collider2D>() != null));
            if (sr != null)
            {
                string spritePath = sr.sprite == null ? "<null>" : AssetDatabase.GetAssetPath(sr.sprite);
                sb.AppendLine("  sprite=" + spritePath);
                sb.AppendLine("  spriteName=" + (sr.sprite == null ? "<null>" : sr.sprite.name));
                sb.AppendLine("  spriteRect=" + (sr.sprite == null ? "<null>" : sr.sprite.rect.ToString()));
                sb.AppendLine("  ppu=" + (sr.sprite == null ? 0 : sr.sprite.pixelsPerUnit));
                sb.AppendLine("  color=" + sr.color);
                sb.AppendLine("  sortingOrder=" + sr.sortingOrder);
            }
            else sb.AppendLine("  no SpriteRenderer");
        }

        if (sb.Length == 0) return "no Cato/Square objects found";
        return sb.ToString();
    }
}
