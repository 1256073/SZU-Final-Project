# 我与医院里的天使 / The Angel I Met in the Hospital

[中文](#中文) | [English](#english)

---

## 中文

> 一款关于医院、相遇、陪伴与重新前进的 Unity 视觉小说游戏。

《我与医院里的天使》是一款以剧情体验为核心的视觉小说作品。故事从一次深夜加班后的意外开始：主角因事故住进医院，本以为生活只是被迫按下暂停键，却在走廊里遇见了患有一型糖尿病的女孩小妍。

小妍沉默、敏感，却依然喜欢糖果、绘画、公园和那只叫库鲁的猫。随着一次次对话、探望、选择与小游戏挑战，主角也从一个习惯用吐槽掩盖疲惫的普通上班族，逐渐学会认真关心他人。

这个故事里的“天使”并不一定是神圣而遥远的存在。它可以是病房里努力笑起来的孩子，可以是认真照顾病人的护士，也可以是那些看似笨拙、却愿意在关键时刻陪伴他人的普通人。

### 功能特色

- 以章节推进的视觉小说剧情，包含序章至第九章
- 对话推进、分支选择、存档读档、历史记录与设置菜单
- 小妍、护士、666 之神等角色与表情差分
- 医院、病房、走廊、城市公园、神秘空间等剧情场景
- 与关键剧情节点结合的 Pacman 风格小游戏挑战
- BGM、音效、语音、UI 与基础画廊资源

### 游戏玩法

游戏主要由两部分构成：

**视觉小说部分**  
玩家通过点击推进对白，在不同章节中阅读主角与小妍、护士、同事和神明之间的互动。剧本整体采用轻松吐槽的表达方式，但核心围绕疾病、孤独、陪伴和希望展开。

**小游戏部分**  
在神明挑战等关键剧情节点中，游戏会切换到 Pacman 风格的 2D 关卡。玩家需要在迷宫式场景中移动、收集物品并躲避敌人。小游戏不只是额外玩法，也象征主角为了帮助小妍而跨越的阻碍。

### 故事简介

主角是一名被工作消耗的普通上班族。一次深夜归途中发生的意外，让他住进了医院。起初，他只觉得住院生活无聊、麻烦、令人焦躁，直到他遇见了小妍。

小妍患有一型糖尿病，不能像普通孩子一样随意吃糖，也长期被医院环境限制。她看似安静疏离，却仍然保留着对外部世界的好奇。主角一开始只是出于一时好心递出一颗糖，后来却逐渐开始关心她的身体、她的画、她想去的公园，以及她能否顺利走向未来。

故事希望表达：真正的治愈并不总是宏大的奇迹，也可能来自一句鼓励、一次陪伴，或一个愿意为他人停留的人。

### 项目信息

| 项目 | 内容 |
| --- | --- |
| 引擎 | Unity |
| Unity 版本 | 2022.3.62f3c1 |
| 工程目录 | `gal/` |
| 推荐启动场景 | `gal/Assets/Scenes/VNMainMenu.unity` |
| 主线剧情场景 | `gal/Assets/Scenes/VNGamePlay.unity` |
| 小游戏场景 | `gal/Assets/Scenes/Pacman.unity`, `gal/Assets/Scenes/Jump2Pac*.unity` |

### 本地运行

1. 克隆本仓库。
2. 使用 Unity Hub 打开 `gal/` 目录。
3. 使用 Unity `2022.3.62f3c1`，或兼容的 Unity 2022.3 LTS 版本。
4. 等待 Unity 导入资源并恢复 Package Manager 依赖。
5. 打开 `Assets/Scenes/VNMainMenu.unity`。
6. 点击 Unity 编辑器顶部的 Play 运行游戏。

### 项目结构

```text
SZU-Final-Project/
├─ README.md
├─ LICENSE
├─ .gitignore
└─ gal/
   ├─ Assets/
   │  ├─ Scenes/                         Unity 场景
   │  ├─ Resources/VNovelizerRes/         视觉小说资源
   │  │  ├─ Backgrounds/                  背景图
   │  │  ├─ Characters/                   角色配置
   │  │  ├─ VNScripts/                    CSV 剧本
   │  │  ├─ Audio/                        BGM、音效、语音
   │  │  └─ VNPrefabs/                    视觉小说 UI 预制体
   │  ├─ PacScripts/                      小游戏脚本
   │  ├─ PacPrefabs/                      小游戏预制体
   │  ├─ PacPhotos/                       小游戏图片资源
   │  ├─ PacAudio/                        小游戏音频
   │  └─ gal资源/                         原始美术、音乐与 UI 资源
   ├─ Packages/                           Unity 包依赖
   │  └─ com.fakecorps.vnovelizer/        本项目使用的本地 VNovelizer 包
   └─ ProjectSettings/                    Unity 项目设置
```

### 剧本与资源位置

- 主线 CSV 剧本：`gal/Assets/Resources/VNovelizerRes/VNScripts/`
- Excel 剧本源文件：`gal/Assets/Resources/VNovelizerRes/ExcelVNScripts/`
- 视觉小说背景：`gal/Assets/Resources/VNovelizerRes/Backgrounds/`
- 角色源文件：`gal/Assets/gal资源/大作业资源/角色/`
- 剧情 UI 预制体：`gal/Assets/Resources/VNovelizerRes/VNPrefabs/UI/`
- 小游戏脚本：`gal/Assets/PacScripts/`
- 小游戏预制体：`gal/Assets/PacPrefabs/`

### 依赖说明

项目使用 Unity Package Manager 管理依赖。定制后的 VNovelizer 包已作为本地 embedded package 放在：

```text
gal/Packages/com.fakecorps.vnovelizer
```

因此 clone 项目后不需要再从远程仓库拉取 VNovelizer。若需要修改视觉小说框架逻辑，应修改 `gal/Packages/com.fakecorps.vnovelizer/` 内的文件，而不是修改 `gal/Library/PackageCache/`。`Library` 是 Unity 自动生成的本地缓存目录，不应提交到版本库。

主要依赖包括：

- VNovelizer
- TextMeshPro
- Unity Input System
- Unity Localization
- Universal Render Pipeline
- Addressables
- PrimeTween

### 提交与打包说明

课程作业提交时通常可以准备：

- 游戏设计文档 Word 版
- 游戏录屏视频
- 可执行文件夹
- 从 Unity 导出的全部 `Assets` 的 `.unitypackage`
- 如需完整 Unity 工程：`Assets/`、`Packages/`、`ProjectSettings/`

不建议提交 Unity 自动生成的缓存目录：

```text
gal/Library/
gal/Temp/
gal/Logs/
gal/obj/
gal/.vs/
```

### 许可证

本项目用于课程作业展示与学习交流。

源代码、媒体素材、剧情文本和第三方组件适用不同授权条款。详情见 [LICENSE](LICENSE)。

简而言之：请勿在未经许可的情况下复用或再分发本项目中的游戏素材、剧情文本、音乐、音效、角色图、背景图或其他媒体内容。第三方包和第三方素材仍遵循其各自许可证。

---

## English

> A Unity visual novel about a hospital, an unexpected meeting, companionship, and learning to move forward again.

**The Angel I Met in the Hospital** is a story-driven visual novel made with Unity. The story begins after a late-night work accident. The protagonist is hospitalized and expects nothing more than a dull interruption to everyday life, until he meets Xiaoyan, a young girl with type 1 diabetes, in the hospital corridor.

Xiaoyan is quiet and sensitive, yet she still loves candy, drawing, parks, and a cat named Kulu. Through conversations, visits, choices, and mini-game challenges, the protagonist gradually changes from a tired office worker who hides behind jokes into someone who can sincerely care for another person.

The “angel” in this story is not necessarily a distant or sacred being. It may be a child trying to smile in a hospital room, a nurse carefully looking after patients, or an ordinary person who chooses to stay with someone at an important moment.

### Features

- Chapter-based visual novel story from the prologue to Chapter 9
- Dialogue progression, choices, save/load, history, and settings
- Characters such as Xiaoyan, the nurse, and the 666 God, with expression variations
- Story backgrounds including hospital rooms, corridors, a city park, and mysterious spaces
- Pacman-style mini-game challenges connected to key story moments
- BGM, sound effects, voice clips, UI resources, and basic gallery content

### Gameplay

The game consists of two main parts:

**Visual novel sections**  
Players click through dialogue and read interactions between the protagonist, Xiaoyan, the nurse, coworkers, and the mysterious god. The writing uses a light and humorous tone, while the core themes focus on illness, loneliness, companionship, and hope.

**Mini-game sections**  
At key story moments, the game switches to Pacman-style 2D stages. Players move through maze-like levels, collect items, and avoid enemies. These mini-games are not just extra gameplay; they represent the obstacles the protagonist must overcome in order to help Xiaoyan.

### Story

The protagonist is an ordinary office worker worn down by work. After an accident on his way home late at night, he is hospitalized. At first, he sees his hospital stay as boring, troublesome, and frustrating. Then he meets Xiaoyan.

Xiaoyan has type 1 diabetes. She cannot eat candy freely like other children, and her life is restricted by the hospital environment. Although she seems quiet and distant, she still keeps her curiosity about the outside world. The protagonist first gives her a piece of candy out of simple kindness, but later begins to care about her health, her drawings, the park she wants to visit, and whether she can move toward a better future.

The story aims to express that healing does not always come from grand miracles. Sometimes it comes from a word of encouragement, a moment of companionship, or someone willing to stay.

### Project Info

| Item | Detail |
| --- | --- |
| Engine | Unity |
| Unity Version | 2022.3.62f3c1 |
| Project Path | `gal/` |
| Start Scene | `gal/Assets/Scenes/VNMainMenu.unity` |
| Main VN Scene | `gal/Assets/Scenes/VNGamePlay.unity` |
| Mini-game Scenes | `gal/Assets/Scenes/Pacman.unity`, `gal/Assets/Scenes/Jump2Pac*.unity` |

### Run Locally

1. Clone this repository.
2. Open the `gal/` folder with Unity Hub.
3. Use Unity `2022.3.62f3c1`, or another compatible Unity 2022.3 LTS version.
4. Wait for Unity to import assets and restore packages.
5. Open `Assets/Scenes/VNMainMenu.unity`.
6. Press Play in the Unity Editor.

### Repository Structure

```text
SZU-Final-Project/
├─ README.md
├─ LICENSE
├─ .gitignore
└─ gal/
   ├─ Assets/
   │  ├─ Scenes/                         Unity scenes
   │  ├─ Resources/VNovelizerRes/         visual novel resources
   │  │  ├─ Backgrounds/                  backgrounds
   │  │  ├─ Characters/                   character configs
   │  │  ├─ VNScripts/                    CSV scripts
   │  │  ├─ Audio/                        BGM, SFX, voice
   │  │  └─ VNPrefabs/                    visual novel UI prefabs
   │  ├─ PacScripts/                      mini-game scripts
   │  ├─ PacPrefabs/                      mini-game prefabs
   │  ├─ PacPhotos/                       mini-game sprites/images
   │  ├─ PacAudio/                        mini-game audio
   │  └─ gal资源/                         original art, audio, and UI resources
   ├─ Packages/                           Unity package dependencies
   │  └─ com.fakecorps.vnovelizer/        embedded VNovelizer package
   └─ ProjectSettings/                    Unity project settings
```

### Script And Asset Locations

- Main CSV scripts: `gal/Assets/Resources/VNovelizerRes/VNScripts/`
- Excel script sources: `gal/Assets/Resources/VNovelizerRes/ExcelVNScripts/`
- VN backgrounds: `gal/Assets/Resources/VNovelizerRes/Backgrounds/`
- Character source images: `gal/Assets/gal资源/大作业资源/角色/`
- VN UI prefabs: `gal/Assets/Resources/VNovelizerRes/VNPrefabs/UI/`
- Mini-game scripts: `gal/Assets/PacScripts/`
- Mini-game prefabs: `gal/Assets/PacPrefabs/`

### Dependencies

This project uses Unity Package Manager. The customized VNovelizer package is embedded locally:

```text
gal/Packages/com.fakecorps.vnovelizer
```

Because of this, the project does not depend on fetching VNovelizer from a remote Git repository after cloning. If you need to modify visual novel framework behavior, edit files under `gal/Packages/com.fakecorps.vnovelizer/` instead of `gal/Library/PackageCache/`. The `Library` folder is Unity-generated local cache and should not be committed.

Main dependencies include:

- VNovelizer
- TextMeshPro
- Unity Input System
- Unity Localization
- Universal Render Pipeline
- Addressables
- PrimeTween

### Notes For Submission Or Packaging

For coursework submission, the following files are usually useful:

- game design document in Word format
- gameplay recording video
- built executable folder
- `.unitypackage` exported from all `Assets`
- for a complete Unity project: `Assets/`, `Packages/`, and `ProjectSettings/`

Do not submit Unity-generated cache folders:

```text
gal/Library/
gal/Temp/
gal/Logs/
gal/obj/
gal/.vs/
```

### License

This project is provided for coursework demonstration and learning purposes.

Source code, media assets, narrative content, and third-party components are governed by different terms. See [LICENSE](LICENSE) for details.

In short: please do not reuse or redistribute the game assets, story text, music, sound effects, character images, backgrounds, or other media materials without permission. Third-party packages and assets remain governed by their own licenses.
