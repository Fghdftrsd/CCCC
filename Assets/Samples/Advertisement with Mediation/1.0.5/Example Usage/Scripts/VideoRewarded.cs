using System;
using UnityEngine;
using Unity.Services.Core;
using System.Threading.Tasks;

namespace Unity.Services.Mediation.Samples
{
    /// <summary>
    /// Sample Implementation of Unity Mediation
    /// </summary>
    public class VideoRewarded : MonoBehaviour
    {
        [Header("Ad Unit Ids"), Tooltip("Android Ad Unit Ids")]
        public string androidAdUnitId;
        [Tooltip("iOS Ad Unit Ids")]
        public string iosAdUnitId;

        [Header("Game Ids"), Tooltip("[Optional] Specifies the iOS GameId. Otherwise uses the GameId of the linked project.")]
        public string iosGameId;
        [Tooltip("[Optional] Specifies the Android GameId. Otherwise uses the GameId of the linked project.")]
        public string androidGameId;

        IRewardedAd m_RewardedAd;

       private AdManager adManager;

        async void Start()
        {
            // Check if ads should be disabled
            adManager = FindObjectOfType<AdManager>();
            if (adManager != null && !adManager.ShouldShowAds)
            {
                Debug.Log("Ads are disabled");
                return;
            }

            // Initialize Unity Services
            try
            {
                Debug.Log("Initializing...");
                await UnityServices.InitializeAsync(GetGameId());
                Debug.Log("Initialized!");
                InitializationComplete();
            }
            catch (System.Exception e)
            {
                InitializationFailed(e);
            }
        }

        void OnDestroy()
        {
            m_RewardedAd?.Dispose();
        }

        InitializationOptions GetGameId()
        {
            var initializationOptions = new InitializationOptions();

#if UNITY_IOS
            if (!string.IsNullOrEmpty(iosGameId))
            {
                initializationOptions.SetGameId(iosGameId);
            }
#elif UNITY_ANDROID
                if (!string.IsNullOrEmpty(androidGameId))
                {
                    initializationOptions.SetGameId(androidGameId);
                }
#endif

            return initializationOptions;
        }

        public void ShowRewarded()
        {
            if (adManager != null && !adManager.ShouldShowAds)
            {
                UserRewarded(null, new RewardEventArgs("Reward", "50"));
                return;
            }

            if (m_RewardedAd?.AdState == AdState.Loaded)
            {
                try
                {
                    var showOptions = new RewardedAdShowOptions { AutoReload = true };
                    m_RewardedAd.ShowAsync(showOptions); 
                    Debug.Log("Rewarded Shown!");
                }
                catch (ShowFailedException e)
                {
                    Debug.LogWarning($"Rewarded failed to show: {e.Message}");
                }
            }
        }

        public async void ShowRewardedWithOptions()
        {
            if (m_RewardedAd?.AdState == AdState.Loaded)
            {
                try
                {
                    //Here we provide a user id and custom data for server to server validation.
                    RewardedAdShowOptions showOptions = new RewardedAdShowOptions();
                    showOptions.AutoReload = true;
                    S2SRedeemData s2SData;
                    s2SData.UserId = "my cool user id";
                    s2SData.CustomData = "{\"reward\":\"Gems\",\"amount\":20}";
                    showOptions.S2SData = s2SData;

                    await m_RewardedAd.ShowAsync(showOptions);
                    Debug.Log("Rewarded Shown!");
                }
                catch (ShowFailedException e)
                {
                    Debug.LogWarning($"Rewarded failed to show: {e.Message}");
                }
            }
        }

        async void LoadAd()
        {
            try
            {
                await m_RewardedAd.LoadAsync();
            }
            catch (LoadFailedException)
            {
                // We will handle the failure in the OnFailedLoad callback
            }
        }

        void InitializationComplete()
        {
            // Impression Event
            MediationService.Instance.ImpressionEventPublisher.OnImpression += ImpressionEvent;

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    m_RewardedAd = MediationService.Instance.CreateRewardedAd(androidAdUnitId);
                    break;

                case RuntimePlatform.IPhonePlayer:
                    m_RewardedAd = MediationService.Instance.CreateRewardedAd(iosAdUnitId);
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    m_RewardedAd = MediationService.Instance.CreateRewardedAd(!string.IsNullOrEmpty(androidAdUnitId) ? androidAdUnitId : iosAdUnitId);
                    break;
                default:
                    Debug.LogWarning("Mediation service is not available for this platform:" + Application.platform);
                    return;
            }

            // Load Events
            m_RewardedAd.OnLoaded += AdLoaded;
            m_RewardedAd.OnFailedLoad += AdFailedLoad;

            // Show Events
            m_RewardedAd.OnUserRewarded += UserRewarded;
            m_RewardedAd.OnClosed += AdClosed;

            Debug.Log($"Initialized On Start. Loading Ad...");

            LoadAd();
        }

        void InitializationFailed(Exception error)
        {
            SdkInitializationError initializationError = SdkInitializationError.Unknown;
            if (error is InitializeFailedException initializeFailedException)
            {
                initializationError = initializeFailedException.initializationError;
            }
            Debug.Log($"Initialization Failed: {initializationError}:{error.Message}");
        }

        void UserRewarded(object sender, RewardEventArgs e)
        {
            GamePlayManager gamePlayManager = FindObjectOfType<GamePlayManager>();
            if (gamePlayManager != null)
            {
                gamePlayManager.AdShowSucessfully();
            }
        }


        void AdClosed(object sender, EventArgs args)
        {
            Debug.Log("Rewarded Closed! Loading Ad...");
        }

        void AdLoaded(object sender, EventArgs e)
        {
            Debug.Log("Ad loaded");
        }

        void AdFailedLoad(object sender, LoadErrorEventArgs e)
        {
            Debug.Log("Failed to load ad");
            Debug.Log(e.Message);
        }

        void ImpressionEvent(object sender, ImpressionEventArgs args)
        {
            var impressionData = args.ImpressionData != null ? JsonUtility.ToJson(args.ImpressionData, true) : "null";
            Debug.Log($"Impression event from ad unit id {args.AdUnitId} : {impressionData}");
        }
    }
}
