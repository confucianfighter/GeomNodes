using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
public class TargetMetadata : MonoBehaviour
{
    public string[] labels = Array.Empty<string>();

    public bool HasLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return false;

        if (labels == null || labels.Length == 0)
            return false;

        for (int i = 0; i < labels.Length; i++)
        {
            string candidate = labels[i];

            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (string.Equals(candidate, label, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // private void OnValidate()
    // {
    //     if (labels == null)
    //         return;

    //     var cleaned = new List<string>(labels.Length);

    //     for (int i = 0; i < labels.Length; i++)
    //     {
    //         string s = labels[i]?.Trim();

    //         if (string.IsNullOrWhiteSpace(s))
    //             continue;

    //         bool alreadyExists = false;
    //         for (int j = 0; j < cleaned.Count; j++)
    //         {
    //             if (string.Equals(cleaned[j], s, StringComparison.OrdinalIgnoreCase))
    //             {
    //                 alreadyExists = true;
    //                 break;
    //             }
    //         }

    //         if (!alreadyExists)
    //             cleaned.Add(s);
    //     }

    //     labels = cleaned.ToArray();
    // }
}