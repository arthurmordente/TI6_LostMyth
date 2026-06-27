using System.Collections.Generic;
using Logic.Scripts.Extensions;
using UnityEngine;

public static class LobbyInteractionZoneBootstrap
{
    public static LobbyInteractionZoneView[] EnsureZones(Transform scenarioRoot, LobbyInteractionZoneView[] configuredZones)
    {
        if (!configuredZones.IsNullOrEmpty())
            return configuredZones;

        if (scenarioRoot == null)
            return configuredZones;

        var zones = new List<LobbyInteractionZoneView>();
        var oganjdan = FindChildTransform(scenarioRoot, "InteractableOgandjan");
        if (oganjdan != null)
        {
            var skillZone = CreateZone(
                scenarioRoot,
                "Zone_SkillLoadout",
                oganjdan.position,
                new Vector3(8f, 3f, 8f),
                LobbyInteractionKind.SkillLoadout);

            var skillHint = FindOrCreateHint(oganjdan, skillZone.transform);
            WireZone(skillZone, skillHint);
            zones.Add(skillZone);

            if (oganjdan.TryGetComponent<OganjdanInteractable>(out var legacyInteractable))
                legacyInteractable.enabled = false;
        }

        var tipsCenter = scenarioRoot.TransformPoint(new Vector3(-6f, 0f, 0f));
        var tipsZone = CreateZone(
            scenarioRoot,
            "Zone_Tips",
            tipsCenter,
            new Vector3(10f, 3f, 10f),
            LobbyInteractionKind.TipsCatalog);

        var tipsHint = CreateHintFromTemplate(scenarioRoot, tipsZone.transform);
        WireZone(tipsZone, tipsHint);
        zones.Add(tipsZone);

        return zones.ToArray();
    }

    static LobbyInteractionZoneView CreateZone(
        Transform parent,
        string zoneName,
        Vector3 worldCenter,
        Vector3 size,
        LobbyInteractionKind kind)
    {
        var zoneGo = new GameObject(zoneName, typeof(BoxCollider), typeof(LobbyInteractionZoneView));
        zoneGo.transform.SetParent(parent, false);
        zoneGo.transform.position = worldCenter;

        var collider = zoneGo.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
        collider.center = Vector3.zero;

        var zone = zoneGo.GetComponent<LobbyInteractionZoneView>();
        zone.Configure(kind, null);
        return zone;
    }

    static void WireZone(LobbyInteractionZoneView zone, LobbyFHintView hint)
    {
        if (hint == null)
            return;

        zone.Configure(zone.Kind, hint);
        hint.SetVisible(false);
    }

    static LobbyFHintView FindOrCreateHint(Transform oganjdan, Transform zoneTransform)
    {
        var popup = FindChildTransform(oganjdan, "PopUp_Interactable")
            ?? FindChildTransform(oganjdan, "UI_PopUp_Interactable");

        if (popup == null)
            return CreateHintFromTemplate(oganjdan.root, zoneTransform);

        if (!popup.TryGetComponent<LobbyFHintView>(out var hint))
            hint = popup.gameObject.AddComponent<LobbyFHintView>();

        hint.SetVisible(false);
        return hint;
    }

    static LobbyFHintView CreateHintFromTemplate(Transform scenarioRoot, Transform hintParent)
    {
        Transform template = null;
        foreach (var transform in scenarioRoot.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name is "PopUp_Interactable" or "UI_PopUp_Interactable")
            {
                template = transform;
                break;
            }
        }

        if (template == null)
            return null;

        var clone = Object.Instantiate(template.gameObject, hintParent);
        clone.name = "Hint_Tips";
        clone.transform.localPosition = new Vector3(0f, 2f, 0f);
        clone.transform.localRotation = Quaternion.identity;

        if (!clone.TryGetComponent<LobbyFHintView>(out var hint))
            hint = clone.AddComponent<LobbyFHintView>();

        hint.SetVisible(false);
        return hint;
    }

    static Transform FindChildTransform(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == childName)
                return transform;
        }

        return null;
    }
}
