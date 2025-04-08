using UnityEngine;
using EFT;
using EFT.Game;
using System.Linq;
using Comfort.Common;
using EFT.Interactive;
using LightsAutomaticIdentiier;
using EFT.CameraControl;

public class IdentifierManager : MonoBehaviour
{
    private Camera playerCamera;
    private Player player;

    public float rayRadius = 0.2f;
    public float rayDistance = 200f;
    static int include = LayerMask.GetMask("Player", "Default", "LowPolyCollider", "HighPolyCollider");
    static int exclude = 1 << LayerMask.NameToLayer("Ignore Raycast");
    public LayerMask Bots = include & ~exclude;
    private RaycastHit[] hitBuffer = new RaycastHit[10]; // Adjust size as needed
    private string currentLabelText = "";
    private float labelDisplayTime = 0f;
    private const float labelDuration = 1f; // seconds


    public void Awake()
    {
        player = Singleton<GameWorld>.Instance.MainPlayer;
        if (player == null) Plugin.Log.LogError("PLAYER IS NULL");
        //playerCamera = player.CameraContainer.GetComponent<Camera>(); // You may want to assign this more explicitly via EFT systems
        playerCamera = Singleton<PlayerCameraController>.Instance.Camera;
        if (playerCamera == null) Plugin.Log.LogError("PLAYERCAM IS NULL");
        Plugin.Log.LogInfo("We attatched to the main player");
    }

    private void Update()
    {
        if (player == null || playerCamera == null) return;

        if (player.HandsController != null && player.HandsController.IsAiming)
        {
            CheckForBot();
        }
        else
        {
            currentLabelText = "";
        }

        // Reduce label display timer
        if (labelDisplayTime > 0)
        {
            labelDisplayTime -= Time.deltaTime;
            if (labelDisplayTime <= 0)
            {
                currentLabelText = "";
            }
        }
    }
    private void CheckForBot()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 150, Bots))
        {
            GameObject hitObject = hit.collider.gameObject;
            BotOwner bot = hitObject.GetComponent<BotOwner>();

            if (bot != null && bot != player && !bot.IsDead)
            {
                DisplayText(bot);
            }
        }
        else
        {
            return;
            // Didn't hit anything
            //HideIdentifierText(); // Optional: nothing to identify
        }

    }
    bool IsObstacle(GameObject obj)
    {
        // Example using tag
        return obj.layer == LayerMask.NameToLayer("Default");

        // Or use a whitelist approach and say:
        // return !IsBot(obj);
    }

    bool IsBot(GameObject obj)
    {
        return obj.TryGetComponent<BotOwner>(out var maybeBot);
    }

    private void DisplayText(BotOwner bot)
    {
        if (bot.EnemiesController.EnemyInfos.TryGetValue(player, out var info))
        {
            currentLabelText = "Hostile";
        }
        else
        {
            currentLabelText = "Friendly";
        }

        labelDisplayTime = labelDuration;
    }

    // Unity GUI function for drawing text to screen
    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentLabelText))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;

            // Set color based on the label text
            if (currentLabelText == "Hostile")
                style.normal.textColor = Color.red;
            else if (currentLabelText == "Friendly")
                style.normal.textColor = Color.green;
            else
                style.normal.textColor = Color.white;

            // Slightly below center (adjust Y as needed)
            Rect rect = new Rect(Screen.width / 2 - 100, Screen.height / 2 + 600, 200, 100);
            GUI.Label(rect, currentLabelText, style);
        }
    }
}