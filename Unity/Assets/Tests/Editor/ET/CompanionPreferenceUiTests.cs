#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("CompanionPreferenceUi"), Timeout(360000)]
        public IEnumerator EntryCompanionPreferenceActualSoloChoicesWorkWashNaturalCloseNextAndRetiredCallbacks() => CompanionPresenceUiJourney(false, true);

        [UnityTest, Category("CompanionPreferenceUi"), Timeout(360000)]
        public IEnumerator EntryCompanionPreferenceActualKcpReadOnlyPermissionWorkWashNaturalCloseNextAndRetiredCallbacks() => CompanionPresenceUiJourney(true, true);
    }
}
#endif
