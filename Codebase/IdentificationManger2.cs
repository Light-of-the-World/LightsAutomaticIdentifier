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
    static int include = LayerMask.GetMask("Player", /*"Default",*/ "HighPolyCollider", "Terrain" /*"HitCollider"*/);
    static int exclude = 1 << LayerMask.NameToLayer("Ignore Raycast");
    public LayerMask Bots = include & ~exclude;
    private readonly Dictionary<string, float> identifiedBots = new Dictionary<string, float>();
    public float IdentificationDurationBase = 0.7f;
    public float IdentificationDistanceMultiplierBase = 0.1f;
    public float IdentificationRangeBase = 100f;
    private float IdentificationMemoryDuration = 60f;
    private float lastSeenTime;
    private const float GracePeriod = 0.5f;
    public float IdentificationDurationCombined = 0.7f;
    public float IdentificationDistanceMultiplierCombined = 0.05f;
    private float IdentificationRangeCombined = 100f;
    private GUIStyle labelStyle;
    private string displayText = "";
    private Color displayColor = Color.white;
    private bool isAttentionElite = false;
    private bool isPerceptionElite = false;
    private float eliteSearchMult = 1;


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
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 150f, Bots))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (!IsBot(hitObject))
            {
                if (Time.time - lastSeenTime > GracePeriod)
                {
                    ResetIdentification(true);
                    return;
                }
                displayText = "Losing target...";
                displayColor = Color.yellow;
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
                displayText = "Losing target...";
                displayColor = Color.yellow;
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
                float requiredTime;
                if (distance <= 15)
                {
                    if (isAttentionElite) { requiredTime = 0.01f; }
                    else if (isPerceptionElite) { requiredTime = (IdentificationDurationCombined / 2) + ((distance * IdentificationDistanceMultiplierCombined) / 12); }
                    else { requiredTime = (IdentificationDurationCombined / 2) + ((distance * IdentificationDistanceMultiplierCombined) / 6); }
                }
                else
                {
                    if (isPerceptionElite) { requiredTime = (IdentificationDurationCombined) + ((distance * IdentificationDistanceMultiplierCombined) / 12); }
                    else { requiredTime = (IdentificationDurationCombined) + ((distance * IdentificationDistanceMultiplierCombined) / 6); }
                }

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
            displayText = "Losing target...";
            displayColor = Color.yellow;
            return;
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

    public void SetCombinedDistances(int attentionLevel, int perceptionLevel, int searchLevel)
    {
        if (attentionLevel == 51) isAttentionElite = true;
        if (perceptionLevel == 51) isPerceptionElite = true;
        if (searchLevel == 51) eliteSearchMult = 1.5f;
        IdentificationDurationCombined = IdentificationDurationBase - (attentionLevel / 100f);
        IdentificationDistanceMultiplierCombined = IdentificationDistanceMultiplierBase - (perceptionLevel / 750);
        IdentificationRangeCombined = IdentificationRangeBase + (searchLevel * 2);
        IdentificationRangeCombined *= eliteSearchMult;
        Plugin.Log.LogInfo($"Logging set values: Duration is {IdentificationDurationCombined}, distance mult is {IdentificationDistanceMultiplierCombined}, and range is {IdentificationRangeCombined}");
    }
}
