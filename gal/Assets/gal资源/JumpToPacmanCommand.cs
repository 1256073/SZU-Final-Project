using System.Collections;
using UnityEngine;

namespace VNovelizer.Core.Commands
{
    public class JumpToPacmanCommand : VNCommand
    {
        public override string CommandName => "jump_pacman";

        public override bool Execute(string args)
        {
            return true; // 关键：必须返回 true
        }

        public override IEnumerator ExecuteAsync(string args)
        {
            string sceneName = string.IsNullOrEmpty(args) ? "PacmanGame" : args.Trim();

            if (SceneLoader.Instance == null)
            {
                Debug.LogError("[JumpPacman] SceneLoader 未找到");
                yield break;
            }

            Debug.Log($"[JumpPacman] 加载小游戏: {sceneName}");
            SceneLoader.Instance.LoadMiniGame(sceneName);

            // 等待小游戏结束
            while (SceneLoader.Instance.IsMiniGameRunning)
                yield return null;

            Debug.Log("[JumpPacman] 小游戏结束，继续剧情");
        }

        public override void Interrupt()
        {
            SceneLoader.Instance?.UnloadMiniGame();
        }

        public override void Simulate(string args)
        {
            SceneLoader.Instance?.UnloadMiniGame();
        }
    }
}