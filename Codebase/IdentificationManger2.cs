using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using LightsAutomaticIdentiier;
using UnityEngine;

public class IdentifierManager2 : MonoBehaviour
{
    private Player player;
    private Camera playerCamera;

    private BotOwner currentTarget;
    private float identificationStartTime;
    private bool isIdentifying;
    static int include = LayerMask.GetMask("Player", "Default", /*"LowPolyCollider",*/ "HighPolyCollider");
    static int exclude = 1 << LayerMask.NameToLayer("Ignore Raycast");
    public LayerMask Bots = include & ~exclude;
    private readonly Dictionary<string, float> identifiedBots = new Dictionary<string, float>();
    private const float IdentificationDurationBase = 0.5f;
    private const float IdentificationDistanceMultiplier = 0.05f;
    private const float IdentificationMemoryDuration = 60f;
    private float lastSeenTime;
    private const float GracePeriod = 0.5f;

    private GUIStyle labelStyle;
    private string displayText = "";
    private Color displayColor = Color.white;


    private void Awake()
    {
        player = Singleton<GameWorld>.Instance.MainPlayer;
        playerCamera = Singleton<PlayerCameraController>.Instance.Camera;

        labelStyle = new GUIStyle
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        if (player == null) Plugin.Log.LogError("PLAYER IS NULL");
        if (playerCamera == null) Plugin.Log.LogError("PLAYERCAM IS NULL");

        Plugin.Log.LogInfo("IdentifierManager2 attached to the main player.");
    }

    private void Update()
    {
        if (player == null || playerCamera == null) return;

        if (!player.HandsController.IsAiming)
        {
            ResetIdentification(true); // hard reset when not aiming
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 150f, Bots))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (!IsBot(hitObject))
            {
                if (Time.time - lastSeenTime > GracePeriod) ResetIdentification(true);
                return;
            }

            var localPlayer = hitObject.GetComponent<LocalPlayer>();
            var botOwner = hitObject.GetComponent<BotOwner>();

            if (localPlayer == null || botOwner == null || botOwner.IsDead)
            {
                if (Time.time - lastSeenTime > GracePeriod)
                {
                    ResetIdentification(true);
                    return;
                }
                return;
            }

            string botId = localPlayer.AccountId;
            lastSeenTime = Time.time;

            if (identifiedBots.TryGetValue(botId, out float lastIdentifiedTime) &&
                Time.time - lastIdentifiedTime < IdentificationMemoryDuration)
            {
                ShowIdentification(botOwner);
                return;
            }

            if (currentTarget != botOwner)
            {
                currentTarget = botOwner;
                identificationStartTime = Time.time;
                isIdentifying = true;
                displayText = "Identifying...";
                displayColor = Color.white;
            }
            else if (isIdentifying)
            {
                float distance = Vector3.Distance(playerCamera.transform.position, botOwner.Position);
                float requiredTime = IdentificationDurationBase + (distance * IdentificationDistanceMultiplier);

                if (Time.time - identificationStartTime >= requiredTime)
                {
                    identifiedBots[botId] = Time.time;
                    ShowIdentification(botOwner);
                    isIdentifying = false;
                }
            }
        }
        else
        {
            // No hit — delay cancel unless grace time exceeded
            if (Time.time - lastSeenTime > GracePeriod)
            {
                ResetIdentification(true);
                return;
            }
        }
    }

    bool IsBot(GameObject obj)
    {
        return obj.TryGetComponent<BotOwner>(out var maybeBot);
    }

    private void ShowIdentification(BotOwner bot)
    {
        var enemyInfos = bot.EnemiesController?.EnemyInfos;
        bool isHostile = enemyInfos != null && enemyInfos.ContainsKey(player);

        displayText = isHostile ? "Hostile" : "Friendly";
        displayColor = isHostile ? Color.red : Color.green;
    }


    private void ResetIdentification(bool immediate)
    {
        if (immediate || currentTarget == null)
        {
            currentTarget = null;
            isIdentifying = false;
            displayText = "";
        }
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(displayText))
        {
            labelStyle.normal.textColor = displayColor;
            float x = Screen.width / 2f;
            float y = Screen.height / 2f + 100f; // Lowered below crosshair
            GUI.Label(new Rect(x - 100, y, 200, 40), displayText, labelStyle);
        }
    }
}
