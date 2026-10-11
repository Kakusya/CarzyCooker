using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormLoginComponent))]
    [FriendOf(typeof(UIFormLoginComponent))]
    public static partial class UIFormLoginComponentSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormLoginComponent self)
        {
            self.OpenAllUIWidgets();
            self.View.LoginButton.SetAsync(self.OnLogin);
            Log.Debug("Login界面OnOpen");
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormLoginComponent self, bool isShutdown)
        {
            
        }

        public static UniTask OnLogin(this UIFormLoginComponent self)
        {
            return LoginHelper.Login(
                self.Root(),
                self.View.AccountInputField.text,
                self.View.PasswordInputField.text);
        }

    }
}
