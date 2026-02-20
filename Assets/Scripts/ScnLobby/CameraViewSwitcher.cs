using UnityEngine;

namespace RDOnline.Lobby
{
    /// <summary>
    /// 视角切换：点击按钮后相机跟随“创建房间”区域并限制边界、禁止玩家移动；点击返回后恢复跟随玩家与大厅边界。
    /// 挂在场景中任意物体上，按钮的 onClick 里调用 SwitchToCreateRoomView() / SwitchToPlayerView() 即可。
    /// </summary>
    public class CameraViewSwitcher : MonoBehaviour
    {
        [Header("玩家视角（返回时用）")]
        [Tooltip("不填则运行时从 PlayerManager 取本地玩家")]
        public Transform PlayerFollowTarget;
        [Tooltip("大厅世界边界（BoxCollider2D）")]
        public BoxCollider2D MainWorldBounds;

        [Header("创建房间视角")]
        [Tooltip("创建房间区域的锚点（空物体即可，相机跟随它）")]
        public Transform CreateRoomFollowTarget;
        [Tooltip("创建房间区域的相机边界（BoxCollider2D）")]
        public BoxCollider2D CreateRoomBounds;

        /// <summary> 切到创建房间视角：禁止移动，相机跟创建房间锚点与边界。 </summary>
        public void SwitchToCreateRoomView()
        {
            LobbyPlayerController.CanMove = false;
            if (PlayerCamera.Instance != null)
            {
                if (CreateRoomFollowTarget != null)
                    PlayerCamera.Instance.SetFollowTarget(CreateRoomFollowTarget);
                if (CreateRoomBounds != null)
                    PlayerCamera.Instance.SetMapBounds(CreateRoomBounds);
            }
        }

        /// <summary> 返回玩家视角：允许移动，相机跟玩家与大厅边界。 </summary>
        public void SwitchToPlayerView()
        {
            LobbyPlayerController.CanMove = true;
            if (PlayerCamera.Instance != null)
            {
                Transform target = PlayerFollowTarget != null ? PlayerFollowTarget : (PlayerManager.Instance != null ? PlayerManager.Instance.LocalPlayerTransform : null);
                if (target != null)
                    PlayerCamera.Instance.SetFollowTarget(target);
                if (MainWorldBounds != null)
                    PlayerCamera.Instance.SetMapBounds(MainWorldBounds);
            }
        }
    }
}
