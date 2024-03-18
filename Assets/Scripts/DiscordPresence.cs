using UnityEngine;

public class DiscordPresence : MonoBehaviour
{
    public Discord.Discord discord;

    // Start is called before the first frame update
    void Start()
    {
        discord = new Discord.Discord(1218098385089990666, (System.UInt64)Discord.CreateFlags.Default);
        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity{
            Details = "Climbing"
        };

        activityManager.UpdateActivity(activity, (res) => {
            if (res == Discord.Result.Ok) 
                Debug.Log("Discord status set");
            else 
                Debug.Log("Discord status failed");
        });
    }

    // Update is called once per frame
    void Update() {discord.RunCallbacks();}
}
