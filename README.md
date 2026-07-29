<div align="center">

# ⚡ Quicklet

**极速 Windows 网页唤起与带参搜索工具**

*启发自 Raycast & Alfred，专为 Windows 用户打造的键盘优先桌面效率神器*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?logo=windows)](https://github.com)
[![Framework](https://img.shields.io/badge/Framework-.NET%208%20WPF-purple?logo=dotnet)](https://github.com)
[![Size](https://img.shields.io/badge/Exe%20Size-234%20KB-success)](https://github.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

</div>

---

## 📖 简介 (Overview)

**Quicklet** 是一款原生、极轻量、极速的 Windows 全局热键网页直达与带参搜索工具。

在日常开发与办公中，我们频繁需要在浏览器中打开 GitHub、Google 翻译、哔哩哔哩、YouTube 或特定内部站点。传统的做法是打开浏览器、点击书签或输入网址，过程繁琐且打断专注。

Quicklet 让一切回归键盘：**按下全局快捷键（默认 `Alt + Q`），输入触发词（如 `gh` 或 `bilibili`）加搜索内容，按下回车即可一秒直达目标网页！**

---

## ✨ 核心功能 (Core Features)

### 1. 🚀 快捷键唤起 & 键盘优先 (Global Hotkey & Keyboard-First)
* **全局快捷键**：默认 `Alt + Q`（支持在设置中心随时自定义录制），无论处于任何软件中，随叫随到，按 `Esc` 极速隐藏。
* **高灵敏录制**：设置中心内置防止 focus 丢失的事件拦截机制，点击键帽即可精准录制任意组合键。

### 2. 🎯 双模式智能路由 (Smart Dual-Mode Routing)
* **网页直达 (Direct Jump)**：输入关键字（如 `bilibili`）按 Enter，直接在浏览器中打开站点的首页。
* **带参搜索 (Parametric Search)**：输入关键字 + 空格 + 检索内容（如 `github wpf`），自动替换 URL 中的 `{query}` 占位符并唤起浏览器搜索结果页。
* **默认搜索引擎兜底**：输入非自定义关键字内容时，自动使用配置的默认搜索引擎（支持 Google、百度、必应或自定义 URL）。

### 3. 🎨 极致的 Windows 11 Fluent 视觉美学 (Native Windows Design)
* **原生 Fluent 风格**：专为 Windows 11 设计的现代卡片式 UI，告别 MacOS 移植感。
* **自动亮/暗主题同步**：实时监听 Windows 系统注册表，跟随系统亮色/深色主题无缝自动切换。
* **自定义无边框弹窗 (Custom MessageBox)**：告别老旧的 Win32 灰色提示框，提示弹窗与主界面采用完全一致的圆角、投影及主题色。
* **极简 8px 悬浮滚动条 (Slim Floating ScrollBar)**：借鉴 VS Code 与 macOS 规范，无箭头、透明轨底、8px 窄径圆角胶囊滑块，带 Hover/Drag 平滑渐变。
* **1:1 垂直对称美学 Header**：无底线边框、主题蓝闪电 Icon 徽标、粗体品牌词与精细 4px 矢量实心分隔点。

### 4. ⚙️ 开箱即用 & 灵活配置 (Easy Configuration)
* **开机自启动**：设置中心内置一键开关，自动写入 Windows 注册表自启项。
* **可视化规则管理**：支持表格内双击单元格直接编辑触发词、显示名称、直达 URL 与带参 URL，支持实时新增/删除。
* **热重载 (Hot-Reload)**：修改保存后无需重启软件，配置即刻在后台热加载生效。

---

## 🛠️ 技术栈与架构 (Tech Stack)

* **核心框架**：C# / .NET 8 WPF (Windows Presentation Foundation)
* **体积控制**：单文件裁剪打包（PublishSingleFile / ReadyToRun），最终可执行程序仅 **234 KB**。
* **持久化存储**：轻量级 JSON 配置文件 (`config.json`)。
* **系统集成**：Windows Win32 API 热键注册（`RegisterHotKey`）与注册表自启挂载。

---

## 📦 配置文件说明 (`config.json`)

Quicklet 的配置文件存放在程序同级目录下的 `config.json` 中，格式简单直观：

```json
{
  "Hotkey": "Alt+Q",
  "Theme": "Auto",
  "DefaultSearchUrl": "https://www.google.com/search?q={query}",
  "StartupWithWindows": false,
  "Keywords": [
    {
      "Keyword": "google",
      "Name": "Google",
      "Url": "https://www.google.com",
      "SearchUrl": "https://www.google.com/search?q={query}"
    },
    {
      "Keyword": "github",
      "Name": "GitHub",
      "Url": "https://github.com",
      "SearchUrl": "https://github.com/search?q={query}"
    },
    {
      "Keyword": "bilibili",
      "Name": "哔哩哔哩",
      "Url": "https://www.bilibili.com",
      "SearchUrl": "https://search.bilibili.com/all?keyword={query}"
    }
  ]
}
```

---

## 🚀 编译与构建 (Build & Publish)

### 开发环境要求
* Windows 10 / 11
* .NET 8.0 SDK 或更高版本

### 编译与发布单文件 Exe

```bash
# 克隆项目
git clone https://github.com/your-username/Quicklet.git
cd Quicklet

# 编译项目
dotnet build

# 发布为 Release 单文件绿色版
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:PublishReadyToRun=true
```

发布成功后，你可以在 `bin/Release/net8.0-windows/win-x64/publish/` 目录下找到轻量的 `Quicklet.exe`。

---

## 📄 开源许可证 (License)

本项目基于 [MIT License](LICENSE) 开源，欢迎自由下载、使用或提交 PR！
